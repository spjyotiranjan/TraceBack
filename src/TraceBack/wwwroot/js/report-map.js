(() => {
  function safeAttributionHtml(value) {
    const source = document.createElement('template');
    source.innerHTML = value;
    const safe = document.createElement('div');
    const discarded = new Set(['SCRIPT', 'STYLE', 'TEMPLATE', 'IFRAME', 'OBJECT', 'EMBED', 'SVG', 'MATH']);

    function appendSafeContent(node, parent) {
      if (node.nodeType === Node.TEXT_NODE) {
        parent.appendChild(document.createTextNode(node.textContent));
        return;
      }
      if (node.nodeType !== Node.ELEMENT_NODE || discarded.has(node.tagName)) return;

      if (node.tagName === 'A') {
        let url;
        try { url = new URL(node.getAttribute('href') || ''); } catch { /* Invalid links become plain text. */ }
        if (url && (url.protocol === 'https:' || url.protocol === 'http:')) {
          const link = document.createElement('a');
          link.href = url.href;
          link.rel = 'noopener noreferrer';
          link.textContent = node.textContent;
          parent.appendChild(link);
          return;
        }
      }

      for (const child of node.childNodes) appendSafeContent(child, parent);
    }

    for (const child of source.content.childNodes) appendSafeContent(child, safe);
    return safe.innerHTML;
  }

  const tileTimeoutMs = 12000;
  let leafletLoad;

  function reloadLeaflet(src) {
    // The originally deferred local asset could fail independently of the tiles.
    leafletLoad ??= new Promise((resolve, reject) => {
      const script = document.createElement('script');
      script.src = src;
      const timer = setTimeout(() => {
        script.remove();
        reject(new Error('Leaflet load timed out'));
      }, tileTimeoutMs);
      script.onload = () => { clearTimeout(timer); resolve(); };
      script.onerror = () => {
        clearTimeout(timer);
        script.remove();
        reject(new Error('Leaflet load failed'));
      };
      document.head.append(script);
    }).finally(() => { leafletLoad = undefined; });
    return leafletLoad;
  }

  function coordinate(value, limit) {
    const text = value?.trim() || '';
    if (!/^[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:e[+-]?\d+)?$/i.test(text)) return null;
    const number = Number(text);
    const rounded = Number(number.toFixed(6));
    return Math.abs(number) <= limit && Math.abs(number - rounded) <= 1e-9 ? rounded : null;
  }

  function readPoint(latitude, longitude) {
    const lat = coordinate(latitude, 90);
    const lng = coordinate(longitude, 180);
    return lat === null || lng === null ? null : [lat, lng];
  }

  function mount(root) {
    const editor = root.dataset.reportMap === 'editor';
    const canvas = root.querySelector('[data-map-canvas]');
    const status = root.querySelector('[data-map-status]');
    const retry = root.querySelector('[data-map-retry]');
    const fields = editor ? root.closest('fieldset') : null;
    const lat = fields?.querySelector('[data-map-latitude]');
    const lng = fields?.querySelector('[data-map-longitude]');
    const clear = fields?.querySelector('[data-map-clear]');
    const selection = fields?.querySelector('[data-map-selection]');
    const selectionHelp = fields?.querySelector('[data-map-selection-help]');
    const disclosure = fields?.querySelector('[data-map-coordinates]');
    const coordinateValue = fields?.querySelector('[data-map-coordinate-value]');
    const selectionActions = fields?.querySelector('[data-map-selection-actions]');
    const externalMap = fields?.querySelector('[data-map-external]');
    const { tileUrl, tileAttribution, leafletSrc } = root.dataset;
    const maxZoom = Math.min(19, Math.max(2, Number.parseInt(root.dataset.maxZoom, 10) || 18));
    const attribution = safeAttributionHtml(tileAttribution || '');
    const loading = editor ? 'Loading map…' : 'Loading map preview…';
    const unavailable = editor
      ? 'Map tiles unavailable. You can still enter coordinates manually.'
      : 'Map preview unavailable. Saved coordinates and the Google Maps link are still available.';
    let point = editor ? readPoint(lat.value, lng.value) : readPoint(root.dataset.latitude, root.dataset.longitude);
    const savedPoint = fields?.querySelector('[data-map-saved-pin="true"]') ? point : null;
    let map;
    let marker;
    let tiles;
    let tileTimer;
    let state;

    function syncDragging() {
      if (editor && marker?.dragging) {
        if (state === 'ready') marker.dragging.enable();
        else marker.dragging.disable();
      }
    }

    function showState(next, message = '') {
      state = next;
      root.dataset.mapState = next;
      canvas.setAttribute('aria-busy', String(next === 'loading'));
      status.textContent = message;
      retry.hidden = next !== 'unavailable';
      syncDragging();
    }

    function syncMarker(recenter) {
      if (!map) return;
      if (!point) {
        marker?.remove();
        marker = undefined;
        return;
      }
      if (marker) marker.setLatLng(point);
      else {
        marker = window.L.marker(point, {
          keyboard: false,
          draggable: editor,
          alt: editor ? 'Selected report location' : 'Saved report location',
        }).addTo(map);
        if (editor) marker.on('dragend', () => {
          if (!point) return;
          if (state === 'ready') selectOnMap(marker.getLatLng());
          else marker.setLatLng(point);
        });
      }
      syncDragging();
      if (recenter) map.setView(point, Math.min(maxZoom, Math.max(map.getZoom(), 13)), { animate: false });
    }

    function syncSelection() {
      const hasText = !!(lat.value.trim() || lng.value.trim());
      const unchanged = point && savedPoint && point.every((value, index) => value === savedPoint[index]);
      selection.closest('.map-selection').toggleAttribute('data-map-has-point', !!point);
      selection.textContent = point ? (unchanged ? 'Saved map pin' : 'Map pin selected') : 'Choose a point';
      selectionHelp.textContent = hasText && !readPoint(lat.value, lng.value)
        ? (point ? 'Correct both coordinates to move the displayed pin.' : 'Enter both valid coordinates to place the pin.')
        : point ? 'Selected point, not a verified address.'
          : 'Select the map or enter coordinates below.';
      if (point && disclosure.hidden) disclosure.open = true;
      disclosure.hidden = !point;
      if (!point) disclosure.open = false;
      const coordinates = point?.map(value => value.toFixed(6));
      coordinateValue.textContent = coordinates ? coordinates.join(', ') : '';
      selectionActions.hidden = !coordinates;
      if (coordinates) externalMap.href = `https://www.google.com/maps/search/?api=1&query=${coordinates.join('%2C')}`;
      else externalMap.removeAttribute('href');
      if (clear) clear.hidden = !hasText;
    }

    function selectOnMap(position) {
      const wrapped = position.wrap();
      lat.value = wrapped.lat.toFixed(6);
      lng.value = wrapped.lng.toFixed(6);
      syncFields();
    }

    function syncFields() {
      const next = readPoint(lat.value, lng.value);
      if (next || (!lat.value.trim() && !lng.value.trim())) {
        point = next;
        syncMarker(true);
      }
      syncSelection();
    }

    function addTiles() {
      clearTimeout(tileTimer);
      // Leaflet detaches map event handlers during the layer's remove event.
      tiles?.remove().off();
      showState('loading', loading);
      const layer = window.L.tileLayer(tileUrl, { attribution, maxZoom });
      tiles = layer;
      let failed = false;
      const fail = () => {
        if (layer !== tiles || failed) return;
        failed = true;
        clearTimeout(tileTimer);
        showState('unavailable', unavailable);
      };
      function beginLoading() {
        if (layer !== tiles || failed) return;
        if (state !== 'loading') showState('loading', loading);
        clearTimeout(tileTimer);
        tileTimer = setTimeout(fail, tileTimeoutMs);
      }
      layer.on('loading', beginLoading);
      layer.on('tileerror', fail);
      layer.on('load', () => {
        if (layer !== tiles || failed) return;
        clearTimeout(tileTimer);
        showState('ready');
      });
      beginLoading();
      layer.addTo(map);
    }

    async function start(retrying = false) {
      const restoreFocus = retrying && document.activeElement === retry;
      showState('loading', loading);
      if (restoreFocus) canvas.focus({ preventScroll: true });
      try {
        if (!window.L && retrying) await reloadLeaflet(leafletSrc);
        if (!window.L) throw new Error('Leaflet unavailable');
        if (!map) {
          map = window.L.map(canvas, {
            center: point || [20, 0],
            zoom: point ? Math.min(maxZoom, 15) : 2,
            minZoom: 2,
            maxZoom,
            dragging: editor,
            scrollWheelZoom: false,
            touchZoom: editor,
            doubleClickZoom: false,
            boxZoom: editor,
            keyboard: editor,
            zoomControl: editor,
            worldCopyJump: editor,
          });
          if (editor) map.on('click', event => {
            if (state === 'ready') selectOnMap(event.latlng);
          });
          syncMarker(false);
        }
        addTiles();
      } catch {
        clearTimeout(tileTimer);
        showState('unavailable', editor
          ? 'Map unavailable. You can still enter coordinates manually.' : unavailable);
      }
    }

    if (editor) {
      // These inputs preview a point, but MVC binding and MapLocationRules validate the post.
      for (const input of [lat, lng]) input.addEventListener('input', syncFields);
      clear?.addEventListener('click', () => {
        lat.value = lng.value = '';
        syncFields();
        lat.focus();
      });
      syncSelection();
    }
    retry.addEventListener('click', () => start(true));
    start();
  }

  document.querySelectorAll('[data-report-map]').forEach(mount);
})();
