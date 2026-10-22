(() => {
  const editor = document.querySelector('[data-image-editor]');
  if (!editor) return;

  const form = editor.closest('form');
  const input = editor.querySelector('#image');
  const choices = editor.querySelector('[data-image-choices]');
  const takePhoto = editor.querySelector('[data-take-photo]');
  const unavailable = editor.querySelector('[data-camera-unavailable]');
  const panel = editor.querySelector('[data-camera-panel]');
  const heading = editor.querySelector('[data-camera-heading]');
  const video = editor.querySelector('[data-camera-video]');
  const cameraStatus = editor.querySelector('[data-camera-status]');
  const capture = editor.querySelector('[data-capture-photo]');
  const retry = editor.querySelector('[data-retry-camera]');
  const cameraUpload = editor.querySelector('[data-camera-upload]');
  const preview = editor.querySelector('[data-image-preview]');
  const previewImage = editor.querySelector('[data-preview-image]');
  const previewStatus = editor.querySelector('[data-preview-status]');
  const previewDetails = editor.querySelector('[data-preview-details]');
  const retake = editor.querySelector('[data-retake-photo]');
  const removal = editor.querySelector('[data-image-removal]');
  const removeCheckbox = editor.querySelector('#remove-image');
  const nativeRemove = editor.querySelector('[data-native-remove]');
  const currentImageUrl = editor.dataset.currentImageUrl || null;
  const error = editor.querySelector('[data-client-image-error]');
  const submit = form.querySelector('[type="submit"]');
  const submitStatus = form.querySelector('[data-submit-status]');
  const maxBytes = 5 * 1024 * 1024;
  const acceptedTypes = new Set(['image/jpeg', 'image/png', 'image/gif', 'image/webp']);
  const canCapture = window.isSecureContext && !!navigator.mediaDevices?.getUserMedia &&
    !!document.createElement('canvas').toBlob && !!window.URL?.createObjectURL &&
    !!window.DataTransfer;
  if (!window.URL?.createObjectURL) return;

  let selectedFile = null;
  let previewUrl = null;
  let stream = null;
  let cameraRequest = 0;
  let choiceExpanded = false;
  let fromCamera = false;

  input.classList.add('image-file-input');
  choices.hidden = false;
  if (nativeRemove) nativeRemove.hidden = true;
  if (!canCapture) {
    takePhoto.hidden = true;
    unavailable.hidden = false;
  }

  function clearClientError() {
    error.textContent = '';
    if (!editor.querySelector('.field-validation-error')) input.removeAttribute('aria-invalid');
  }

  function showClientError(message) {
    error.textContent = message;
    input.setAttribute('aria-invalid', 'true');
  }

  function setInputFile(file) {
    if (!window.DataTransfer) {
      if (!file) input.value = '';
      return;
    }
    const transfer = new DataTransfer();
    if (file) transfer.items.add(file);
    input.files = transfer.files;
  }

  function revokePreview() {
    if (previewUrl) URL.revokeObjectURL(previewUrl);
    previewUrl = null;
  }

  function render() {
    const removingCurrent = !!removeCheckbox?.checked;
    const hasImage = !!selectedFile || (!!currentImageUrl && !removingCurrent);
    preview.hidden = !hasImage;
    removal.hidden = !removingCurrent;
    choices.hidden = hasImage && !choiceExpanded && !removingCurrent;
    input.tabIndex = choices.hidden ? -1 : 0;
    if (!hasImage) return;

    if (selectedFile) {
      previewImage.src = previewUrl;
      previewImage.alt = 'Selected image preview';
      previewStatus.textContent = editor.dataset.editing === 'true'
        ? 'New image selected; it will replace the current image when you save'
        : 'Image ready to publish';
      previewDetails.textContent = `${selectedFile.name} · ${(selectedFile.size / 1024 / 1024).toFixed(2)} MB`;
    } else {
      previewImage.src = currentImageUrl;
      previewImage.alt = 'Current report image';
      previewStatus.textContent = 'Current image';
      previewDetails.textContent = '';
    }
    retake.hidden = !selectedFile || !fromCamera || !canCapture;
    editor.querySelector('[data-remove-pending]').hidden = !selectedFile;
    editor.querySelector('[data-remove-current]').hidden = !!selectedFile;
  }

  function stopCamera(keepPanel = false) {
    cameraRequest += 1;
    video.onloadedmetadata = null;
    video.onerror = null;
    capture.disabled = true;
    for (const track of stream?.getTracks() ?? []) track.stop();
    stream = null;
    video.srcObject = null;
    if (!keepPanel) panel.hidden = true;
  }

  function cameraFailure(message, recoverable = true) {
    stopCamera(true);
    cameraStatus.textContent = message;
    retry.hidden = !recoverable;
    cameraUpload.hidden = false;
    showClientError(message);
    (recoverable ? retry : editor.querySelector('[data-cancel-camera]')).focus();
  }

  async function openCamera() {
    if (!canCapture || !panel.hidden && (stream || cameraStatus.textContent === 'Waiting for camera permission.')) return;
    stopCamera();
    const request = cameraRequest;
    clearClientError();
    panel.hidden = false;
    retry.hidden = true;
    cameraUpload.hidden = true;
    cameraStatus.textContent = 'Waiting for camera permission.';
    heading.focus();

    try {
      const opened = await navigator.mediaDevices.getUserMedia({
        audio: false,
        video: { facingMode: { ideal: 'environment' } }
      });
      if (request !== cameraRequest) {
        opened.getTracks().forEach(track => track.stop());
        return;
      }
      stream = opened;
      video.srcObject = opened;
      video.onloadedmetadata = () => {
        if (request !== cameraRequest) return;
        if (video.videoWidth > 0 && video.videoHeight > 0) {
          capture.disabled = false;
          cameraStatus.textContent = 'Camera ready.';
        }
      };
      video.onerror = () => cameraFailure('Camera could not start. Try again or upload an image.');
      await video.play();
    } catch (failure) {
      if (request !== cameraRequest) return;
      const message = failure.name === 'NotAllowedError' || failure.name === 'SecurityError'
        ? 'Camera access is blocked. Allow access in your browser settings, or upload an image instead.'
        : failure.name === 'NotFoundError'
          ? 'No camera was found on this device. You can upload an image instead.'
          : 'Camera could not start. Try again or upload an image.';
      cameraFailure(message, !['NotAllowedError', 'SecurityError', 'NotFoundError'].includes(failure.name));
    }
  }

  function validateFile(file) {
    return file.size > 0 && file.size <= maxBytes && acceptedTypes.has(file.type);
  }

  function chooseFile(file, cameraFile = false) {
    if (!validateFile(file)) {
      if (window.DataTransfer) {
        setInputFile(selectedFile);
      } else {
        revokePreview();
        selectedFile = null;
        input.value = '';
        render();
      }
      showClientError('Choose a JPEG, PNG, GIF, or WebP image under 5 MB.');
      return false;
    }
    revokePreview();
    selectedFile = file;
    previewUrl = URL.createObjectURL(file);
    fromCamera = cameraFile;
    if (removeCheckbox) removeCheckbox.checked = false;
    choiceExpanded = false;
    clearClientError();
    render();
    return true;
  }

  function encodeJpeg(canvas, quality) {
    return new Promise(resolve => canvas.toBlob(resolve, 'image/jpeg', quality));
  }

  async function capturePhoto() {
    if (!stream || capture.disabled || !video.videoWidth || !video.videoHeight) return;
    capture.disabled = true;
    let captureRequest = cameraRequest;
    try {
      const canvas = document.createElement('canvas');
      const scale = Math.min(1, Math.sqrt(12_000_000 / (video.videoWidth * video.videoHeight)));
      canvas.width = Math.max(1, Math.floor(video.videoWidth * scale));
      canvas.height = Math.max(1, Math.floor(video.videoHeight * scale));
      canvas.getContext('2d').drawImage(video, 0, 0, canvas.width, canvas.height);
      const original = document.createElement('canvas');
      original.width = canvas.width;
      original.height = canvas.height;
      original.getContext('2d').drawImage(canvas, 0, 0);
      stopCamera();
      captureRequest = cameraRequest;

      let blob;
      for (let attempt = 0; attempt < 5; attempt += 1) {
        blob = await encodeJpeg(canvas, Math.max(0.45, 0.85 - attempt * 0.1));
        if (captureRequest !== cameraRequest) return;
        if (blob?.size > 0 && blob.size <= maxBytes) break;
        if (attempt >= 2) {
          canvas.width = Math.max(1, Math.floor(canvas.width * 0.75));
          canvas.height = Math.max(1, Math.floor(canvas.height * 0.75));
          canvas.getContext('2d').drawImage(original, 0, 0, canvas.width, canvas.height);
        }
      }
      if (!blob || blob.size === 0 || blob.size > maxBytes || blob.type !== 'image/jpeg') {
        throw new Error('The captured image could not be encoded.');
      }
      const file = new File([blob], `camera-${Date.now()}.jpg`, { type: 'image/jpeg', lastModified: Date.now() });
      setInputFile(file);
      input.dispatchEvent(new Event('change', { bubbles: true }));
      fromCamera = true;
      render();
      previewStatus.focus();
    } catch {
      if (captureRequest !== cameraRequest) return;
      stopCamera();
      showClientError('The photo could not be prepared. Try again or upload an image.');
      choiceExpanded = true;
      render();
      takePhoto.focus();
    }
  }

  input.addEventListener('change', () => {
    stopCamera();
    const file = input.files?.[0];
    if (file) chooseFile(file, file.name.startsWith('camera-') && file.type === 'image/jpeg');
  });
  takePhoto.addEventListener('click', openCamera);
  retry.addEventListener('click', openCamera);
  capture.addEventListener('click', capturePhoto);
  editor.querySelector('[data-cancel-camera]').addEventListener('click', () => {
    stopCamera();
    clearClientError();
    takePhoto.focus();
  });
  editor.querySelector('[data-replace-image]').addEventListener('click', () => {
    choiceExpanded = true;
    render();
    input.focus();
  });
  editor.querySelector('[data-retake-photo]').addEventListener('click', openCamera);
  editor.querySelector('[data-remove-pending]').addEventListener('click', () => {
    stopCamera();
    revokePreview();
    selectedFile = null;
    fromCamera = false;
    setInputFile(null);
    choiceExpanded = false;
    clearClientError();
    render();
    takePhoto.hidden ? input.focus() : takePhoto.focus();
  });
  editor.querySelector('[data-remove-current]').addEventListener('click', () => {
    stopCamera();
    if (removeCheckbox) removeCheckbox.checked = true;
    render();
    editor.querySelector('[data-undo-remove]').focus();
  });
  editor.querySelector('[data-undo-remove]').addEventListener('click', () => {
    removeCheckbox.checked = false;
    render();
    previewStatus.focus();
  });
  removeCheckbox?.addEventListener('change', () => {
    if (removeCheckbox.checked) {
      revokePreview();
      selectedFile = null;
      setInputFile(null);
    }
    render();
  });
  panel.addEventListener('keydown', event => {
    if (event.key !== 'Escape') return;
    event.preventDefault();
    stopCamera();
    takePhoto.focus();
  });
  form.addEventListener('submit', () => {
    stopCamera();
    submit.disabled = true;
    submit.textContent = submit.dataset.submitLabel;
    submitStatus.textContent = submit.dataset.submitLabel;
  });
  window.addEventListener('pagehide', () => {
    stopCamera();
    revokePreview();
  });
  render();
})();
