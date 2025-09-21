(async function initCollectionsPage() {
  if (document.body.dataset.page !== 'collections') {
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
    renderMissingProfile('collections-root');
    return;
  }

  if (!state.defaultsCollections) {
    state.defaultsCollections = await api.get('/api/defaults/collections');
  }

  renderCollections();

  function renderCollections() {
    const root = document.getElementById('collections-root');
    if (!root) {
      return;
    }

    const sections = [
      { label: 'Chart Collections', data: state.defaultsCollections.charts },
      { label: 'Awards', data: state.defaultsCollections.awards },
      { label: 'Movies', data: state.defaultsCollections.movies },
      { label: 'TV Shows', data: state.defaultsCollections.shows },
      { label: 'Movies & TV', data: state.defaultsCollections.both },
    ];

    root.innerHTML = '';

    sections.forEach((section) => {
      const card = document.createElement('div');
      card.className = 'card';
      card.innerHTML = `<h2>${section.label}</h2>`;

      Object.entries(section.data || {}).forEach(([categoryName, items]) => {
        const heading = document.createElement('h3');
        heading.textContent = categoryName;
        heading.style.marginTop = '1rem';
        card.appendChild(heading);

        const grid = document.createElement('div');
        grid.className = 'overlay-grid';

        items.forEach((item) => {
          const key = item.id;
          const enabled = !!(state.profile.selectedCharts && state.profile.selectedCharts[key]);

          const block = document.createElement('div');
          block.className = 'overlay-item';

          const label = document.createElement('label');
          const checkbox = document.createElement('input');
          checkbox.type = 'checkbox';
          checkbox.checked = enabled;
          checkbox.addEventListener('change', async () => {
            try {
              await saveProfile((profile) => {
                profile.selectedCharts = profile.selectedCharts || {};
                profile.selectedCharts[key] = checkbox.checked;
              });
            } catch (err) {
              showToast(err.message, 'error');
              checkbox.checked = !checkbox.checked;
            }
          });

          const description = document.createElement('div');
          description.innerHTML = `<strong>${item.name}</strong><br><span class="help">${item.description}</span>`;

          label.appendChild(checkbox);
          label.appendChild(description);
          block.appendChild(label);
          grid.appendChild(block);
        });

        card.appendChild(grid);
      });

      root.appendChild(card);
    });
  }
})();
