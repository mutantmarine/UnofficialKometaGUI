using Microsoft.AspNetCore.SignalR;
using KometaGUIv3.Web.Hubs;

namespace KometaGUIv3.Web.Services
{
    public class LocalhostServerService
    {
        private readonly IHubContext<SyncHub> _hubContext;
        private readonly ILogger<LocalhostServerService> _logger;
        private bool _isRunning;
        private int _port;

        public LocalhostServerService(IHubContext<SyncHub> hubContext, ILogger<LocalhostServerService> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
            _isRunning = false;
            _port = 6969; // Default port
        }

        public bool IsRunning => _isRunning;
        public int Port => _port;

        public async Task StartAsync(int port = 6969)
        {
            if (_isRunning)
            {
                _logger.LogWarning("Server is already running on port {Port}", _port);
                return;
            }

            try
            {
                _port = port;
                _isRunning = true;
                
                _logger.LogInformation("Localhost server starting on port {Port}", _port);
                
                // Notify all connected clients
                await _hubContext.Clients.All.SendAsync("ServerStatusChanged", true, _port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start localhost server on port {Port}", port);
                _isRunning = false;
                throw;
            }
        }

        public async Task StopAsync()
        {
            if (!_isRunning)
            {
                _logger.LogWarning("Server is not running");
                return;
            }

            try
            {
                _isRunning = false;
                _logger.LogInformation("Localhost server stopped on port {Port}", _port);
                
                // Notify all connected clients
                await _hubContext.Clients.All.SendAsync("ServerStatusChanged", false, _port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping localhost server");
                throw;
            }
        }

        public string GetServerUrl()
        {
            return _isRunning ? $"http://localhost:{_port}" : "";
        }
    }
}