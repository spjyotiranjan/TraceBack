(() => {
  const mapRoot = document.querySelector('[data-report-map]');
  if (!mapRoot) return;

  const canvas = mapRoot.querySelector('[data-map-canvas]');
  const status = mapRoot.querySelector('[data-map-status]');
  const retry = mapRoot.querySelector('[data-map-retry]');
  const isEditor = mapRoot.dataset.reportMap === 'editor';
  const latitudeInput = isEditor ? document.getElementById('latitude') : null;
  const longitudeInput = isEditor ? document.getElementById('longitude') : null;
  const selection = isEditor ? document.querySelector('[data-map-selection]') : null;
  const error = isEditor ? document.querySelector('[data-map-error]') : null;
  const clear = isEditor ? mapRoot.querySelector('[data-map-clear]') : null;
  const maxZoom = Math.min(19, Math.max(2, Number.parseInt(mapRoot.dataset.maxZoom, 10) || 18));
  const tileUrl = mapRoot.dataset.tileUrl;
  const attribution = mapRoot.dataset.tileAttribution;
  let map;
  let marker;
  let tileLayer;
  let selectedPoint;

  function parsedCoordinate(value, min, max) {
    const text = value.trim();
    if (!/^[+-]?(?:\d+(?:\.\d*)?|\.\d+)$/.test(text)) return null;
    if ((text.split('.')[1] || '').length > 6) return null;
    const number = Number(text);
    return Number.isFinite(number) && number >= min && number <= max ? number : null;
  }

  function readFields() {
    const latitudeText = latitudeInput.value.trim();
    const longitudeText = longitudeInput.value.trim();
    if (!latitudeText && !longitudeText) return { empty: true };
    const latitude = parsedCoordinate(latitudeText, -90, 90);
    const longitude = parsedCoordinate(longitudeText, -180, 180);
    return latitude === null || longitude === null
      ? { invalid: true, field: latitude === null ? latitudeInput : longitudeInput }
      : { point: [latitude, longitude] };
  }

  function showStatus(message, unavailable = false) {
    status.textContent = message;
    retry.hidden = !unavailable;
    mapRoot.classList.toggle('map-unavailable', unavailable);
  }

  function showSelection(point) {
    selection.textContent = point
      ? `Selected location: ${point[0].toFixed(6)}, ${point[1].toFixed(6)}.`
      : 'No map location selected.';
  }

  function showError(message, field) {
    error.textContent = message;
    for (const input of [latitudeInput, longitudeInput]) {
      if (message) input.setAttribute('aria-invalid', 'true');
      else input.removeAttribute('aria-invalid');
    }
    if (field) field.focus();
  }

  function setPoint(point, updateFields = false) {
    selectedPoint = point;
    if (isEditor) {
      if (updateFields) {
        latitudeInput.value = point[0].toFixed(6);
        longitudeInput.value = point[1].toFixed(6);
      }
      showSelection(point);
      showError('');
    }
    if (!map) return;
    if (!marker) {
      marker = L.marker(point, { draggable: isEditor, keyboard: false }).addTo(map);
      if (isEditor) {
        marker.on('dragend', () => {
          const position = marker.getLatLng().wrap();
          setPoint([Number(position.lat.toFixed(6)), Number(position.lng.toFixed(6))], true);
        });
      }
    } else {
      marker.setLatLng(point);
    }
    if (isEditor) map.setView(point, Math.max(map.getZoom(), 13));
  }

  function addTiles() {
    if (tileLayer) map.removeLayer(tileLayer);
    showStatus(isEditor ? 'Loading map…' : 'Loading map preview…');
    tileLayer = L.tileLayer(tileUrl, { attribution, maxZoom });
    let tileFailed = false;
    tileLayer.once('tileload', () => { if (!tileFailed) showStatus(''); });
    tileLayer.on('tileerror', () => {
      tileFailed = true;
      showStatus(isEditor
        ? 'Map tiles unavailable. Enter coordinates to keep choosing a location.'
        : 'Map preview unavailable. The location and Google Maps link remain available.', true);
    });
    tileLayer.addTo(map);
  }

  function initializeMap() {
    if (!window.L) {
      showStatus(isEditor
        ? 'Map unavailable. Enter latitude and longitude to choose a location.'
        : 'Map preview unavailable. The location and Google Maps link remain available.', true);
      return;
    }
    if (map) {
      addTiles();
      return;
    }
    const center = selectedPoint || [20, 0];
    map = L.map(canvas, {
      center,
      zoom: selectedPoint ? 15 : 2,
      maxZoom,
      dragging: isEditor,
      scrollWheelZoom: isEditor,
      touchZoom: isEditor,
      doubleClickZoom: isEditor,
      boxZoom: isEditor,
      keyboard: isEditor,
      zoomControl: isEditor
    });
    if (isEditor) {
      map.on('click', event => {
        const position = event.latlng.wrap();
        setPoint([Number(position.lat.toFixed(6)), Number(position.lng.toFixed(6))], true);
      });
    }
    addTiles();
    if (selectedPoint) setPoint(selectedPoint);
  }

  retry.addEventListener('click', initializeMap);

  if (isEditor) {
    const initial = readFields();
    if (initial.point) setPoint(initial.point);
    for (const input of [latitudeInput, longitudeInput]) {
      input.addEventListener('input', () => {
        const value = readFields();
        if (value.point) setPoint(value.point);
        else if (value.empty) showSelection(null);
        else selection.textContent = 'Coordinates need correction before the pin can move.';
        showError('');
      });
    }
    clear?.addEventListener('click', () => {
      latitudeInput.value = '';
      longitudeInput.value = '';
      selectedPoint = null;
      if (marker) {
        map.removeLayer(marker);
        marker = null;
      }
      showSelection(null);
      showError('');
      latitudeInput.focus();
    });
    mapRoot.querySelector('[data-map-change]').addEventListener('click', () => {
      if (map) canvas.focus();
      else latitudeInput.focus();
      showStatus(map
        ? 'Click or tap the map to move the pin, or enter coordinates above.'
        : 'Enter latitude and longitude to move the pin.');
    });
    const form = mapRoot.closest('form');
    form.addEventListener('submit', event => {
      const value = readFields();
      if (value.point || (value.empty && mapRoot.dataset.pointRequired !== 'true')) return;
      event.preventDefault();
      showError(value.empty
        ? 'Choose a location on the map or enter latitude and longitude.'
        : 'Enter valid latitude and longitude with at most six decimal places.',
      value.field || latitudeInput);
    }, { capture: true });
    if (document.querySelector('#map-location-error .field-validation-error')) {
      latitudeInput.setAttribute('aria-invalid', 'true');
      longitudeInput.setAttribute('aria-invalid', 'true');
    }
  } else {
    const latitude = parsedCoordinate(mapRoot.dataset.latitude, -90, 90);
    const longitude = parsedCoordinate(mapRoot.dataset.longitude, -180, 180);
    if (latitude !== null && longitude !== null) selectedPoint = [latitude, longitude];
  }

  initializeMap();
})();
