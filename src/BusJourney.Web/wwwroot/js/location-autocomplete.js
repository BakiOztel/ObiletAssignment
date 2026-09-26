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
    #getDisabledId;
    #disabledHint;
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
     *   getDisabledId?: () => number | undefined,
     *   disabledHint?: string,
     * }} options
     *   `getDisabledId` is asked on every render; the matching suggestion is shown but cannot be picked
     *   (used to keep origin and destination different), `disabledHint` explains why.
     */
    constructor(root, {
        endpoint = '/locations',
        debounceMs = 300,
        initialItems = [],
        onChange,
        getDisabledId = () => undefined,
        disabledHint = '',
    } = {}) {
        this.#input = root.querySelector('[data-ac-input]');
        this.#hidden = root.querySelector('[data-ac-value]');
        this.#list = root.querySelector('[data-ac-list]');
        this.#endpoint = endpoint;
        this.#debounceMs = debounceMs;
        this.#initialItems = initialItems;
        this.#onChange = onChange;
        this.#getDisabledId = getDisabledId;
        this.#disabledHint = disabledHint;

        if (this.#hidden.value) {
            this.#selected = { id: Number(this.#hidden.value), name: this.#input.value };
        }

        this.#input.addEventListener('focus', () => {
            this.#input.select();
            this.#render(this.#initialItems);
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

        const disabledId = this.#getDisabledId();

        if (items.length === 0) {
            const empty = document.createElement('li');
            empty.className = 'suggestion suggestion--empty';
            empty.textContent = 'Sonuç bulunamadı';
            this.#list.replaceChildren(empty);
        } else {
            this.#list.replaceChildren(...items.map((item, index) =>
                this.#createOption(item, index, item.id === disabledId)));
        }
        this.#open();
    }

    #createOption(item, index, disabled) {
        const option = document.createElement('li');
        option.id = `${this.#list.id}-option-${index}`;
        option.className = disabled ? 'suggestion suggestion--disabled' : 'suggestion';
        option.setAttribute('role', 'option');
        option.setAttribute('aria-selected', 'false');
        option.textContent = item.name;

        if (disabled) {
            option.setAttribute('aria-disabled', 'true');
            if (this.#disabledHint) {
                const hint = document.createElement('span');
                hint.className = 'suggestion__hint';
                hint.textContent = this.#disabledHint;
                option.append(hint);
            }
        }

        // pointerdown (not click) so the input keeps focus and the outside-click handler does not fire first.
        option.addEventListener('pointerdown', (event) => {
            event.preventDefault();
            if (!disabled) {
                this.#select(item);
            }
        });
        return option;
    }

    /** Moves the keyboard highlight by `step`, skipping disabled options. */
    #move(step) {
        const options = this.#list.querySelectorAll('[role="option"]');
        for (let index = this.#activeIndex + step; index >= 0 && index < options.length; index += step) {
            if (options[index].getAttribute('aria-disabled') !== 'true') {
                this.#highlight(index);
                return;
            }
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
        this.setValue(item);
        this.#close();
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
