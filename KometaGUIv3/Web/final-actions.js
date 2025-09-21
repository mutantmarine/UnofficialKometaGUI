(async function initFinalActionsPage() {
  if (document.body.dataset.page !== 'final-actions') {
    return;
  }

  const {
    state,
    api,
    showToast,
    loadActiveProfile,
    refreshStatus,
    renderMissingProfile,
  } = window.app;

  await Promise.all([loadActiveProfile(), refreshStatus()]);

  if (!state.profile) {
    renderMissingProfile('final-root');
    return;
  }

  let lastTicks = 0;
  bindEvents();
  await refreshSummary();
  await pollLogs();
  const timer = setInterval(pollLogs, 4000);
  window.addEventListener('beforeunload', () => clearInterval(timer));

  function bindEvents() {
    handleClick('generate-yaml', async () => {
      const path = document.getElementById('yaml-target').value.trim();
      const result = await api.post('/api/actions/generate-yaml', path ? { targetPath: path } : {});
      showToast(`YAML saved to ${result?.path || 'configured directory'}`, 'success');
    });

    handleClick('run-kometa', async () => {
      const path = document.getElementById('config-path').value.trim();
      await api.post('/api/actions/run-kometa', path ? { configPath: path } : {});
      showToast('Kometa run started.', 'success');
    });

    handleClick('stop-kometa', async () => {
      await api.post('/api/actions/stop-kometa');
      showToast('Stop request sent.', 'info');
    });

    handleClick('create-schedule', async () => {
      const interval = parseInt(document.getElementById('schedule-interval').value, 10) || 1;
      const frequency = document.getElementById('schedule-frequency').value;
      const time = document.getElementById('schedule-time').value.replace(':', '') || '0200';
      await api.post('/api/actions/create-schedule', { interval, frequency, time });
      showToast('Scheduled task created.', 'success');
      await refreshSummary();
    });

    handleClick('remove-schedule', async () => {
      await api.post('/api/actions/remove-schedule');
      showToast('Scheduled task removed.', 'info');
      await refreshSummary();
    });

    handleClick('check-install', async () => {
      const status = await api.get('/api/actions/installation-status');
      renderInstallation(status);
      showToast('Installation status refreshed.', 'info');
    });

    handleClick('install-kometa', async () => {
      const force = document.getElementById('force-install')?.checked || false;
      await api.post('/api/actions/install-kometa', { force });
      showToast('Installation started.', 'success');
      await refreshSummary();
    });

    handleClick('update-kometa', async () => {
      await api.post('/api/actions/update-kometa');
      showToast('Update triggered.', 'success');
      await refreshSummary();
    });
  }

  function handleClick(id, handler) {
    const btn = document.getElementById(id);
    if (!btn) {
      return;
    }
    btn.addEventListener('click', async () => {
      btn.disabled = true;
      try {
        await handler();
      } catch (err) {
        showToast(err.message, 'error');
      } finally {
        btn.disabled = false;
        await pollLogs();
      }
    });
  }

  async function refreshSummary() {
    try {
      const [status, schedule, install] = await Promise.all([
        api.get('/api/status'),
        api.get('/api/actions/schedule-status'),
        api.get('/api/actions/installation-status'),
      ]);

      renderSchedule(schedule);
      renderInstallation(install);
      state.status = status;
    } catch (err) {
      // ignore summary errors
    }
  }

  function renderSchedule(data) {
    const badge = document.getElementById('schedule-status');
    if (!badge) {
      return;
    }
    const exists = !!data?.exists;
    badge.textContent = exists ? 'Scheduled task present' : 'No scheduled task';
    badge.className = `badge ${exists ? 'success' : ''}`;
  }

  function renderInstallation(status) {
    const container = document.getElementById('install-summary');
    if (!container) {
      return;
    }

    if (!status) {
      container.innerHTML = '<p class="muted">No data yet.</p>';
      return;
    }

    container.innerHTML = `
      <div class="grid two">
        <div>
          <h3>Kometa</h3>
          <p class="muted">${status.isKometaInstalled ? `Installed (${status.kometaVersion || 'Unknown'})` : 'Not installed'}</p>
        </div>
        <div>
          <h3>Virtual Environment</h3>
          <p class="muted">${status.isVirtualEnvironmentReady ? 'Ready' : 'Missing'}</p>
        </div>
        <div>
          <h3>Dependencies</h3>
          <p class="muted">${status.areDependenciesInstalled ? 'Installed' : 'Missing'}</p>
        </div>
        <div>
          <h3>Python</h3>
          <p class="muted">${status.isPythonReady ? 'Available' : 'Missing'}</p>
        </div>
        <div>
          <h3>Git</h3>
          <p class="muted">${status.isGitReady ? 'Available' : 'Missing'}</p>
        </div>
      </div>
    `;
  }

  async function pollLogs() {
    try {
      const query = lastTicks ? `?sinceTicks=${lastTicks}` : '';
      const logs = await api.get(`/api/logs${query}`);
      if (!Array.isArray(logs) || !logs.length) {
        return;
      }

      const stream = document.getElementById('log-stream');
      if (!stream) {
        return;
      }

      logs.forEach((entry) => {
        const timestamp = new Date(entry.timestamp);
        const timeText = isNaN(timestamp.getTime()) ? entry.timestamp : timestamp.toLocaleTimeString();
        const line = document.createElement('div');
        line.textContent = `[${timeText}] [${entry.source}] ${entry.message}`;
        stream.appendChild(line);
        stream.scrollTop = stream.scrollHeight;

        const parsed = Date.parse(entry.timestamp);
        if (!Number.isNaN(parsed)) {
          const ticks = parsed * 10000 + 621355968000000000;
          lastTicks = Math.max(lastTicks, ticks);
        }
      });
    } catch (err) {
      // swallow log polling errors
    }
  }
})();
