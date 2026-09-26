/**
 * Reusable location combobox (WAI-ARIA combobox pattern).
 *
 * Markup hooks inside `root`:
 *   [data-ac-input]  visible text input (role="combobox")
 *   [data-ac-value]  hidden input that carries the selected location id to the server
 *   [data-ac-list]   suggestion list (role="listbox")
 *
 * Suggestions come from this application's backend (`endpoint`), never from the provider API directly.
 * Typing is debounced and every new request aborts the previous one, so a slow stale response
 * can never overwrite a newer one.
 */
export class LocationAutocomplete {
    #input;
    #hidden;
    #list;
    #endpoint;
    #debounceMs;
    #initialItems;
    #onChange;
    #getTakenId;
    #takenHint;
    #onSwap;
    #items = [];
    #activeIndex = -1;
    #selected = null;
    #timer = 0;
    #abortController = null;

    /**
     * @param {HTMLElement} root
     * @param {{
     *   endpoint?: string,
     *   debounceMs?: number,
     *   initialItems?: {id:number,name:string}[],
     *   onChange?: (item) => void,
     *   getTakenId?: () => number | undefined,
     *   takenHint?: string,
     *   onSwap?: (previous) => void,
     * }} options
     *   `getTakenId` is asked on every render; the matching suggestion is greyed out and labelled with
     *   `takenHint`. Picking it still works and calls `onSwap` with this field's previous value, so the caller
     *   can move it to the other field (origin and destination never end up equal).
     */
    constructor(root, {
        endpoint = '/locations',
        debounceMs = 300,
        initialItems = [],
        onChange,
        getTakenId = () => undefined,
        takenHint = '',
        onSwap,
    } = {}) {
        this.#input = root.querySelector('[data-ac-input]');
        this.#hidden = root.querySelector('[data-ac-value]');
        this.#list = root.querySelector('[data-ac-list]');
        this.#endpoint = endpoint;
        this.#debounceMs = debounceMs;
        this.#initialItems = initialItems;
        this.#onChange = onChange;
        this.#getTakenId = getTakenId;
        this.#takenHint = takenHint;
        this.#onSwap = onSwap;

        if (this.#hidden.value) {
            this.#selected = { id: Number(this.#hidden.value), name: this.#input.value };
        }

        this.#input.addEventListener('focus', () => {
            this.#input.select();
            this.#render(this.#initialItems);
        });
        // The field keeps focus after a pick, so a second tap fires no focus event: reopen the list on click too.
        this.#input.addEventListener('click', () => {
            if (this.#list.hidden) {
                this.#input.select();
                this.#render(this.#initialItems);
            }
        });
        this.#input.addEventListener('input', () => this.#onInput());
        this.#input.addEventListener('keydown', (event) => this.#onKeyDown(event));
        document.addEventListener('pointerdown', (event) => {
            if (!root.contains(event.target)) {
                this.#close();
            }
        });
    }

    /** @returns {{id:number, name:string} | null} */
    getValue() {
        return this.#selected;
    }

    /** @param {{id:number, name:string} | null} item */
    setValue(item) {
        this.#selected = item;
        this.#input.value = item?.name ?? '';
        this.#hidden.value = item?.id ?? '';
    }

    #onInput() {
        // Free text is not a selection: the id is cleared until the user picks a suggestion.
        this.#selected = null;
        this.#hidden.value = '';
        clearTimeout(this.#timer);

        const term = this.#input.value.trim();
        if (!term) {
            this.#abortController?.abort();
            this.#render(this.#initialItems);
            return;
        }
        this.#timer = setTimeout(() => this.#search(term), this.#debounceMs);
    }

    async #search(term) {
        this.#abortController?.abort();
        this.#abortController = new AbortController();

        try {
            const response = await fetch(`${this.#endpoint}?q=${encodeURIComponent(term)}`, {
                headers: { Accept: 'application/json' },
                signal: this.#abortController.signal,
            });
            if (!response.ok) {
                throw new Error(`Location search failed with HTTP ${response.status}`);
            }
            this.#render(await response.json());
        } catch (error) {
            if (error.name !== 'AbortError') {
                console.error(error);
                this.#close();
            }
        }
    }

    #onKeyDown(event) {
        const isOpen = !this.#list.hidden;
        switch (event.key) {
            case 'ArrowDown':
                event.preventDefault();
                if (!isOpen) {
                    this.#render(this.#items.length ? this.#items : this.#initialItems);
                } else {
                    this.#move(1);
                }
                break;
            case 'ArrowUp':
                event.preventDefault();
                this.#move(-1);
                break;
            case 'Enter':
                if (isOpen && this.#activeIndex >= 0) {
                    event.preventDefault();
                    this.#select(this.#items[this.#activeIndex]);
                }
                break;
            case 'Escape':
                this.#close();
                break;
        }
    }

    #render(items) {
        this.#items = items;
        this.#activeIndex = -1;
        this.#input.removeAttribute('aria-activedescendant');

        const takenId = this.#getTakenId();

        if (items.length === 0) {
            const empty = document.createElement('li');
            empty.className = 'suggestion suggestion--empty';
            empty.textContent = 'Sonuç bulunamadı';
            this.#list.replaceChildren(empty);
        } else {
            this.#list.replaceChildren(...items.map((item, index) =>
                this.#createOption(item, index, item.id === takenId)));
        }
        this.#open();
    }

    #createOption(item, index, taken) {
        const option = document.createElement('li');
        option.id = `${this.#list.id}-option-${index}`;
        option.className = taken ? 'suggestion suggestion--taken' : 'suggestion';
        option.setAttribute('role', 'option');
        option.setAttribute('aria-selected', 'false');
        option.textContent = item.name;

        if (taken) {
            if (this.#takenHint) {
                const hint = document.createElement('span');
                hint.className = 'suggestion__hint';
                hint.textContent = this.#takenHint;
                option.append(hint);
            }
        }

        // pointerdown only keeps the input focused. Selecting must wait for click: closing the list on touch-down
        // lets the finger's tap land on whatever is underneath (the search button), and would also select on scroll.
        option.addEventListener('pointerdown', (event) => event.preventDefault());
        option.addEventListener('click', () => this.#select(item));
        return option;
    }

    /** Moves the keyboard highlight by `step`, staying within the list. */
    #move(step) {
        const next = this.#activeIndex + step;
        if (next >= 0 && next < this.#list.querySelectorAll('[role="option"]').length) {
            this.#highlight(next);
        }
    }

    #highlight(index) {
        const options = this.#list.querySelectorAll('[role="option"]');
        if (!options.length) {
            return;
        }
        options[this.#activeIndex]?.setAttribute('aria-selected', 'false');
        this.#activeIndex = index;
        const active = options[index];
        active.setAttribute('aria-selected', 'true');
        active.scrollIntoView({ block: 'nearest' });
        this.#input.setAttribute('aria-activedescendant', active.id);
    }

    #select(item) {
        const previous = this.#selected;
        const wasTaken = item.id === this.#getTakenId();
        this.setValue(item);
        this.#close();
        if (wasTaken) this.#onSwap?.(previous);
        this.#onChange?.(item);
    }

    #open() {
        this.#list.hidden = false;
        this.#input.setAttribute('aria-expanded', 'true');
    }

    #close() {
        this.#list.hidden = true;
        this.#input.setAttribute('aria-expanded', 'false');
        this.#input.removeAttribute('aria-activedescendant');
    }
}
