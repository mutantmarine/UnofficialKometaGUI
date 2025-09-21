(async function initProfilePage() {
  if (document.body.dataset.page !== 'profile') {
    return;
  }

  const {
    state,
    api,
    showToast,
    loadProfiles,
    loadActiveProfile,
    refreshStatus,
  } = window.app;

  await Promise.all([loadProfiles(), loadActiveProfile(), refreshStatus()]);
  renderProfileList();
  renderProfileSummary();
  bindProfileEvents();

  function renderProfileList() {
    const list = document.getElementById('profile-list');
    if (!list) {
      return;
    }

    list.innerHTML = '';

    if (!state.profiles.length) {
      const empty = document.createElement('div');
      empty.className = 'muted';
      empty.textContent = 'No profiles found.';
      list.appendChild(empty);
      return;
    }

    state.profiles.forEach((name) => {
      const li = document.createElement('li');

      const label = document.createElement('span');
      label.textContent = name;
      if (name === state.activeProfileName) {
        const badge = document.createElement('span');
        badge.className = 'badge success';
        badge.textContent = 'Active';
        label.appendChild(document.createTextNode(' '));
        label.appendChild(badge);
      }

      const actions = document.createElement('div');
      actions.className = 'inline-actions';

      const selectBtn = document.createElement('button');
      selectBtn.textContent = 'Select';
      selectBtn.className = 'secondary';
      selectBtn.disabled = name === state.activeProfileName;
      selectBtn.addEventListener('click', async () => {
        try {
          await api.post('/api/profiles/select', { name });
          await Promise.all([loadProfiles(), loadActiveProfile(), refreshStatus()]);
          renderProfileList();
          renderProfileSummary();
          showToast(`Profile '${name}' selected.`, 'success');
        } catch (err) {
          showToast(err.message, 'error');
        }
      });

      const deleteBtn = document.createElement('button');
      deleteBtn.textContent = 'Delete';
      deleteBtn.className = 'danger';
      deleteBtn.addEventListener('click', async () => {
        if (!confirm(`Delete profile '${name}'?`)) {
          return;
        }
        try {
          await api.del(`/api/profiles/${encodeURIComponent(name)}`);
          await Promise.all([loadProfiles(), loadActiveProfile(), refreshStatus()]);
          renderProfileList();
          renderProfileSummary();
          showToast(`Profile '${name}' deleted.`, 'success');
        } catch (err) {
          showToast(err.message, 'error');
        }
      });

      actions.appendChild(selectBtn);
      actions.appendChild(deleteBtn);
      li.appendChild(label);
      li.appendChild(actions);
      list.appendChild(li);
    });
  }

  function renderProfileSummary() {
    const summary = document.getElementById('profile-summary');
    if (!summary) {
      return;
    }

    if (!state.profile) {
      summary.innerHTML = '<p class="muted">No active profile selected.</p>';
      return;
    }

    const libraries = (state.profile.selectedLibraries || []).join(', ') || 'None selected';
    const services = Object.keys(state.profile.optionalServices || {}).length;

    summary.innerHTML = `
      <div class="grid two">
        <div>
          <h3>Kometa</h3>
          <p class="muted">${state.profile.kometaDirectory || 'Not set'}</p>
        </div>
        <div>
          <h3>Plex</h3>
          <p class="muted">${state.profile.plex?.url || 'Not configured'}</p>
        </div>
        <div>
          <h3>Libraries</h3>
          <p class="muted">${libraries}</p>
        </div>
        <div>
          <h3>Optional Services</h3>
          <p class="muted">${services} configured</p>
        </div>
      </div>
    `;
  }

  function bindProfileEvents() {
    const input = document.getElementById('new-profile-name');
    const button = document.getElementById('create-profile-btn');
    if (!input || !button) {
      return;
    }

    const createProfile = async () => {
      const name = input.value.trim();
      if (!name) {
        showToast('Enter a profile name.', 'error');
        return;
      }

      try {
        await api.post('/api/profiles', { name });
        input.value = '';
        await Promise.all([loadProfiles(), loadActiveProfile(), refreshStatus()]);
        renderProfileList();
        renderProfileSummary();
        showToast(`Profile '${name}' created.`, 'success');
      } catch (err) {
        showToast(err.message, 'error');
      }
    };

    button.addEventListener('click', createProfile);
    input.addEventListener('keydown', (event) => {
      if (event.key === 'Enter') {
        event.preventDefault();
        createProfile();
      }
    });
  }
})();
