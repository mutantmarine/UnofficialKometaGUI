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

  // Import functionality
  let parsedImportData = null;

  function bindImportEvents() {
    const fileInput = document.getElementById('config-file-input');
    const parseBtn = document.getElementById('parse-config-btn');
    const confirmBtn = document.getElementById('confirm-import-btn');
    const cancelBtn = document.getElementById('cancel-import-btn');
    const targetProfileSelect = document.getElementById('import-target-profile');

    if (!fileInput || !parseBtn || !confirmBtn || !cancelBtn || !targetProfileSelect) {
      return;
    }

    // Enable parse button when file selected
    fileInput.addEventListener('change', (e) => {
      const hasFile = e.target.files && e.target.files.length > 0;
      parseBtn.disabled = !hasFile;
    });

    // Parse and preview config file
    parseBtn.addEventListener('click', async () => {
      const file = fileInput.files?.[0];
      if (!file) {
        showToast('Please select a config file.', 'error');
        return;
      }

      parseBtn.disabled = true;
      parseBtn.textContent = 'Parsing...';

      try {
        const formData = new FormData();
        formData.append('configFile', file);

        const response = await fetch('/api/profiles/import/preview', {
          method: 'POST',
          body: formData,
        });

        const result = await response.json();

        if (!response.ok) {
          throw new Error(result.error || 'Failed to parse config');
        }

        // Store parsed data for later save
        parsedImportData = result.profileData;

        // Show preview
        renderImportPreview(result.preview, result.warnings);

        // Populate profile dropdown
        populateProfileDropdown();

        // Show preview and save sections
        document.getElementById('preview-section').style.display = 'block';
        document.getElementById('save-section').style.display = 'block';

        showToast('Configuration parsed successfully!', 'success');
      } catch (err) {
        showToast(err.message, 'error');
        parsedImportData = null;
      } finally {
        parseBtn.disabled = false;
        parseBtn.textContent = 'Parse & Preview';
      }
    });

    // Enable confirm button when profile selected
    targetProfileSelect.addEventListener('change', (e) => {
      confirmBtn.disabled = !e.target.value;
    });

    // Confirm import
    confirmBtn.addEventListener('click', async () => {
      const targetProfile = targetProfileSelect.value;
      const importMode = document.getElementById('import-mode')?.value || 'replace';

      if (!targetProfile) {
        showToast('Please select a target profile.', 'error');
        return;
      }

      if (!parsedImportData) {
        showToast('No parsed configuration data available.', 'error');
        return;
      }

      const confirmMsg =
        importMode === 'replace'
          ? `This will REPLACE all settings in profile "${targetProfile}". Continue?`
          : `This will MERGE settings into profile "${targetProfile}". Continue?`;

      if (!confirm(confirmMsg)) {
        return;
      }

      confirmBtn.disabled = true;
      confirmBtn.textContent = 'Importing...';

      try {
        await api.post('/api/profiles/import/save', {
          targetProfileName: targetProfile,
          importedProfile: parsedImportData,
          overwriteMode: importMode,
        });

        // Refresh profile data
        await Promise.all([loadProfiles(), loadActiveProfile(), refreshStatus()]);
        renderProfileList();
        renderProfileSummary();

        // Reset import UI
        resetImportUI();

        showToast(`Configuration imported successfully into "${targetProfile}"!`, 'success');
      } catch (err) {
        showToast(err.message, 'error');
      } finally {
        confirmBtn.disabled = false;
        confirmBtn.textContent = 'Import Configuration';
      }
    });

    // Cancel import
    cancelBtn.addEventListener('click', () => {
      resetImportUI();
    });
  }

  function renderImportPreview(preview, warnings) {
    // Render warnings
    const warningsContainer = document.getElementById('preview-warnings');
    if (warningsContainer && warnings && warnings.length > 0) {
      warningsContainer.innerHTML = warnings
        .map(
          (w) => `
        <div class="warning-item ${w.severity}">
          <strong>${w.section}:</strong> ${w.message}
        </div>
      `
        )
        .join('');
    }

    // Render services
    const servicesDiv = document.getElementById('preview-services');
    if (servicesDiv) {
      servicesDiv.innerHTML = `
      <p class="preview-summary">
        <strong>Plex:</strong> ${preview.plexUrl || 'Not configured'}<br>
        <strong>TMDb:</strong> ${preview.hasTMDbKey ? 'Configured' : 'Missing'}<br>
        <strong>Services:</strong> ${preview.enabledServices?.length || 0}
      </p>
      ${
        preview.enabledServices && preview.enabledServices.length > 0
          ? `
        <div class="preview-list">
          ${preview.enabledServices.map((s) => `<div class="preview-list-item">- ${s}</div>`).join('')}
        </div>
      `
          : ''
      }
    `;
    }

    // Render libraries
    const librariesDiv = document.getElementById('preview-libraries');
    if (librariesDiv) {
      librariesDiv.innerHTML = `
      <p class="preview-summary"><strong>Count:</strong> ${preview.libraryCount || 0}</p>
      ${
        preview.libraryNames && preview.libraryNames.length > 0
          ? `
        <div class="preview-list">
          ${preview.libraryNames.map((l) => `<div class="preview-list-item">- ${l}</div>`).join('')}
        </div>
      `
          : '<p class="muted">No libraries found</p>'
      }
    `;
    }

    // Render collections
    const collectionsDiv = document.getElementById('preview-collections');
    if (collectionsDiv) {
      collectionsDiv.innerHTML = `
      <p class="preview-summary"><strong>Count:</strong> ${preview.collectionCount || 0}</p>
      ${
        preview.collectionTypes && preview.collectionTypes.length > 0
          ? `
        <div class="preview-list">
          ${preview.collectionTypes
            .slice(0, 10)
            .map((c) => `<div class="preview-list-item">- ${c}</div>`)
            .join('')}
          ${
            preview.collectionTypes.length > 10
              ? `<div class="preview-list-item muted">...and ${preview.collectionTypes.length - 10} more</div>`
              : ''
          }
        </div>
      `
          : '<p class="muted">No collections found</p>'
      }
    `;
    }

    // Render overlays
    const overlaysDiv = document.getElementById('preview-overlays');
    if (overlaysDiv) {
      const builderLevelSummary = preview.overlaysByBuilderLevel
        ? Object.entries(preview.overlaysByBuilderLevel)
            .map(([level, count]) => `${level}: ${count}`)
            .join(', ')
        : '';

      overlaysDiv.innerHTML = `
      <p class="preview-summary">
        <strong>Count:</strong> ${preview.overlayCount || 0}
        ${builderLevelSummary ? `<br><strong>Levels:</strong> ${builderLevelSummary}` : ''}
      </p>
      ${
        preview.overlayTypes && preview.overlayTypes.length > 0
          ? `
        <div class="preview-list">
          ${preview.overlayTypes
            .slice(0, 10)
            .map((o) => `<div class="preview-list-item">- ${o}</div>`)
            .join('')}
          ${
            preview.overlayTypes.length > 10
              ? `<div class="preview-list-item muted">...and ${preview.overlayTypes.length - 10} more</div>`
              : ''
          }
        </div>
      `
          : '<p class="muted">No overlays found</p>'
      }
    `;
    }
  }

  function populateProfileDropdown() {
    const select = document.getElementById('import-target-profile');
    if (!select) return;

    select.innerHTML = '<option value="">-- Select Profile --</option>';

    state.profiles.forEach((name) => {
      const option = document.createElement('option');
      option.value = name;
      option.textContent = name;
      select.appendChild(option);
    });

    // Add "Create New Profile" option
    const newOption = document.createElement('option');
    newOption.value = '_new_';
    newOption.textContent = '+ Create New Profile';
    select.appendChild(newOption);

    // Handle new profile creation
    select.addEventListener('change', async (e) => {
      if (e.target.value === '_new_') {
        const newName = prompt('Enter new profile name:');
        if (newName && newName.trim()) {
          try {
            await api.post('/api/profiles', { name: newName.trim() });
            await loadProfiles();
            populateProfileDropdown();
            select.value = newName.trim();
            showToast(`Profile "${newName}" created.`, 'success');
          } catch (err) {
            showToast(err.message, 'error');
            select.value = '';
          }
        } else {
          select.value = '';
        }
      }
    });
  }

  function resetImportUI() {
    const fileInput = document.getElementById('config-file-input');
    const parseBtn = document.getElementById('parse-config-btn');
    const targetProfileSelect = document.getElementById('import-target-profile');
    const confirmBtn = document.getElementById('confirm-import-btn');

    if (fileInput) fileInput.value = '';
    if (parseBtn) parseBtn.disabled = true;
    if (targetProfileSelect) targetProfileSelect.value = '';
    if (confirmBtn) confirmBtn.disabled = true;

    document.getElementById('preview-section').style.display = 'none';
    document.getElementById('save-section').style.display = 'none';

    parsedImportData = null;
  }

  // Initialize import events
  bindImportEvents();
})();
