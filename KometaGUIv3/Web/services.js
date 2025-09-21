(async function initServicesPage() {
  if (document.body.dataset.page !== 'services') {
    return;
  }

  const {
    state,
    showToast,
    loadActiveProfile,
    refreshStatus,
    saveProfile,
    renderMissingProfile,
  } = window.app;

  await Promise.all([loadActiveProfile(), refreshStatus()]);

  if (!state.profile) {
    renderMissingProfile('services-root');
    return;
  }

  renderTable();
  bindEvents();

  function renderTable() {
    const body = document.getElementById('services-table-body');
    if (!body) {
      return;
    }

    const entries = Object.entries(state.profile.optionalServices || {});
    if (!entries.length) {
      body.innerHTML = '<tr><td colspan="3" class="muted">No optional services configured.</td></tr>';
      return;
    }

    body.innerHTML = '';
    entries.forEach(([key, value]) => {
      const row = document.createElement('tr');

      const keyCell = document.createElement('td');
      keyCell.textContent = key;

      const valueCell = document.createElement('td');
      const input = document.createElement('input');
      input.type = 'text';
      input.value = value;
      input.addEventListener('change', async () => {
        try {
          await saveProfile((profile) => {
            profile.optionalServices = profile.optionalServices || {};
            profile.optionalServices[key] = input.value.trim();
          }, 'Optional services updated.');
        } catch (err) {
          showToast(err.message, 'error');
        }
      });
      valueCell.appendChild(input);

      const actionCell = document.createElement('td');
      const remove = document.createElement('button');
      remove.className = 'danger';
      remove.textContent = 'Remove';
      remove.addEventListener('click', async () => {
        if (!confirm(`Remove '${key}'?`)) {
          return;
        }
        try {
          await saveProfile((profile) => {
            profile.optionalServices = profile.optionalServices || {};
            delete profile.optionalServices[key];
          }, 'Optional service removed.');
          renderTable();
        } catch (err) {
          showToast(err.message, 'error');
        }
      });
      actionCell.appendChild(remove);

      row.appendChild(keyCell);
      row.appendChild(valueCell);
      row.appendChild(actionCell);
      body.appendChild(row);
    });
  }

  function bindEvents() {
    const addBtn = document.getElementById('add-service');
    if (!addBtn) {
      return;
    }

    addBtn.addEventListener('click', async () => {
      const key = prompt('Enter the service key (e.g. radarr_url, trakt_client_id):');
      if (!key) {
        return;
      }
      if (state.profile.optionalServices && state.profile.optionalServices[key]) {
        showToast('That key already exists.', 'error');
        return;
      }
      try {
        await saveProfile((profile) => {
          profile.optionalServices = profile.optionalServices || {};
          profile.optionalServices[key] = '';
        }, 'Optional service added.');
        renderTable();
      } catch (err) {
        showToast(err.message, 'error');
      }
    });
  }
})();
