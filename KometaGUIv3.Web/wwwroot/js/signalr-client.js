// SignalR Client for real-time synchronization
class KometaSyncClient {
    constructor() {
        this.connection = null;
        this.connected = false;
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 5;
        this.callbacks = {};
    }

    async initialize() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/synchub")
            .withAutomaticReconnect()
            .build();

        this.setupEventHandlers();
        
        try {
            await this.connection.start();
            this.connected = true;
            this.reconnectAttempts = 0;
            console.log("SignalR Connected");
            this.showStatusMessage("Connected to sync server", "success");
        } catch (err) {
            console.error("SignalR Connection Error:", err);
            this.showStatusMessage("Failed to connect to sync server", "error");
            this.scheduleReconnect();
        }
    }

    setupEventHandlers() {
        // Connection events
        this.connection.onreconnecting(() => {
            this.connected = false;
            this.showStatusMessage("Reconnecting to sync server...", "warning");
        });

        this.connection.onreconnected(() => {
            this.connected = true;
            this.reconnectAttempts = 0;
            this.showStatusMessage("Reconnected to sync server", "success");
        });

        this.connection.onclose(() => {
            this.connected = false;
            this.showStatusMessage("Disconnected from sync server", "error");
            this.scheduleReconnect();
        });

        // Profile management events
        this.connection.on("ProfileCreated", (profile) => {
            this.handleProfileCreated(profile);
        });

        this.connection.on("ProfileDeleted", (profileName) => {
            this.handleProfileDeleted(profileName);
        });

        this.connection.on("ProfileUpdated", (profile) => {
            this.handleProfileUpdated(profile);
        });

        this.connection.on("ProfileSelected", (profileName) => {
            this.handleProfileSelected(profileName);
        });

        // Navigation events
        this.connection.on("PageChanged", (pageIndex, pageName) => {
            this.handlePageChanged(pageIndex, pageName);
        });

        this.connection.on("ValidationStatusChanged", (pageIndex, isValid) => {
            this.handleValidationStatusChanged(pageIndex, isValid);
        });

        // Configuration change events
        this.connection.on("ConnectionsChanged", (plex, tmdb, libraries) => {
            this.handleConnectionsChanged(plex, tmdb, libraries);
        });

        this.connection.on("ChartsChanged", (selectedCharts) => {
            this.handleChartsChanged(selectedCharts);
        });

        this.connection.on("OverlaysChanged", (overlaySettings) => {
            this.handleOverlaysChanged(overlaySettings);
        });

        this.connection.on("OptionalServicesChanged", (services, enabled) => {
            this.handleOptionalServicesChanged(services, enabled);
        });

        this.connection.on("SettingsChanged", (settings) => {
            this.handleSettingsChanged(settings);
        });

        // Kometa execution events
        this.connection.on("KometaStarted", (profileName) => {
            this.handleKometaStarted(profileName);
        });

        this.connection.on("KometaStopped", (profileName) => {
            this.handleKometaStopped(profileName);
        });

        this.connection.on("KometaLogMessage", (message) => {
            this.handleKometaLogMessage(message);
        });

        this.connection.on("KometaError", (error) => {
            this.handleKometaError(error);
        });

        // Task scheduler events
        this.connection.on("ScheduleCreated", (profileName, frequency) => {
            this.handleScheduleCreated(profileName, frequency);
        });

        this.connection.on("ScheduleDeleted", (profileName) => {
            this.handleScheduleDeleted(profileName);
        });

        // Server status events
        this.connection.on("ServerStatusChanged", (isRunning, port) => {
            this.handleServerStatusChanged(isRunning, port);
        });

        this.connection.on("ClientConnected", (connectionId, userAgent) => {
            this.handleClientConnected(connectionId, userAgent);
        });

        this.connection.on("ClientDisconnected", (connectionId) => {
            this.handleClientDisconnected(connectionId);
        });
    }

    async scheduleReconnect() {
        if (this.reconnectAttempts >= this.maxReconnectAttempts) {
            this.showStatusMessage("Maximum reconnection attempts reached", "error");
            return;
        }

        this.reconnectAttempts++;
        const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), 30000);
        
        setTimeout(async () => {
            try {
                await this.connection.start();
                this.connected = true;
                this.reconnectAttempts = 0;
                this.showStatusMessage("Reconnected to sync server", "success");
            } catch (err) {
                console.error("Reconnect failed:", err);
                this.scheduleReconnect();
            }
        }, delay);
    }

    // Public methods for sending data to hub
    async sendProfileUpdate(profile) {
        if (!this.connected) return;
        try {
            await this.connection.invoke("SendProfileUpdate", profile);
        } catch (err) {
            console.error("Error sending profile update:", err);
        }
    }

    async sendPageNavigation(pageIndex) {
        if (!this.connected) return;
        try {
            await this.connection.invoke("SendPageNavigation", pageIndex);
        } catch (err) {
            console.error("Error sending page navigation:", err);
        }
    }

    async sendConfigurationChange(changeType, data) {
        if (!this.connected) return;
        try {
            await this.connection.invoke("SendConfigurationChange", changeType, data);
        } catch (err) {
            console.error("Error sending configuration change:", err);
        }
    }

    async requestKometaExecution(profileName, action) {
        if (!this.connected) return;
        try {
            await this.connection.invoke("RequestKometaExecution", profileName, action);
        } catch (err) {
            console.error("Error requesting Kometa execution:", err);
        }
    }

    async requestScheduleOperation(operation, profileName, frequency = null) {
        if (!this.connected) return;
        try {
            await this.connection.invoke("RequestScheduleOperation", operation, profileName, frequency);
        } catch (err) {
            console.error("Error requesting schedule operation:", err);
        }
    }

    // Event handlers
    handleProfileCreated(profile) {
        this.fireCallback('profileCreated', profile);
        this.showStatusMessage(`Profile '${profile.Name}' created`, "success");
    }

    handleProfileDeleted(profileName) {
        this.fireCallback('profileDeleted', profileName);
        this.showStatusMessage(`Profile '${profileName}' deleted`, "info");
    }

    handleProfileUpdated(profile) {
        this.fireCallback('profileUpdated', profile);
        // Don't show message for every update to avoid spam
    }

    handleProfileSelected(profileName) {
        this.fireCallback('profileSelected', profileName);
        this.showStatusMessage(`Profile '${profileName}' selected`, "info");
    }

    handlePageChanged(pageIndex, pageName) {
        this.fireCallback('pageChanged', { pageIndex, pageName });
        // Don't show message for page changes to avoid spam
    }

    handleValidationStatusChanged(pageIndex, isValid) {
        this.fireCallback('validationStatusChanged', { pageIndex, isValid });
    }

    handleConnectionsChanged(plex, tmdb, libraries) {
        this.fireCallback('connectionsChanged', { plex, tmdb, libraries });
    }

    handleChartsChanged(selectedCharts) {
        this.fireCallback('chartsChanged', selectedCharts);
    }

    handleOverlaysChanged(overlaySettings) {
        this.fireCallback('overlaysChanged', overlaySettings);
    }

    handleOptionalServicesChanged(services, enabled) {
        this.fireCallback('optionalServicesChanged', { services, enabled });
    }

    handleSettingsChanged(settings) {
        this.fireCallback('settingsChanged', settings);
    }

    handleKometaStarted(profileName) {
        this.fireCallback('kometaStarted', profileName);
        this.showStatusMessage(`Kometa started for '${profileName}'`, "success");
    }

    handleKometaStopped(profileName) {
        this.fireCallback('kometaStopped', profileName);
        this.showStatusMessage(`Kometa stopped for '${profileName}'`, "info");
    }

    handleKometaLogMessage(message) {
        this.fireCallback('kometaLogMessage', message);
        this.appendToLog(message, 'info');
    }

    handleKometaError(error) {
        this.fireCallback('kometaError', error);
        this.showStatusMessage(error, "error");
        this.appendToLog(error, 'error');
    }

    handleScheduleCreated(profileName, frequency) {
        this.fireCallback('scheduleCreated', { profileName, frequency });
        this.showStatusMessage(`Schedule created: ${frequency}`, "success");
    }

    handleScheduleDeleted(profileName) {
        this.fireCallback('scheduleDeleted', profileName);
        this.showStatusMessage(`Schedule deleted for '${profileName}'`, "info");
    }

    handleServerStatusChanged(isRunning, port) {
        this.fireCallback('serverStatusChanged', { isRunning, port });
        const status = isRunning ? `running on port ${port}` : 'stopped';
        this.showStatusMessage(`Server ${status}`, isRunning ? "success" : "info");
    }

    handleClientConnected(connectionId, userAgent) {
        this.fireCallback('clientConnected', { connectionId, userAgent });
    }

    handleClientDisconnected(connectionId) {
        this.fireCallback('clientDisconnected', connectionId);
    }

    // Utility methods
    on(event, callback) {
        if (!this.callbacks[event]) {
            this.callbacks[event] = [];
        }
        this.callbacks[event].push(callback);
    }

    off(event, callback) {
        if (!this.callbacks[event]) return;
        const index = this.callbacks[event].indexOf(callback);
        if (index > -1) {
            this.callbacks[event].splice(index, 1);
        }
    }

    fireCallback(event, data) {
        if (!this.callbacks[event]) return;
        this.callbacks[event].forEach(callback => {
            try {
                callback(data);
            } catch (err) {
                console.error(`Error in callback for ${event}:`, err);
            }
        });
    }

    showStatusMessage(message, type = "info") {
        // Create or update status message element
        let statusEl = document.getElementById('sync-status');
        if (!statusEl) {
            statusEl = document.createElement('div');
            statusEl.id = 'sync-status';
            statusEl.style.cssText = `
                position: fixed;
                top: 20px;
                right: 20px;
                z-index: 1000;
                max-width: 300px;
                pointer-events: none;
            `;
            document.body.appendChild(statusEl);
        }

        const alertEl = document.createElement('div');
        alertEl.className = `alert alert-${type === 'error' ? 'danger' : type}`;
        alertEl.textContent = message;
        alertEl.style.cssText = `
            margin-bottom: 10px;
            pointer-events: auto;
            opacity: 0;
            transform: translateX(100%);
            transition: all 0.3s ease;
        `;

        statusEl.appendChild(alertEl);

        // Animate in
        setTimeout(() => {
            alertEl.style.opacity = '1';
            alertEl.style.transform = 'translateX(0)';
        }, 10);

        // Auto remove after delay
        setTimeout(() => {
            alertEl.style.opacity = '0';
            alertEl.style.transform = 'translateX(100%)';
            setTimeout(() => {
                if (alertEl.parentNode) {
                    alertEl.parentNode.removeChild(alertEl);
                }
            }, 300);
        }, type === 'error' ? 5000 : 3000);
    }

    appendToLog(message, type = 'info') {
        const logContainer = document.getElementById('kometa-log');
        if (!logContainer) return;

        const timestamp = new Date().toLocaleTimeString();
        const logEntry = document.createElement('div');
        logEntry.className = `log-entry log-${type}`;
        logEntry.innerHTML = `<span class="log-timestamp">[${timestamp}]</span> ${message}`;

        logContainer.appendChild(logEntry);
        logContainer.scrollTop = logContainer.scrollHeight;

        // Limit log entries to prevent memory issues
        const entries = logContainer.querySelectorAll('.log-entry');
        if (entries.length > 1000) {
            entries[0].remove();
        }
    }
}

// Global instance
window.kometaSync = new KometaSyncClient();

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.kometaSync.initialize();
});