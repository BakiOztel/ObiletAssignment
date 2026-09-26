/**
 * "Back to top" button for long journey lists. It appears once the page heading (the band) has scrolled
 * out of view; an IntersectionObserver is used instead of a scroll listener, so nothing runs while scrolling.
 */
const button = document.getElementById('back-to-top');
const band = document.querySelector('.band');

// The button is only rendered when there are journeys to scroll through.
if (button && band) {
    new IntersectionObserver(([entry]) => {
        button.classList.toggle('is-visible', !entry.isIntersecting);
    }).observe(band);

    button.addEventListener('click', () => {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });
}
