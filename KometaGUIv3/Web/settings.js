(async function initSettingsPage() {
  if (document.body.dataset.page !== 'settings') {
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
    renderMissingProfile('settings-root');
    return;
  }

  renderSettings();
  bindEvents();

  function renderSettings() {
    const settings = state.profile.settings || {};
    setValue('sync-mode', settings.syncMode);
    setValue('minimum-items', settings.minimumItems);
    setChecked('delete-below', settings.deleteBelowMinimum);
    setValue('run-again-delay', settings.runAgainDelay);
    setChecked('enable-cache', settings.cache !== false);
    setValue('overlay-filetype', settings.overlayArtworkFiletype);
    setValue('overlay-quality', settings.overlayArtworkQuality);
  }

  function setValue(id, value) {
    const el = document.getElementById(id);
    if (el) {
      el.value = value ?? '';
    }
  }

  function setChecked(id, value) {
    const el = document.getElementById(id);
    if (el) {
      el.checked = !!value;
    }
  }

  function bindEvents() {
    handleChange('sync-mode', (profile, value) => {
      profile.settings = profile.settings || {};
      profile.settings.syncMode = value;
    });

    handleChange('minimum-items', (profile, value) => {
      profile.settings = profile.settings || {};
      profile.settings.minimumItems = parseInt(value, 10) || 0;
    });

    handleToggle('delete-below', (profile, checked) => {
      profile.settings = profile.settings || {};
      profile.settings.deleteBelowMinimum = checked;
    });

    handleChange('run-again-delay', (profile, value) => {
      profile.settings = profile.settings || {};
      profile.settings.runAgainDelay = parseInt(value, 10) || 0;
    });

    handleToggle('enable-cache', (profile, checked) => {
      profile.settings = profile.settings || {};
      profile.settings.cache = checked;
    });

    handleChange('overlay-filetype', (profile, value) => {
      profile.settings = profile.settings || {};
      profile.settings.overlayArtworkFiletype = value;
    });

    handleChange('overlay-quality', (profile, value) => {
      profile.settings = profile.settings || {};
      profile.settings.overlayArtworkQuality = parseInt(value, 10) || 90;
    });
  }

  function handleChange(id, mutator) {
    const input = document.getElementById(id);
    if (!input) {
      return;
    }
    input.addEventListener('change', async () => {
      try {
        await saveProfile((profile) => mutator(profile, input.value), 'Settings saved.');
      } catch (err) {
        showToast(err.message, 'error');
        renderSettings();
      }
    });
  }

  function handleToggle(id, mutator) {
    const input = document.getElementById(id);
    if (!input) {
      return;
    }
    input.addEventListener('change', async () => {
      try {
        await saveProfile((profile) => mutator(profile, input.checked), 'Settings saved.');
      } catch (err) {
        showToast(err.message, 'error');
        renderSettings();
      }
    });
  }
})();
