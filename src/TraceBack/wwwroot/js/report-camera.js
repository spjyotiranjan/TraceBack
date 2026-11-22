export const maxPhotoBytes = 5 * 1024 * 1024;

export function connectCamera(opener, input, photoChanged) {
  const dialog = document.querySelector('[data-report-camera]');
  if (!dialog?.showModal || !navigator.mediaDevices?.getUserMedia || !window.DataTransfer ||
      !HTMLCanvasElement.prototype.toBlob) {
    // On phones that expose native capture, ask the platform camera to supply the
    // normal file input. The regular upload remains available separately.
    if ('capture' in input && matchMedia('(pointer: coarse)').matches) {
      opener.hidden = false;
      opener.addEventListener('click', () => {
        input.setAttribute('capture', 'environment');
        try { input.click(); } finally { input.removeAttribute('capture'); }
      });
    }
    return;
  }

  const find = name => dialog.querySelector(`[data-camera-${name}]`);
  const video = find('video');
  const frame = find('frame');
  const review = find('review');
  const status = find('status');
  const capture = find('capture');
  const retake = find('retake');
  const confirm = find('confirm');
  const upload = find('upload');
  const cancel = find('cancel');
  let stream;
  let frameUrl;
  let photo;
  let generation = 0;

  function stopTracks() {
    stream?.getTracks().forEach(track => track.stop());
    stream = null;
    video.srcObject = null;
  }

  function discardReview() {
    if (frameUrl) URL.revokeObjectURL(frameUrl);
    frameUrl = null;
    review.removeAttribute('src');
    photo = null;
  }

  function setState(state, message) {
    status.textContent = message;
    frame.hidden = state === 'error';
    video.hidden = state === 'review';
    review.hidden = state !== 'review';
    capture.hidden = state === 'review' || state === 'error';
    capture.disabled = state !== 'live';
    retake.hidden = state !== 'review';
    confirm.hidden = state !== 'review';
    upload.hidden = state !== 'error';
  }

  function close() {
    generation++;
    stopTracks();
    discardReview();
    if (dialog.open) {
      dialog.close();
      opener.focus({ preventScroll: true });
    }
  }

  function fail(message) {
    stopTracks();
    discardReview();
    setState('error', message);
    upload.focus();
  }

  async function start() {
    const request = ++generation;
    discardReview();
    setState('waiting', 'Waiting for camera permission…');
    try {
      const opened = await navigator.mediaDevices.getUserMedia({ audio: false, video: { facingMode: { ideal: 'environment' } } });
      if (request !== generation || !dialog.open) {
        opened.getTracks().forEach(track => track.stop());
        return;
      }
      stream = opened;
      for (const track of opened.getVideoTracks()) {
        track.addEventListener('ended', () => {
          if (stream === opened) {
            generation++;
            fail('The camera stopped. You can upload an image instead.');
          }
        }, { once: true });
      }
      video.srcObject = stream;
      await video.play();
      if (request !== generation || !dialog.open) return;
      if (!video.videoWidth || !video.videoHeight) throw new Error('No video frame');
      setState('live', 'Camera ready.');
    } catch (error) {
      if (request !== generation || !dialog.open) return;
      const denied = ['NotAllowedError', 'SecurityError'].includes(error?.name);
      fail(denied ? 'Camera access is blocked. Allow access in your browser settings, or upload an image instead.'
        : 'The camera is unavailable. You can upload an image instead.');
    }
  }

  opener.hidden = false;
  opener.addEventListener('click', () => {
    dialog.showModal();
    start();
  });
  retake.addEventListener('click', () => { cancel.focus(); start(); });
  cancel.addEventListener('click', close);
  dialog.addEventListener('cancel', event => { event.preventDefault(); close(); });
  dialog.addEventListener('keydown', event => {
    if (event.key !== 'Tab') return;
    const buttons = [...dialog.querySelectorAll('button:not([disabled]):not([hidden])')];
    const first = buttons[0];
    const last = buttons.at(-1);
    if (event.shiftKey && (document.activeElement === first || !buttons.includes(document.activeElement))) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault();
      first.focus();
    }
  });
  upload.addEventListener('click', () => { close(); input.focus(); input.click(); });
  capture.addEventListener('click', async () => {
    if (!stream || capture.disabled) return;
    const request = generation;
    cancel.focus();
    setState('waiting', 'Preparing photo…');
    try {
      const canvas = document.createElement('canvas');
      const scale = Math.min(1, 1600 / Math.max(video.videoWidth, video.videoHeight));
      canvas.width = Math.max(1, Math.floor(video.videoWidth * scale));
      canvas.height = Math.max(1, Math.floor(video.videoHeight * scale));
      canvas.getContext('2d').drawImage(video, 0, 0, canvas.width, canvas.height);
      stopTracks();
      const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/jpeg', .88));
      if (request !== generation || !dialog.open) return;
      if (!blob?.size || blob.size > maxPhotoBytes || blob.type !== 'image/jpeg') throw new Error('Cannot encode photo');
      photo = new File([blob], 'camera-photo.jpg', { type: blob.type });
      review.src = frameUrl = URL.createObjectURL(blob);
      setState('review', 'Review your photo before attaching it.');
      confirm.focus();
    } catch {
      if (request === generation && dialog.open) fail('The photo could not be prepared. You can upload an image instead.');
    }
  });
  confirm.addEventListener('click', () => {
    if (!photo) return;
    try {
      const transfer = new DataTransfer();
      transfer.items.add(photo);
      input.files = transfer.files;
    } catch {
      fail('This browser could not attach the photo. You can upload an image instead.');
      return;
    }
    close();
    photoChanged();
  });
  window.addEventListener('pagehide', close);
}
