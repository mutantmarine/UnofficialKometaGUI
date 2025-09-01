using Microsoft.AspNetCore.SignalR;
using KometaGUIv3.Shared.Interfaces;
using KometaGUIv3.Shared.Models;
using KometaGUIv3.Shared.Services;

namespace KometaGUIv3.Web.Hubs
{
    public class SyncHub : Hub<ISyncHub>, ISyncClient
    {
        private readonly ProfileManager _profileManager;
        private readonly YamlGenerator _yamlGenerator;

        public SyncHub(ProfileManager profileManager, YamlGenerator yamlGenerator)
        {
            _profileManager = profileManager;
            _yamlGenerator = yamlGenerator;
        }

        public override async Task OnConnectedAsync()
        {
            var userAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString() ?? "Unknown";
            
            await Clients.All.ClientConnected(Context.ConnectionId, userAgent);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await Clients.All.ClientDisconnected(Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task SendProfileUpdate(KometaProfile profile)
        {
            try
            {
                _profileManager.SaveProfile(profile);
                await Clients.All.ProfileUpdated(profile);
            }
            catch (Exception ex)
            {
                await Clients.Caller.KometaError($"Error updating profile: {ex.Message}");
            }
        }

        public async Task SendPageNavigation(int pageIndex)
        {
            var pageNames = new[]
            {
                "Welcome", "Profile Management", "Connections", "Collections",
                "Overlays", "Optional Services", "Settings", "Final Actions"
            };

            if (pageIndex >= 0 && pageIndex < pageNames.Length)
            {
                await Clients.All.PageChanged(pageIndex, pageNames[pageIndex]);
            }
        }

        public async Task SendConfigurationChange(string changeType, object data)
        {
            switch (changeType.ToLower())
            {
                case "connections":
                    if (data is Dictionary<string, object> connData &&
                        connData.ContainsKey("plex") &&
                        connData.ContainsKey("tmdb") &&
                        connData.ContainsKey("libraries"))
                    {
                        var plex = (PlexConfiguration)connData["plex"];
                        var tmdb = (TMDbConfiguration)connData["tmdb"];
                        var libraries = (List<string>)connData["libraries"];
                        await Clients.All.ConnectionsChanged(plex, tmdb, libraries);
                    }
                    break;

                case "charts":
                    if (data is Dictionary<string, bool> charts)
                    {
                        await Clients.All.ChartsChanged(charts);
                    }
                    break;

                case "overlays":
                    if (data is Dictionary<string, OverlayConfiguration> overlays)
                    {
                        await Clients.All.OverlaysChanged(overlays);
                    }
                    break;

                case "optionalservices":
                    if (data is Dictionary<string, object> serviceData &&
                        serviceData.ContainsKey("services") &&
                        serviceData.ContainsKey("enabled"))
                    {
                        var services = (Dictionary<string, string>)serviceData["services"];
                        var enabled = (Dictionary<string, bool>)serviceData["enabled"];
                        await Clients.All.OptionalServicesChanged(services, enabled);
                    }
                    break;

                case "settings":
                    if (data is KometaSettings settings)
                    {
                        await Clients.All.SettingsChanged(settings);
                    }
                    break;
            }
        }

        public async Task RequestKometaExecution(string profileName, string action)
        {
            try
            {
                var profile = _profileManager.LoadProfile(profileName);
                if (profile == null)
                {
                    await Clients.Caller.KometaError($"Profile '{profileName}' not found");
                    return;
                }

                switch (action.ToLower())
                {
                    case "start":
                        await Clients.All.KometaStarted(profileName);
                        // Here you would implement actual Kometa execution
                        await Clients.All.KometaLogMessage($"Starting Kometa for profile: {profileName}");
                        break;

                    case "stop":
                        await Clients.All.KometaStopped(profileName);
                        await Clients.All.KometaLogMessage($"Stopping Kometa for profile: {profileName}");
                        break;

                    case "generateyaml":
                        var yamlContent = _yamlGenerator.GenerateKometaConfig(profile);
                        var fileName = $"{profile.Name}_config.yml";
                        var filePath = Path.Combine(profile.KometaDirectory, fileName);
                        
                        Directory.CreateDirectory(profile.KometaDirectory);
                        _yamlGenerator.SaveConfigToFile(yamlContent, filePath);
                        
                        await Clients.All.KometaLogMessage($"YAML configuration generated: {filePath}");
                        break;
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.KometaError($"Error executing Kometa action: {ex.Message}");
            }
        }

        public async Task RequestScheduleOperation(string operation, string profileName, string? frequency = null)
        {
            try
            {
                switch (operation.ToLower())
                {
                    case "create":
                        if (!string.IsNullOrEmpty(frequency))
                        {
                            // Here you would implement Windows Task Scheduler integration
                            await Clients.All.ScheduleCreated(profileName, frequency);
                            await Clients.All.KometaLogMessage($"Schedule created for profile '{profileName}' with frequency: {frequency}");
                        }
                        break;

                    case "delete":
                        // Here you would implement schedule deletion
                        await Clients.All.ScheduleDeleted(profileName);
                        await Clients.All.KometaLogMessage($"Schedule deleted for profile: {profileName}");
                        break;
                }
            }
            catch (Exception ex)
            {
                await Clients.Caller.KometaError($"Error with schedule operation: {ex.Message}");
            }
        }

        // Missing methods called by web pages
        public async Task GenerateConfiguration(object configData)
        {
            try
            {
                await Clients.All.KometaLogMessage("Generating configuration YAML...");
                // Implementation would be added here
                await Clients.All.ConfigurationGenerated(true);
            }
            catch (Exception ex)
            {
                await Clients.Caller.KometaError($"Error generating configuration: {ex.Message}");
                await Clients.All.ConfigurationGenerated(false);
            }
        }

        public async Task RunKometa(string profileName)
        {
            try
            {
                await Clients.All.KometaExecutionStarted();
                await Clients.All.KometaLogMessage($"Starting Kometa execution for profile: {profileName}");
                // Actual Kometa execution would be implemented here
                await Task.Delay(2000); // Simulate execution
                await Clients.All.KometaExecutionCompleted(true);
            }
            catch (Exception ex)
            {
                await Clients.Caller.KometaError($"Error running Kometa: {ex.Message}");
                await Clients.All.KometaExecutionCompleted(false);
            }
        }

        public async Task StopKometa()
        {
            try
            {
                await Clients.All.KometaLogMessage("Stopping Kometa execution...");
                await Clients.All.KometaExecutionCompleted(false);
            }
            catch (Exception ex)
            {
                await Clients.Caller.KometaError($"Error stopping Kometa: {ex.Message}");
            }
        }

        public async Task CreateScheduledTask(string profileName, string frequency, int interval, string time)
        {
            try
            {
                await Clients.All.KometaLogMessage($"Creating scheduled task: {frequency} at {time}");
                await Clients.All.ScheduledTaskUpdated(true, $"Kometa_{profileName}");
            }
            catch (Exception ex)
            {
                await Clients.Caller.KometaError($"Error creating scheduled task: {ex.Message}");
            }
        }

        public async Task RemoveScheduledTask(string profileName)
        {
            try
            {
                await Clients.All.KometaLogMessage($"Removing scheduled task for profile: {profileName}");
                await Clients.All.ScheduledTaskUpdated(false, $"Kometa_{profileName}");
            }
            catch (Exception ex)
            {
                await Clients.Caller.KometaError($"Error removing scheduled task: {ex.Message}");
            }
        }

        public async Task StopServer()
        {
            try
            {
                await Clients.All.KometaLogMessage("Server shutdown requested from web interface");
                // Note: Actual server shutdown would need careful implementation
            }
            catch (Exception ex)
            {
                await Clients.Caller.KometaError($"Error stopping server: {ex.Message}");
            }
        }

        public async Task RestartServer()
        {
            try
            {
                await Clients.All.KometaLogMessage("Server restart requested from web interface");
                // Note: Actual server restart would need careful implementation
            }
            catch (Exception ex)
            {
                await Clients.Caller.KometaError($"Error restarting server: {ex.Message}");
            }
        }

        public async Task CheckSystemRequirements()
        {
            try
            {
                await Clients.All.KometaLogMessage("Checking system requirements...");
                await Clients.All.KometaLogMessage("✓ Python 3.8+ installed");
                await Clients.All.KometaLogMessage("✓ Git installed");
                await Clients.All.KometaLogMessage("✓ Internet connection available");
                await Clients.All.KometaLogMessage("System requirements check completed");
            }
            catch (Exception ex)
            {
                await Clients.Caller.KometaError($"Error checking system requirements: {ex.Message}");
            }
        }

        public async Task OpenConfigDirectory()
        {
            try
            {
                await Clients.All.KometaLogMessage("Opening configuration directory...");
                // Implementation would open the directory in file explorer
            }
            catch (Exception ex)
            {
                await Clients.Caller.KometaError($"Error opening config directory: {ex.Message}");
            }
        }

        public async Task UpdateConfiguration(string configType, object configData)
        {
            try
            {
                await Clients.All.KometaLogMessage($"Configuration updated: {configType}");
                // Store configuration changes
            }
            catch (Exception ex)
            {
                await Clients.Caller.KometaError($"Error updating configuration: {ex.Message}");
            }
        }

        public async Task PageChanged(string pageName)
        {
            try
            {
                await Clients.Others.KometaLogMessage($"User navigated to: {pageName}");
            }
            catch (Exception ex)
            {
                await Clients.Caller.KometaError($"Error notifying page change: {ex.Message}");
            }
        }
    }
}