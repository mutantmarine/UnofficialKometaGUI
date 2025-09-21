(async function initConnectionsPage() {
  if (document.body.dataset.page !== 'connections') {
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
    renderMissingProfile('connections-root');
    return;
  }

  renderConnections();
  bindEvents();

  function renderConnections() {
    setValue('kometa-directory', state.profile.kometaDirectory);
    setValue('plex-token', state.profile.plex?.token);
    setValue('plex-url', state.profile.plex?.url);
    setValue('tmdb-api-key', state.profile.tmDb?.apiKey || state.profile.tMDb?.apiKey);

    renderLibraryList();
  }

  function setValue(id, value) {
    const input = document.getElementById(id);
    if (input) {
      input.value = value ?? '';
    }
  }

  function renderLibraryList() {
    const container = document.getElementById('library-container');
    if (!container) {
      return;
    }

    const libraries = state.profile.plex?.availableLibraries || [];
    const selected = new Set(state.profile.selectedLibraries || []);

    container.innerHTML = '';

    if (!libraries.length) {
      container.innerHTML = '<p class="muted">No libraries loaded yet. Authenticate with Plex and fetch libraries.</p>';
      return;
    }

    libraries.forEach((library) => {
      const item = document.createElement('label');
      item.className = 'library-item';

      const checkbox = document.createElement('input');
      checkbox.type = 'checkbox';
      checkbox.checked = library.isSelected || selected.has(library.name);
      checkbox.addEventListener('change', async () => {
        try {
          await saveProfile((profile) => {
            profile.selectedLibraries = profile.selectedLibraries || [];
            const set = new Set(profile.selectedLibraries);
            if (checkbox.checked) {
              set.add(library.name);
            } else {
              set.delete(library.name);
            }
            profile.selectedLibraries = Array.from(set);
            profile.plex = profile.plex || {};
            profile.plex.availableLibraries = (profile.plex.availableLibraries || []).map((lib) =>
              lib.name === library.name ? { ...lib, isSelected: checkbox.checked } : lib,
            );
          }, 'Libraries updated.');
        } catch (err) {
          showToast(err.message, 'error');
          checkbox.checked = !checkbox.checked;
        }
      });

      const meta = document.createElement('div');
      meta.innerHTML = `<strong>${library.name}</strong><br><span class="muted">${library.type || 'Unknown type'}</span>`;

      item.appendChild(checkbox);
      item.appendChild(meta);
      container.appendChild(item);
    });
  }

  function bindEvents() {
    handleClick('save-directory', async () => {
      const directory = document.getElementById('kometa-directory').value.trim();
      await saveProfile((profile) => {
        profile.kometaDirectory = directory;
      }, 'Kometa directory saved.');
    });

    handleClick('save-plex-token', async () => {
      const token = document.getElementById('plex-token').value.trim();
      await saveProfile((profile) => {
        profile.plex = profile.plex || {};
        profile.plex.token = token;
        profile.plex.isAuthenticated = !!token;
      }, 'Plex token saved.');
    });

    handleClick('validate-plex-token', async () => {
      const token = document.getElementById('plex-token').value.trim();
      if (!token) {
        showToast('Enter a Plex token first.', 'error');
        return;
      }
      try {
        const { isValid } = await api.post('/api/plex/validate-token', { token });
        showToast(isValid ? 'Token valid.' : 'Token invalid.', isValid ? 'success' : 'error');
      } catch (err) {
        showToast(err.message, 'error');
      }
    });

    handleClick('start-plex-oauth', async () => {
      try {
        const result = await api.post('/api/plex/oauth/start');
        if (result?.token) {
          document.getElementById('plex-token').value = result.token;
          await saveProfile((profile) => {
            profile.plex = profile.plex || {};
            profile.plex.token = result.token;
            profile.plex.isAuthenticated = true;
          }, 'Plex token retrieved.');
        } else {
          showToast('Complete authentication in the opened browser window.', 'info');
        }
      } catch (err) {
        showToast(err.message, 'error');
      }
    });

    handleClick('cancel-plex-oauth', async () => {
      try {
        await api.post('/api/plex/oauth/cancel');
        showToast('Authentication cancelled.', 'info');
      } catch (err) {
        showToast(err.message, 'error');
      }
    });

    handleClick('fetch-servers', async () => {
      const token = document.getElementById('plex-token').value.trim();
      if (!token) {
        showToast('Enter a Plex token first.', 'error');
        return;
      }
      try {
        const servers = await api.post('/api/plex/servers', { token });
        populateServers(servers);
        showToast(`${servers.length} server(s) detected.`, 'success');
      } catch (err) {
        showToast(err.message, 'error');
      }
    });

    handleClick('save-plex-url', async () => {
      const url = document.getElementById('plex-url').value.trim();
      if (!url) {
        showToast('Enter a Plex URL.', 'error');
        return;
      }
      await saveProfile((profile) => {
        profile.plex = profile.plex || {};
        profile.plex.url = url;
        profile.plex.isManualMode = true;
      }, 'Plex URL saved.');
    });

    const serverSelect = document.getElementById('plex-server-select');
    if (serverSelect) {
      serverSelect.addEventListener('change', async () => {
        const value = serverSelect.value;
        if (!value) {
          return;
        }
        document.getElementById('plex-url').value = value;
        await saveProfile((profile) => {
          profile.plex = profile.plex || {};
          profile.plex.url = value;
          profile.plex.isManualMode = false;
        }, 'Plex server selected.');
      });
    }

    handleClick('fetch-libraries', async () => {
      const token = document.getElementById('plex-token').value.trim();
      const serverUrl = document.getElementById('plex-url').value.trim();
      if (!token || !serverUrl) {
        showToast('Token and server URL are required to load libraries.', 'error');
        return;
      }
      try {
        const libraries = await api.post('/api/plex/libraries', { token, serverUrl });
        await saveProfile((profile) => {
          profile.plex = profile.plex || {};
          profile.plex.availableLibraries = libraries.map((lib) => ({
            ...lib,
            isSelected: (profile.selectedLibraries || []).includes(lib.name),
          }));
        }, 'Libraries refreshed.');
        renderConnections();
      } catch (err) {
        showToast(err.message, 'error');
      }
    });

    handleClick('save-tmdb-api-key', async () => {
      const value = document.getElementById('tmdb-api-key').value.trim();
      await saveProfile((profile) => {
        profile.tmDb = profile.tmDb || profile.tMDb || {};
        profile.tmDb.apiKey = value;
      }, 'TMDb API key saved.');
    });
  }

  function handleClick(id, handler) {
    const button = document.getElementById(id);
    if (!button) {
      return;
    }
    button.addEventListener('click', async () => {
      button.disabled = true;
      try {
        await handler();
      } catch (err) {
        showToast(err.message, 'error');
      } finally {
        button.disabled = false;
      }
    });
  }

  function populateServers(servers) {
    const select = document.getElementById('plex-server-select');
    if (!select) {
      return;
    }
    select.innerHTML = '<option value="">Select a server</option>';
    servers.forEach((server) => {
      const option = document.createElement('option');
      const url = server.url || server.address || server.host || '';
      option.value = server.getUrl ? server.getUrl : url;
      option.textContent = `${server.name || 'Server'} (${url || 'unknown'})`;
      select.appendChild(option);
    });
  }
})();
