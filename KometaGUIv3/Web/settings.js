const settingsSections = [
  {
    title: 'Core Settings',
    description: 'Essential behaviour for collections and sync operations.',
    fields: [
      {
        prop: 'syncMode',
        label: 'Sync Mode',
        type: 'select',
        options: [
          { value: 'append', label: 'append' },
          { value: 'sync', label: 'sync' },
        ],
        defaultValue: 'append',
        help: 'append = add items only, sync = mirror the collection exactly',
      },
      {
        prop: 'minimumItems',
        label: 'Minimum Items',
        type: 'number',
        min: 0,
        max: 100,
        defaultValue: 1,
        help: 'Collections with fewer items than this will be skipped.',
      },
      {
        prop: 'deleteBelowMinimum',
        label: 'Delete Below Minimum',
        type: 'checkbox',
        defaultValue: true,
        help: 'Remove collections that do not meet the minimum item requirement.',
      },
      {
        prop: 'runAgainDelay',
        label: 'Run Again Delay (hours)',
        type: 'number',
        min: 1,
        max: 24,
        defaultValue: 2,
        help: 'Delay before Kometa runs again when using the run_again option.',
      },
    ],
  },
  {
    title: 'Cache & Performance',
    description: 'Tuning options that influence caching and API usage.',
    fields: [
      {
        prop: 'cache',
        label: 'Enable Cache',
        type: 'checkbox',
        defaultValue: true,
        help: 'Caching significantly improves performance on repeat runs.',
      },
      {
        prop: 'cacheExpiration',
        label: 'Cache Expiration (days)',
        type: 'number',
        min: 1,
        max: 365,
        defaultValue: 60,
        help: 'How long cached data remains valid before refresh.',
      },
      {
        prop: 'verifySSL',
        label: 'Verify SSL Certificates',
        type: 'checkbox',
        defaultValue: true,
        help: 'Disable only if you encounter SSL errors with self-signed certs.',
      },
    ],
  },
  {
    title: 'Asset Management',
    description: 'Control how Kometa works with posters, artwork, and folders.',
    fields: [
      {
        prop: 'assetFolders',
        label: 'Use Asset Folders',
        type: 'checkbox',
        defaultValue: true,
        help: 'Organise assets in subfolders by collection name.',
      },
      {
        prop: 'assetDepth',
        label: 'Asset Folder Depth',
        type: 'number',
        min: 0,
        max: 5,
        defaultValue: 0,
        help: 'How deep to search within asset folders (0 = only root).',
      },
      {
        prop: 'createAssetFolders',
       label: 'Create Missing Asset Folders',
        type: 'checkbox',
        defaultValue: false,
        help: 'Automatically create folders when assets are missing.',
      },
      {
        prop: 'prioritizeAssets',
        label: 'Prioritise Local Assets',
        type: 'checkbox',
        defaultValue: false,
        help: 'Use local assets instead of downloading from online sources.',
      },
      {
        prop: 'dimensionalAssetRename',
        label: 'Dimensional Asset Rename',
        type: 'checkbox',
        defaultValue: false,
        help: 'Add image dimensions to asset filenames.',
      },
      {
        prop: 'downloadUrlAssets',
        label: 'Download URL Assets',
        type: 'checkbox',
        defaultValue: false,
        help: 'Download and save assets specified by URL in configs.',
      },
      {
        prop: 'showAssetNotNeeded',
        label: 'Show Asset Not Needed Messages',
        type: 'checkbox',
        defaultValue: true,
        help: 'Display messages when assets are not required for items.',
      },
    ],
  },
  {
    title: 'Display & Reporting',
    description: 'Choose what information appears in logs and reports.',
    fields: [
      { prop: 'showUnmanaged', label: 'Show Unmanaged Collections', type: 'checkbox', defaultValue: true, help: 'Display collections that exist but are not in your config.' },
      { prop: 'showUnconfigured', label: 'Show Unconfigured Collections', type: 'checkbox', defaultValue: true, help: 'Display details about collections not listed in your config.' },
      { prop: 'showMissing', label: 'Show Missing Items', type: 'checkbox', defaultValue: true, help: 'Display items that should be in collections but are missing.' },
      { prop: 'showMissingAssets', label: 'Show Missing Assets', type: 'checkbox', defaultValue: true, help: 'Display information about missing posters and artwork.' },
      { prop: 'showFiltered', label: 'Show Filtered Items', type: 'checkbox', defaultValue: false, help: 'Display items that were filtered out of collections.' },
      { prop: 'showOptions', label: 'Show Collection Options', type: 'checkbox', defaultValue: true, help: 'Display collection configuration options in the log output.' },
      { prop: 'saveReport', label: 'Save Reports To Disk', type: 'checkbox', defaultValue: false, help: 'Save detailed collection reports as files for later review.' },
    ],
  },
  {
    title: 'Advanced Options',
    description: 'Power-user settings for fine tuning behaviour and overlays.',
    fields: [
      { prop: 'deleteNotScheduled', label: 'Delete Not Scheduled', type: 'checkbox', defaultValue: false, help: 'Remove collections that are not scheduled to run this cycle.' },
      { prop: 'missingOnlyReleased', label: 'Missing Only Released', type: 'checkbox', defaultValue: false, help: 'Only show missing items that have already been released.' },
      { prop: 'onlyFilterMissing', label: 'Only Filter Missing', type: 'checkbox', defaultValue: false, help: 'Apply filters only to items that are missing from your library.' },
      { prop: 'itemRefreshDelay', label: 'Item Refresh Delay (seconds)', type: 'number', min: 0, max: 60, defaultValue: 0, help: 'Delay to prevent overwhelming your server during refreshes.' },
      { prop: 'playlistReport', label: 'Generate Playlist Reports', type: 'checkbox', defaultValue: false, help: 'Create detailed reports for playlist operations.' },
      { prop: 'overlayArtworkFiletype', label: 'Overlay Artwork Filetype', type: 'select', options: [
        { value: 'webp_lossy', label: 'webp_lossy' },
        { value: 'webp_lossless', label: 'webp_lossless' },
        { value: 'jpg', label: 'jpg' },
        { value: 'png', label: 'png' },
      ], defaultValue: 'webp_lossy', help: 'Format to use when generating overlay artwork files.' },
      { prop: 'overlayArtworkQuality', label: 'Overlay Artwork Quality', type: 'number', min: 1, max: 100, defaultValue: 90, help: 'Image quality for overlays (higher = better quality, larger files).' },
    ],
  },
];

