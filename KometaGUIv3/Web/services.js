const serviceDefinitions = {
  tautulli: {
    group: 'local',
    name: 'Tautulli',
    description: 'Statistics and monitoring for Plex',
    fields: [
      {
        key: 'tautulli_url',
        label: 'URL',
        placeholder: (ctx) => `http://${ctx.plexHost || '192.168.1.12'}:8181`,
      },
      { key: 'tautulli_key', label: 'API Key', type: 'password' },
    ],
  },
  radarr: {
    group: 'local',
    name: 'Radarr',
    description: 'Movie collection manager',
    fields: [
      {
        key: 'radarr_url',
        label: 'URL',
        placeholder: (ctx) => `http://${ctx.plexHost || '192.168.1.12'}:7878`,
      },
      { key: 'radarr_key', label: 'API Key', type: 'password' },
    ],
    advanced: {
      title: 'Radarr Advanced Configuration',
      fields: [
        { key: 'radarr_root_folder_path', label: 'Root Folder Path', placeholder: 'S:/Movies' },
        { key: 'radarr_quality_profile', label: 'Quality Profile', placeholder: 'HD-1080p' },
        { key: 'radarr_monitor', label: 'Monitor', placeholder: 'true' },
        { key: 'radarr_availability', label: 'Availability', placeholder: 'announced' },
      ],
      checkboxes: [
        { key: 'radarr_add_missing', label: 'Add Missing' },
        { key: 'radarr_add_existing', label: 'Add Existing' },
        { key: 'radarr_upgrade_existing', label: 'Upgrade Existing' },
      ],
    },
  },
  sonarr: {
    group: 'local',
    name: 'Sonarr',
    description: 'TV series collection manager',
    fields: [
      {
        key: 'sonarr_url',
        label: 'URL',
        placeholder: (ctx) => `http://${ctx.plexHost || '192.168.1.12'}:8989`,
      },
      { key: 'sonarr_key', label: 'API Key', type: 'password' },
    ],
    advanced: {
      title: 'Sonarr Advanced Configuration',
      fields: [
        { key: 'sonarr_root_folder_path', label: 'Root Folder Path', placeholder: 'S:/TV Shows' },
        { key: 'sonarr_quality_profile', label: 'Quality Profile', placeholder: 'HD-1080p' },
        { key: 'sonarr_language_profile', label: 'Language Profile', placeholder: 'English' },
        { key: 'sonarr_series_type', label: 'Series Type', placeholder: 'standard' },
        { key: 'sonarr_monitor', label: 'Monitor', placeholder: 'all' },
      ],
      checkboxes: [
        { key: 'sonarr_season_folder', label: 'Season Folder', defaultValue: true },
        { key: 'sonarr_add_missing', label: 'Add Missing' },
        { key: 'sonarr_add_existing', label: 'Add Existing' },
        { key: 'sonarr_upgrade_existing', label: 'Upgrade Existing' },
      ],
    },
  },
  gotify: {
    group: 'local',
    name: 'Gotify',
    description: 'Push notification service',
    fields: [
      {
        key: 'gotify_url',
        label: 'URL',
        placeholder: (ctx) => `http://${ctx.plexHost || '192.168.1.12'}:80`,
      },
      { key: 'gotify_key', label: 'Token', type: 'password' },
    ],
  },
  ntfy: {
    group: 'local',
    name: 'ntfy',
    description: 'Simple push notification service',
    fields: [
      {
        key: 'ntfy_url',
        label: 'URL',
        placeholder: (ctx) => `http://${ctx.plexHost || '192.168.1.12'}:80`,
      },
      { key: 'ntfy_key', label: 'Token', type: 'password' },
    ],
  },
  github: {
    group: 'api',
    name: 'GitHub',
    description: 'GitHub integration for custom configs',
    fields: [{ key: 'github_key', label: 'Personal Access Token', type: 'password' }],
  },
  omdb: {
    group: 'api',
    name: 'OMDb',
    description: 'Open Movie Database',
    fields: [{ key: 'omdb_key', label: 'API Key', type: 'password' }],
    link: 'http://www.omdbapi.com/apikey.aspx',
  },
  mdblist: {
    group: 'api',
    name: 'MDBList',
    description: 'Movie / TV database service',
    fields: [{ key: 'mdblist_key', label: 'API Key', type: 'password' }],
    link: 'https://mdblist.com/api/',
  },
  notifiarr: {
    group: 'api',
    name: 'Notifiarr',
    description: 'Notification service for media apps',
    fields: [{ key: 'notifiarr_key', label: 'API Key', type: 'password' }],
    link: 'https://notifiarr.com/',
  },
  anidb: {
    group: 'api',
    name: 'AniDB',
    description: 'Anime database',
    fields: [{ key: 'anidb_key', label: 'Username / Password', type: 'text' }],
    link: 'https://anidb.net/',
  },
  trakt: {
    group: 'api',
    name: 'Trakt',
    description: 'Movie and TV show tracking',
    fields: [
      { key: 'trakt_client_id', label: 'Client ID', type: 'text' },
      { key: 'trakt_client_secret', label: 'Client Secret', type: 'password' },
      { key: 'trakt_pin', label: 'PIN', type: 'text' },
    ],
    link: 'https://trakt.tv/oauth/applications',
  },
  mal: {
    group: 'api',
    name: 'MyAnimeList',
    description: 'Anime and manga database',
    fields: [
      { key: 'mal_client_id', label: 'Client ID', type: 'text' },
      { key: 'mal_client_secret', label: 'Client Secret', type: 'password' },
    ],
    link: 'https://myanimelist.net/apiconfig',
    advanced: {
      title: 'MyAnimeList Advanced Configuration',
      fields: [
        { key: 'mal_cache_expiration', label: 'Cache Expiration (minutes)', placeholder: '60' },
        { key: 'mal_localhost_url', label: 'Localhost URL', placeholder: '' },
      ],
    },
  },
};

