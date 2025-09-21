(async function initOverlaysPage() {
  if (document.body.dataset.page !== 'overlays') {
    return;
  }

  const {
    state,
    api,
    showToast,
    loadActiveProfile,
    refreshStatus,
    saveProfile,
    renderMissingProfile,
  } = window.app;

  await Promise.all([loadActiveProfile(), refreshStatus()]);

  if (!state.profile) {
    renderMissingProfile('overlays-root');
    return;
  }

  if (!state.defaultsOverlays) {
    state.defaultsOverlays = await api.get('/api/defaults/overlays');
  }

  renderOverlays();

  function renderOverlays() {
    const root = document.getElementById('overlays-root');
    if (!root) {
      return;
    }

    const map = state.defaultsOverlays.mediaMap || {};
    const overlays = state.defaultsOverlays.overlays || {};

    root.innerHTML = '';

    Object.entries(map).forEach(([mediaType, ids]) => {
      const card = document.createElement('div');
      card.className = 'card';
      card.innerHTML = `<h2>${mediaType}</h2>`;

      const grid = document.createElement('div');
      grid.className = 'overlay-grid';

      ids.forEach((id) => {
        const info = overlays[id];
        if (!info) {
          return;
        }
        const key = `${id}_${mediaType.replace(/\s+/g, '_')}`;
        const enabled = !!(state.profile.overlaySettings && state.profile.overlaySettings[key]?.isEnabled);

        const item = document.createElement('div');
        item.className = 'overlay-item';
        const label = document.createElement('label');

        const checkbox = document.createElement('input');
        checkbox.type = 'checkbox';
        checkbox.checked = enabled;
        checkbox.addEventListener('change', async () => {
          try {
            await saveProfile((profile) => {
              profile.overlaySettings = profile.overlaySettings || {};
              profile.overlaySettings[key] = profile.overlaySettings[key] || {
                overlayType: id,
                mediaType,
              };
              profile.overlaySettings[key].isEnabled = checkbox.checked;
            });
          } catch (err) {
            showToast(err.message, 'error');
            checkbox.checked = !checkbox.checked;
          }
        });

        const body = document.createElement('div');
        body.innerHTML = `<strong>${info.name}</strong><br><span class="help">${info.description}</span>`;

        label.appendChild(checkbox);
        label.appendChild(body);
        item.appendChild(label);
        grid.appendChild(item);
      });

      card.appendChild(grid);
      root.appendChild(card);
    });
  }
})();
