using KometaGUIv3.Shared.Models;

namespace KometaGUIv3.Shared.Interfaces
{
    public interface ISyncHub
    {
        // Profile management events
        Task ProfileCreated(KometaProfile profile);
        Task ProfileDeleted(string profileName);
        Task ProfileUpdated(KometaProfile profile);
        Task ProfileSelected(string profileName);
        
        // Navigation events
        Task PageChanged(int pageIndex, string pageName);
        Task ValidationStatusChanged(int pageIndex, bool isValid);
        
        // Configuration change events
        Task ConnectionsChanged(PlexConfiguration plex, TMDbConfiguration tmdb, List<string> selectedLibraries);
        Task ChartsChanged(Dictionary<string, bool> selectedCharts);
        Task OverlaysChanged(Dictionary<string, OverlayConfiguration> overlaySettings);
        Task OptionalServicesChanged(Dictionary<string, string> optionalServices, Dictionary<string, bool> enabledServices);
        Task SettingsChanged(KometaSettings settings);
        
        // Kometa execution events
        Task KometaStarted(string profileName);
        Task KometaStopped(string profileName);
        Task KometaLogMessage(string message);
        Task KometaError(string error);
        
        // Task scheduler events
        Task ScheduleCreated(string profileName, string frequency);
        Task ScheduleDeleted(string profileName);
        Task ScheduledTaskUpdated(bool created, string taskName);
        
        // Configuration generation events
        Task ConfigurationGenerated(bool success);
        
        // Kometa execution events (additional)
        Task KometaExecutionStarted();
        Task KometaExecutionCompleted(bool success);
        
        // Server status events
        Task ServerStatusChanged(bool isRunning, int port);
        Task ClientConnected(string connectionId, string userAgent);
        Task ClientDisconnected(string connectionId);
    }
    
    public interface ISyncClient
    {
        // Hub methods that clients can call
        Task JoinGroup(string groupName);
        Task LeaveGroup(string groupName);
        Task SendProfileUpdate(KometaProfile profile);
        Task SendPageNavigation(int pageIndex);
        Task SendConfigurationChange(string changeType, object data);
        Task RequestKometaExecution(string profileName, string action);
        Task RequestScheduleOperation(string operation, string profileName, string frequency = null);
        
        // Configuration and execution methods
        Task GenerateConfiguration(object configData);
        Task RunKometa(string profileName);
        Task StopKometa();
        
        // Scheduled task methods
        Task CreateScheduledTask(string profileName, string frequency, int interval, string time);
        Task RemoveScheduledTask(string profileName);
        
        // System and server methods
        Task CheckSystemRequirements();
        Task OpenConfigDirectory();
        Task UpdateConfiguration(string configType, object configData);
        Task PageChanged(string pageName);
        Task StopServer();
        Task RestartServer();
    }
}