const serviceGroups = {
  local: {
    title: 'Local Services',
    containerId: 'local-services',
  },
  api: {
    title: 'API Services',
    containerId: 'api-services',
  },
};

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

  renderServices();

  function ensureProfileState(profile) {
    profile.enabledServices = profile.enabledServices || {};
    profile.optionalServices = profile.optionalServices || {};
  }

  function getPlexHost() {
    const url = state.profile?.plex?.url || '';
    try {
      if (!url) return null;
      return new URL(url).hostname;
    } catch (err) {
      return null;
    }
  }

  function context() {
    return { plexHost: getPlexHost() };
  }

  function collectKeys(def) {
    const keys = new Set();
    def.fields?.forEach((field) => keys.add(field.key));
    def.advanced?.fields?.forEach((field) => keys.add(field.key));
    def.advanced?.checkboxes?.forEach((field) => keys.add(field.key));
    return keys;
  }

  function getValue(key, fallback = '') {
    const store = state.profile.optionalServices || {};
    const value = store[key];
    return value === undefined || value === null || value === '' ? fallback : value;
  }

  function isEnabled(serviceId) {
    return !!(state.profile.enabledServices && state.profile.enabledServices[serviceId]);
  }

  function renderServices() {
    ensureProfileState(state.profile);

    Object.entries(serviceGroups).forEach(([groupId, group]) => {
      const container = document.getElementById(group.containerId);
      if (!container) {
        return;
      }
      container.innerHTML = '';

      const services = Object.entries(serviceDefinitions)
        .filter(([, def]) => def.group === groupId)
        .sort((a, b) => a[1].name.localeCompare(b[1].name));

      services.forEach(([serviceId, def]) => {
        container.appendChild(buildServiceCard(serviceId, def));
      });

      if (!services.length) {
        const empty = document.createElement('p');
        empty.className = 'muted';
        empty.textContent = 'No services in this category.';
        container.appendChild(empty);
      }
    });
  }

  function buildServiceCard(serviceId, def) {
    const card = document.createElement('div');
    card.className = 'service-card';

    const header = document.createElement('div');
    header.className = 'service-header';

    const toggleLabel = document.createElement('label');
    toggleLabel.className = 'service-toggle';

    const toggle = document.createElement('input');
    toggle.type = 'checkbox';
    toggle.checked = isEnabled(serviceId);
    toggle.addEventListener('change', () => handleToggleService(serviceId, def, toggle.checked));

    const nameSpan = document.createElement('span');
    nameSpan.textContent = def.name;

    toggleLabel.appendChild(toggle);
    toggleLabel.appendChild(nameSpan);
    header.appendChild(toggleLabel);

    if (def.advanced) {
      const advancedBtn = document.createElement('button');
      advancedBtn.className = 'secondary';
      advancedBtn.textContent = 'Advanced';
      advancedBtn.disabled = !isEnabled(serviceId);
      advancedBtn.addEventListener('click', () => openAdvancedModal(serviceId, def));
      header.appendChild(advancedBtn);
    }

    card.appendChild(header);

    const desc = document.createElement('p');
    desc.className = 'service-description';
    desc.textContent = def.description;
    card.appendChild(desc);

    if (def.link) {
      const link = document.createElement('a');
      link.href = def.link;
      link.target = '_blank';
      link.rel = 'noopener noreferrer';
      link.className = 'muted';
      link.textContent = 'Get API key';
      card.appendChild(link);
    }

    if (def.fields && def.fields.length) {
      const fieldsWrapper = document.createElement('div');
      fieldsWrapper.className = 'service-fields';
      if (!isEnabled(serviceId)) {
        fieldsWrapper.classList.add('disabled');
      }

      def.fields.forEach((field) => {
        const label = document.createElement('label');
        label.textContent = field.label;

        const input = document.createElement('input');
        input.type = field.type === 'password' ? 'password' : 'text';
        const placeholder = typeof field.placeholder === 'function'
          ? field.placeholder(context())
          : field.placeholder;
        if (placeholder) {
          input.placeholder = placeholder;
        }
        input.value = getValue(field.key, placeholder || '');
        input.disabled = !isEnabled(serviceId);
        input.addEventListener('change', () => handleFieldChange(serviceId, def, field, input.value));

        label.appendChild(input);
        fieldsWrapper.appendChild(label);
      });

      card.appendChild(fieldsWrapper);
    }

    return card;
  }

  async function handleToggleService(serviceId, def, enabled) {
    try {
      await saveProfile((profile) => {
        ensureProfileState(profile);
        profile.enabledServices[serviceId] = enabled;

        if (enabled) {
          const ctx = context();
          def.fields?.forEach((field) => {
            const placeholder = typeof field.placeholder === 'function'
              ? field.placeholder(ctx)
              : field.placeholder;
            if (!profile.optionalServices[field.key]) {
              profile.optionalServices[field.key] = placeholder || '';
            }
          });
        } else {
          collectKeys(def).forEach((key) => delete profile.optionalServices[key]);
        }
      }, enabled ? `${def.name} enabled.` : `${def.name} disabled.`);

      await loadActiveProfile();
      renderServices();
    } catch (err) {
      showToast(err.message, 'error');
      await loadActiveProfile();
      renderServices();
    }
  }

  async function handleFieldChange(serviceId, def, field, rawValue) {
    if (!isEnabled(serviceId)) {
      return;
    }

    const value = (rawValue || '').trim();

    try {
      await saveProfile((profile) => {
        ensureProfileState(profile);
        if (!profile.enabledServices[serviceId]) {
          return;
        }
        if (value) {
          profile.optionalServices[field.key] = value;
        } else {
          delete profile.optionalServices[field.key];
        }
      }, `${def.name} saved.`);

      await loadActiveProfile();
      renderServices();
    } catch (err) {
      showToast(err.message, 'error');
      await loadActiveProfile();
      renderServices();
    }
  }

  function openAdvancedModal(serviceId, def) {
    if (!def.advanced || !isEnabled(serviceId)) {
      showToast('Enable the service first.', 'error');
      return;
    }

    const backdrop = document.createElement('div');
    backdrop.className = 'modal-backdrop';

    const dialog = document.createElement('div');
    dialog.className = 'modal-dialog';

    const title = document.createElement('h3');
    title.textContent = def.advanced.title;
    dialog.appendChild(title);

    const form = document.createElement('div');
    form.className = 'service-fields';

    const inputs = new Map();

    def.advanced.fields?.forEach((field) => {
      const label = document.createElement('label');
      label.textContent = field.label;

      const input = document.createElement('input');
      input.type = 'text';
      input.value = getValue(field.key, field.placeholder || '');
      inputs.set(field.key, input);

      label.appendChild(input);
      form.appendChild(label);
    });

    def.advanced.checkboxes?.forEach((field) => {
      const label = document.createElement('label');
      label.className = 'service-toggle';

      const checkbox = document.createElement('input');
      checkbox.type = 'checkbox';
      const current = getValue(field.key, field.defaultValue ? 'true' : 'false');
      checkbox.checked = current === true || current === 'true' || current === 'True';
      inputs.set(field.key, checkbox);

      const span = document.createElement('span');
      span.textContent = field.label;

      label.appendChild(checkbox);
      label.appendChild(span);
      form.appendChild(label);
    });

    dialog.appendChild(form);

    const actions = document.createElement('div');
    actions.className = 'modal-actions';

    const cancel = document.createElement('button');
    cancel.className = 'secondary';
    cancel.textContent = 'Cancel';
    cancel.addEventListener('click', () => document.body.removeChild(backdrop));

    const save = document.createElement('button');
    save.textContent = 'Save';
    save.addEventListener('click', async () => {
      try {
        await saveProfile((profile) => {
          ensureProfileState(profile);
          if (!profile.enabledServices[serviceId]) {
            return;
          }

          def.advanced.fields?.forEach((field) => {
            const input = inputs.get(field.key);
            const value = (input.value || '').trim();
            if (value) {
              profile.optionalServices[field.key] = value;
            } else {
              delete profile.optionalServices[field.key];
            }
          });

          def.advanced.checkboxes?.forEach((field) => {
            const input = inputs.get(field.key);
            profile.optionalServices[field.key] = input.checked ? 'true' : 'false';
          });
        }, `${def.name} advanced settings saved.`);

        document.body.removeChild(backdrop);
        await loadActiveProfile();
        renderServices();
      } catch (err) {
        showToast(err.message, 'error');
      }
    });

    actions.appendChild(cancel);
    actions.appendChild(save);
    dialog.appendChild(actions);

    backdrop.appendChild(dialog);
    document.body.appendChild(backdrop);
  }
})();
