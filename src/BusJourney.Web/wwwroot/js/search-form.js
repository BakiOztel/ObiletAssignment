import { LocationAutocomplete } from './location-autocomplete.js';

/**
 * Search form behaviour: origin/destination autocomplete, swap, day arrows and the Today/Tomorrow switch,
 * client-side validation (mirrors JourneyQueryValidator on the server) and remembering the last search.
 */

const STORAGE_KEY = 'lastSearch';

const form = document.getElementById('search-form');
const errorList = document.getElementById('form-errors');
const dateInput = form.elements.date;
const today = dateInput.min;
const tomorrow = dateInput.dataset.tomorrow;
const quickDates = form.querySelector('.quick-dates');
const quickDateButtons = quickDates.querySelectorAll('[data-quick-date]');
const dayStepButtons = form.querySelectorAll('[data-day-step]');
const searchButton = form.querySelector('[data-search-button]');
const loadingOverlay = document.getElementById('loading-overlay');

const initialItems = JSON.parse(document.getElementById('initial-locations').textContent);
// Each field disables, in its suggestions, the location already picked on the other side.
const origin = new LocationAutocomplete(form.querySelector('[data-location-field="origin"]'), {
    initialItems,
    getDisabledId: () => destination.getValue()?.id,
    disabledHint: 'Varış noktası olarak seçili',
});
const destination = new LocationAutocomplete(form.querySelector('[data-location-field="destination"]'), {
    initialItems,
    getDisabledId: () => origin.getValue()?.id,
    disabledHint: 'Kalkış noktası olarak seçili',
});

// localStorage may be unavailable (private mode, blocked storage); the form must keep working without it.
function loadLastSearch() {
    try {
        return JSON.parse(localStorage.getItem(STORAGE_KEY));
    } catch {
        return null;
    }
}

function saveLastSearch() {
    try {
        localStorage.setItem(STORAGE_KEY, JSON.stringify({
            origin: origin.getValue(),
            destination: destination.getValue(),
            date: dateInput.value,
        }));
    } catch {
        // Not remembering the search is acceptable.
    }
}

/** Adds days to an ISO date (yyyy-MM-dd). UTC avoids daylight-saving shifts. */
function addDays(isoDate, days) {
    const date = new Date(`${isoDate}T00:00:00Z`);
    date.setUTCDate(date.getUTCDate() + days);
    return date.toISOString().slice(0, 10);
}

// Keeps the Today/Tomorrow switch and the previous-day arrow in step with the date field.
function syncDateControls() {
    let activeIndex = '';
    quickDateButtons.forEach((button, index) => {
        const isActive = button.dataset.quickDate === dateInput.value;
        button.setAttribute('aria-pressed', String(isActive));
        if (isActive) activeIndex = String(index);
    });
    quickDates.dataset.active = activeIndex;

    // ISO dates (yyyy-MM-dd) compare correctly as strings.
    dayStepButtons[0].disabled = !dateInput.value || dateInput.value <= today;
}

function validate() {
    const errors = [];
    const from = origin.getValue();
    const to = destination.getValue();

    if (!from) errors.push('Lütfen kalkış noktası seçin.');
    if (!to) errors.push('Lütfen varış noktası seçin.');
    if (from && to && from.id === to.id) errors.push('Kalkış ve varış noktası aynı olamaz.');
    // ISO dates (yyyy-MM-dd) compare correctly as strings.
    if (!dateInput.value) errors.push('Lütfen bir tarih seçin.');
    else if (dateInput.value < today) errors.push('Geçmiş bir tarih seçilemez.');

    return errors;
}

function showErrors(errors) {
    errorList.replaceChildren(...errors.map((message) => {
        const item = document.createElement('li');
        item.textContent = message;
        return item;
    }));
    errorList.hidden = errors.length === 0;
}

// Journeys are fetched server side before the next page renders; the overlay covers that wait
// and the disabled button prevents a double submit.
function setLoading(isLoading) {
    loadingOverlay.classList.toggle('is-visible', isLoading);
    searchButton.disabled = isLoading;
}

function restoreLastSearch() {
    const saved = loadLastSearch();
    if (!saved) {
        return;
    }
    if (saved.origin) origin.setValue(saved.origin);
    if (saved.destination) destination.setValue(saved.destination);
    // A remembered date that is now in the past falls back to the default (tomorrow).
    dateInput.value = saved.date && saved.date >= today ? saved.date : tomorrow;
}

form.querySelector('[data-swap]').addEventListener('click', () => {
    const from = origin.getValue();
    origin.setValue(destination.getValue());
    destination.setValue(from);
});

quickDateButtons.forEach((button) => button.addEventListener('click', () => {
    dateInput.value = button.dataset.quickDate;
    syncDateControls();
}));

// Previous / next day. Never steps before today; an empty field starts from today.
dayStepButtons.forEach((button) => button.addEventListener('click', () => {
    const next = addDays(dateInput.value || today, Number(button.dataset.dayStep));
    dateInput.value = next < today ? today : next;
    syncDateControls();
}));

dateInput.addEventListener('change', syncDateControls);

form.addEventListener('submit', (event) => {
    const errors = validate();
    showErrors(errors);
    if (errors.length) {
        event.preventDefault();
        return;
    }
    saveLastSearch();
    setLoading(true);
});

// Going back restores this page from the back/forward cache with the overlay still visible.
window.addEventListener('pageshow', (event) => {
    if (event.persisted) {
        setLoading(false);
    }
});

if (form.dataset.restore === 'true') {
    restoreLastSearch();
}
syncDateControls();
