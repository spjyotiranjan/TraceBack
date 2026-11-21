document.querySelectorAll('img[data-report-photo]').forEach(photo => {
  const showError = () => {
    photo.hidden = true;
    photo.nextElementSibling.hidden = false;
  };
  photo.addEventListener('error', showError, { once: true });
  // Cached failures can occur before the deferred script runs.
  if (photo.complete && photo.naturalWidth === 0) showError();
});