const appState = {
  profiles: [],
  activeProfileName: '',
  profile: null,
  status: null,
  defaultsCollections: null,
  defaultsOverlays: null,
};

const api = {
  async request(path, opts = {}) {
    const options = { method: 'GET', ...opts };
    options.headers = { 'Content-Type': 'application/json', ...(options.headers || {}) };

    if (options.body && typeof options.body !== 'string') {
      options.body = JSON.stringify(options.body);
    }

    if (options.method === 'GET' || options.method === 'HEAD') {
      delete options.body;
    }

    const response = await fetch(path, options);
    const text = await response.text();
    let data = null;

    if (text) {
      try {
        data = JSON.parse(text);
      } catch (err) {
        data = text;
      }
    }

    if (!response.ok) {
      const message = data && data.error ? data.error : response.statusText;
      throw new Error(message);
    }

    return data;
  },

  get(path) {
    return this.request(path);
  },

  post(path, body) {
    return this.request(path, { method: 'POST', body });
  },

  put(path, body) {
    return this.request(path, { method: 'PUT', body });
  },

  del(path) {
    return this.request(path, { method: 'DELETE' });
  },
};

function showToast(message, type = 'info') {
  let toast = document.getElementById('toast');
  if (!toast) {
    toast = document.createElement('div');
    toast.id = 'toast';
    document.body.appendChild(toast);
  }

  toast.textContent = message;
  toast.className = `toast ${type}`;
  requestAnimationFrame(() => {
    toast.classList.add('show');
    setTimeout(() => toast.classList.remove('show'), 3000);
  });
}

function buildNavigation(activePage) {
  const header = document.querySelector('header');
  if (header) {
    return;
  }

  const navItems = [
    { id: 'profile', label: 'Profile', href: 'profile.html' },
    { id: 'connections', label: 'Connections', href: 'connections.html' },
    { id: 'collections', label: 'Collections', href: 'collections.html' },
    { id: 'overlays', label: 'Overlays', href: 'overlays.html' },
    { id: 'services', label: 'Services', href: 'services.html' },
    { id: 'settings', label: 'Settings', href: 'settings.html' },
    { id: 'final-actions', label: 'Final Actions', href: 'final-actions.html' },
  ];

  const headerEl = document.createElement('header');
  const nav = document.createElement('nav');

  navItems.forEach((item) => {
    const link = document.createElement('a');
    link.href = item.href;
    link.textContent = item.label;
    if (item.id === activePage) {
      link.classList.add('active');
    }
    nav.appendChild(link);
  });

  const statusChip = document.createElement('div');
  statusChip.className = 'status-chip';
  statusChip.id = 'status-chip';
  statusChip.textContent = 'Server: offline';

  headerEl.appendChild(nav);
  headerEl.appendChild(statusChip);
  document.body.insertBefore(headerEl, document.body.firstChild);
}

async function refreshStatus() {
  try {
    const status = await api.get('/api/status');
    appState.status = status;
    const chip = document.getElementById('status-chip');
    if (chip) {
      if (status?.running) {
        chip.textContent = status.profile ? `Server: ${status.profile}` : 'Server: running';
        chip.style.color = 'var(--success)';
        chip.style.background = 'rgba(76, 202, 114, 0.2)';
      } else {
        chip.textContent = 'Server: offline';
        chip.style.color = 'var(--muted)';
        chip.style.background = 'rgba(255,255,255,0.08)';
      }
    }
  } catch (err) {
    const chip = document.getElementById('status-chip');
    if (chip) {
      chip.textContent = 'Server: offline';
      chip.style.color = 'var(--muted)';
      chip.style.background = 'rgba(255,255,255,0.08)';
    }
  }
}

async function loadProfiles() {
  const payload = await api.get('/api/profiles');
  appState.profiles = payload?.profiles ?? [];
  appState.activeProfileName = payload?.active ?? '';
}

async function loadActiveProfile() {
  try {
    const profile = await api.get('/api/profile');
    appState.profile = profile || null;
  } catch (err) {
    appState.profile = null;
  }
}

function cloneProfile(profile) {
  return profile ? JSON.parse(JSON.stringify(profile)) : null;
}

async function saveProfile(mutator, successMessage) {
  if (!appState.profile) {
    showToast('No active profile selected.', 'error');
    return;
  }

  const updated = cloneProfile(appState.profile);
  mutator(updated);
  await api.put('/api/profile', updated);
  appState.profile = updated;
  if (successMessage) {
    showToast(successMessage, 'success');
  }
}

function renderMissingProfile(containerId) {
  const node = document.getElementById(containerId);
  if (!node) {
    return;
  }
  node.innerHTML = `
    <div class="card">
      <h2>No Profile Selected</h2>
      <p class="muted">Create or select a profile on the Profile page to continue.</p>
      <div class="mt-1">
        <a class="btn secondary" href="profile.html">Go to Profile Management</a>
      </div>
    </div>
  `;
}

document.addEventListener('DOMContentLoaded', async () => {
  const page = document.body.dataset.page || '';
  buildNavigation(page);
  await refreshStatus();
  setInterval(refreshStatus, 12000);
});

window.app = {
  state: appState,
  api,
  showToast,
  buildNavigation,
  refreshStatus,
  loadProfiles,
  loadActiveProfile,
  saveProfile,
  cloneProfile,
  renderMissingProfile,
};