const fieldRegistry = new Map();

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

  buildSettingsUI();
  populateValues();

  async function updateSetting(field, rawValue) {
    try {
      await saveProfile((profile) => {
        profile.settings = profile.settings || {};

        switch (field.type) {
          case 'checkbox':
            profile.settings[field.prop] = !!rawValue;
            break;
          case 'number': {
            const numberValue = Number.parseInt(rawValue, 10);
            const clamped = Number.isFinite(numberValue)
              ? clamp(numberValue, field.min, field.max, field.defaultValue)
              : field.defaultValue;
            profile.settings[field.prop] = clamped;
            break;
          }
          case 'stringList':
            profile.settings[field.prop] = parseList(rawValue);
            break;
          default:
            profile.settings[field.prop] = typeof rawValue === 'string' ? rawValue.trim() : rawValue;
            break;
        }
      }, `${field.label} saved.`);

      await loadActiveProfile();
      populateValues();
    } catch (err) {
      showToast(err.message, 'error');
      populateValues();
    }
  }

  function buildSettingsUI() {
    const root = document.getElementById('settings-root');
    if (!root) {
      return;
    }

    fieldRegistry.clear();

    // Remove everything except introductory card
    root.querySelectorAll('.settings-card, .settings-reset').forEach((node) => node.remove());

    const defaultsCard = document.createElement('section');
    defaultsCard.className = 'card settings-reset';
    const resetButton = document.createElement('button');
    resetButton.className = 'danger';
    resetButton.textContent = 'Reset All to Defaults';
    resetButton.addEventListener('click', async () => {
      if (!confirm('Reset all settings to their default values?')) {
        return;
      }
      try {
        await saveProfile((profile) => {
          profile.settings = buildDefaultSettings();
        }, 'Settings reset to defaults.');
        await loadActiveProfile();
        populateValues();
        showToast('Settings reset to defaults.', 'info');
      } catch (err) {
        showToast(err.message, 'error');
      }
    });
    defaultsCard.appendChild(resetButton);

    settingsSections.forEach((section) => {
      const card = document.createElement('section');
      card.className = 'card settings-card';

      const title = document.createElement('h2');
      title.textContent = section.title;
      card.appendChild(title);

      if (section.description) {
        const desc = document.createElement('p');
        desc.className = 'description';
        desc.textContent = section.description;
        card.appendChild(desc);
      }

      const grid = document.createElement('div');
      grid.className = 'form-grid settings-grid';

      section.fields.forEach((field) => {
        const fieldElement = createFieldElement(field);
        grid.appendChild(fieldElement);
      });

      card.appendChild(grid);
      root.appendChild(card);
    });

    root.appendChild(defaultsCard);
  }

  function createFieldElement(field) {
    let wrapper;

    if (field.type === 'checkbox') {
      wrapper = document.createElement('div');
      wrapper.className = 'setting-toggle';

      const input = document.createElement('input');
      input.type = 'checkbox';
      wrapper.appendChild(input);

      const label = document.createElement('span');
      label.textContent = field.label;
      wrapper.appendChild(label);

      if (field.help) {
        const help = document.createElement('p');
        help.className = 'help';
        help.textContent = field.help;
        wrapper.appendChild(help);
      }

      input.addEventListener('change', () => updateSetting(field, input.checked));
      fieldRegistry.set(field.prop, { element: input, field });
    } else {
      wrapper = document.createElement('label');
      wrapper.textContent = field.label;

      let input;

      if (field.type === 'select') {
        input = document.createElement('select');
        (field.options || []).forEach((option) => {
          const opt = document.createElement('option');
          opt.value = option.value;
          opt.textContent = option.label;
          input.appendChild(opt);
        });
      } else if (field.type === 'number') {
        input = document.createElement('input');
        input.type = 'number';
        if (typeof field.min === 'number') {
          input.min = field.min;
        }
        if (typeof field.max === 'number') {
          input.max = field.max;
        }
      } else if (field.type === 'stringList') {
        input = document.createElement('textarea');
        input.rows = 3;
        wrapper.classList.add('full-width');
      } else {
        input = document.createElement('input');
        input.type = 'text';
      }

      if (field.placeholder) {
        input.placeholder = field.placeholder;
      }

      input.addEventListener('change', () => updateSetting(field, input.value));

      wrapper.appendChild(input);

      if (field.help) {
        const help = document.createElement('p');
        help.className = 'help';
        help.textContent = field.help;
        wrapper.appendChild(help);
      }

      fieldRegistry.set(field.prop, { element: input, field });
    }

    return wrapper;
  }

  function populateValues() {
    const settings = state.profile.settings || {};

    fieldRegistry.forEach(({ element, field }) => {
      let value = settings[field.prop];

      if (value === undefined || value === null || value === '') {
        value = field.defaultValue;
      }

      switch (field.type) {
        case 'checkbox':
          element.checked = value !== undefined ? !!value : !!field.defaultValue;
          break;
        case 'number':
          element.value = value ?? field.defaultValue ?? 0;
          break;
        case 'select':
          element.value = value ?? field.defaultValue ?? '';
          break;
        case 'stringList':
          if (Array.isArray(value)) {
            element.value = value.join('\n');
          } else if (typeof value === 'string') {
            element.value = value;
          } else {
            element.value = '';
          }
          break;
        default:
          element.value = value ?? '';
          break;
      }
    });
  }

  function buildDefaultSettings() {
    const defaults = {};
    settingsSections.forEach((section) => {
      section.fields.forEach((field) => {
        if (field.type === 'stringList') {
          defaults[field.prop] = Array.isArray(field.defaultValue)
            ? [...field.defaultValue]
            : [];
        } else {
          defaults[field.prop] = field.defaultValue;
        }
      });
    });
    return defaults;
  }

  function clamp(value, min, max, fallback) {
    if (typeof min === 'number' && value < min) {
      return min;
    }
    if (typeof max === 'number' && value > max) {
      return max;
    }
    return Number.isFinite(value) ? value : fallback;
  }

  function parseList(rawValue) {
    if (!rawValue) {
      return [];
    }
    return rawValue
      .split(/[,\n]/)
      .map((entry) => entry.trim())
      .filter((entry) => entry.length > 0);
  }
})();
