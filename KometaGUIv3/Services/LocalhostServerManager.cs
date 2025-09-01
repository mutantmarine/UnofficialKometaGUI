using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using KometaGUIv3.Shared.Models;
using KometaGUIv3.Shared.Services;

namespace KometaGUIv3.Services
{
    public class LocalhostServerManager : IDisposable
    {
        private Process? _serverProcess;
        private HubConnection? _hubConnection;
        private readonly ProfileManager _profileManager;
        private bool _isServerRunning = false;
        private int _serverPort = 6969;
        private bool _disposed = false;

        public event EventHandler<bool> ServerStatusChanged;
        public event EventHandler<string> ServerLogReceived;
        public event EventHandler<string> ServerError;

        public LocalhostServerManager(ProfileManager profileManager)
        {
            _profileManager = profileManager;
        }

        public bool IsServerRunning => _isServerRunning;
        public int ServerPort => _serverPort;
        public string ServerUrl => _isServerRunning ? $"http://localhost:{_serverPort}" : "";

        public async Task<bool> StartServerAsync(int port = 6969)
        {
            if (_isServerRunning)
            {
                ServerLogReceived?.Invoke(this, "Server is already running");
                return true;
            }

            try
            {
                _serverPort = port;
                
                // Get the path to the web application
                var webAppPath = GetWebApplicationPath();
                if (!Directory.Exists(webAppPath))
                {
                    ServerError?.Invoke(this, $"Web application not found at: {webAppPath}");
                    return false;
                }

                // Build the web project first to ensure it's up to date
                ServerLogReceived?.Invoke(this, "Building web project...");
                var buildResult = await BuildWebProjectAsync(webAppPath);
                if (!buildResult)
                {
                    ServerError?.Invoke(this, "Failed to build web project");
                    return false;
                }

                // Start the web server process
                var startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --no-build --urls \"http://localhost:{port}\"",
                    WorkingDirectory = webAppPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    Environment = { ["ASPNETCORE_ENVIRONMENT"] = "Development" }
                };

                _serverProcess = new Process { StartInfo = startInfo };
                
                _serverProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        ServerLogReceived?.Invoke(this, e.Data);
                        
                        // Check for successful startup indicators
                        if (e.Data.Contains("Now listening on:") && e.Data.Contains($"localhost:{port}"))
                        {
                            _isServerRunning = true;
                            ServerStatusChanged?.Invoke(this, true);
                        }
                    }
                };

                _serverProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        ServerError?.Invoke(this, e.Data);
                    }
                };

                _serverProcess.Start();
                _serverProcess.BeginOutputReadLine();
                _serverProcess.BeginErrorReadLine();

                // Wait for server to start with better detection
                var timeout = DateTime.Now.AddSeconds(15); // Increase timeout
                while (!_isServerRunning && DateTime.Now < timeout && !_serverProcess.HasExited)
                {
                    await Task.Delay(500);
                }

                // Check final status
                if (_serverProcess.HasExited)
                {
                    var exitCode = _serverProcess.ExitCode;
                    ServerError?.Invoke(this, $"Server process exited unexpectedly with code {exitCode}");
                    return false;
                }

                if (!_isServerRunning)
                {
                    ServerError?.Invoke(this, "Server failed to start within timeout period");
                    await StopServerAsync();
                    return false;
                }

                ServerLogReceived?.Invoke(this, $"Localhost server started successfully on port {port}");

                // Initialize SignalR connection
                await InitializeSignalRConnection();

                return true;
            }
            catch (Exception ex)
            {
                ServerError?.Invoke(this, $"Failed to start server: {ex.Message}");
                return false;
            }
        }

        public async Task StopServerAsync()
        {
            if (!_isServerRunning)
            {
                ServerLogReceived?.Invoke(this, "Server is not running");
                return;
            }

            try
            {
                // Disconnect SignalR
                if (_hubConnection != null)
                {
                    await _hubConnection.StopAsync();
                    await _hubConnection.DisposeAsync();
                    _hubConnection = null;
                }

                // Stop the server process
                if (_serverProcess != null && !_serverProcess.HasExited)
                {
                    _serverProcess.Kill();
                    _serverProcess.WaitForExit(5000);
                    _serverProcess.Dispose();
                    _serverProcess = null;
                }

                _isServerRunning = false;
                ServerStatusChanged?.Invoke(this, false);
                ServerLogReceived?.Invoke(this, "Localhost server stopped");
            }
            catch (Exception ex)
            {
                ServerError?.Invoke(this, $"Error stopping server: {ex.Message}");
            }
        }

        public void OpenInBrowser()
        {
            if (!_isServerRunning)
            {
                MessageBox.Show("Server is not running. Please start the server first.", "Server Not Running", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var url = $"http://localhost:{_serverPort}";
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                ServerLogReceived?.Invoke(this, $"Opened browser to {url}");
            }
            catch (Exception ex)
            {
                ServerError?.Invoke(this, $"Failed to open browser: {ex.Message}");
            }
        }

        private async Task InitializeSignalRConnection()
        {
            try
            {
                var url = $"http://localhost:{_serverPort}/synchub";
                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(url)
                    .WithAutomaticReconnect()
                    .Build();

                // Set up event handlers for synchronization
                _hubConnection.On<KometaProfile>("ProfileCreated", (profile) =>
                {
                    ServerLogReceived?.Invoke(this, $"Profile '{profile.Name}' created via web interface");
                });

                _hubConnection.On<string>("ProfileDeleted", (profileName) =>
                {
                    ServerLogReceived?.Invoke(this, $"Profile '{profileName}' deleted via web interface");
                });

                _hubConnection.On<KometaProfile>("ProfileUpdated", (profile) =>
                {
                    // Sync profile changes from web to Windows app
                    _profileManager.SaveProfile(profile);
                });

                _hubConnection.On<int, string>("PageChanged", (pageIndex, pageName) =>
                {
                    ServerLogReceived?.Invoke(this, $"Web user navigated to: {pageName}");
                });

                _hubConnection.On<string>("KometaLogMessage", (message) =>
                {
                    ServerLogReceived?.Invoke(this, $"Kometa: {message}");
                });

                _hubConnection.On<string>("KometaError", (error) =>
                {
                    ServerError?.Invoke(this, $"Kometa Error: {error}");
                });

                // Handle connection events
                _hubConnection.Reconnecting += (exception) =>
                {
                    ServerLogReceived?.Invoke(this, "Reconnecting to web interface...");
                    return Task.CompletedTask;
                };

                _hubConnection.Reconnected += (connectionId) =>
                {
                    ServerLogReceived?.Invoke(this, "Reconnected to web interface");
                    return Task.CompletedTask;
                };

                _hubConnection.Closed += (exception) =>
                {
                    if (exception != null)
                    {
                        ServerError?.Invoke(this, $"Connection closed: {exception.Message}");
                    }
                    return Task.CompletedTask;
                };

                await _hubConnection.StartAsync();
                ServerLogReceived?.Invoke(this, "Connected to web interface for real-time sync");
            }
            catch (Exception ex)
            {
                ServerError?.Invoke(this, $"Failed to initialize SignalR connection: {ex.Message}");
            }
        }

        public async Task SendProfileUpdateAsync(KometaProfile profile)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("SendProfileUpdate", profile);
                }
                catch (Exception ex)
                {
                    ServerError?.Invoke(this, $"Failed to send profile update: {ex.Message}");
                }
            }
        }

        public async Task SendPageNavigationAsync(int pageIndex)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("SendPageNavigation", pageIndex);
                }
                catch (Exception ex)
                {
                    ServerError?.Invoke(this, $"Failed to send page navigation: {ex.Message}");
                }
            }
        }

        public async Task RequestKometaExecutionAsync(string profileName, string action)
        {
            if (_hubConnection?.State == HubConnectionState.Connected)
            {
                try
                {
                    await _hubConnection.InvokeAsync("RequestKometaExecution", profileName, action);
                }
                catch (Exception ex)
                {
                    ServerError?.Invoke(this, $"Failed to request Kometa execution: {ex.Message}");
                }
            }
        }

        private async Task<bool> BuildWebProjectAsync(string webAppPath)
        {
            try
            {
                var buildProcess = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "build --no-restore",
                    WorkingDirectory = webAppPath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = buildProcess };
                
                var output = new System.Text.StringBuilder();
                var error = new System.Text.StringBuilder();

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        output.AppendLine(e.Data);
                        ServerLogReceived?.Invoke(this, $"Build: {e.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        error.AppendLine(e.Data);
                        ServerLogReceived?.Invoke(this, $"Build Error: {e.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());

                if (process.ExitCode == 0)
                {
                    ServerLogReceived?.Invoke(this, "Web project built successfully");
                    return true;
                }
                else
                {
                    ServerError?.Invoke(this, $"Build failed with exit code {process.ExitCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ServerError?.Invoke(this, $"Build exception: {ex.Message}");
                return false;
            }
        }

        private string GetWebApplicationPath()
        {
            var currentDir = Application.StartupPath;
            ServerLogReceived?.Invoke(this, $"Application.StartupPath: {currentDir}");
            
            // Strategy 1: Standard path (3 levels up from bin/Debug/net8.0-windows)
            var solutionDir = Directory.GetParent(currentDir)?.Parent?.Parent?.FullName;
            ServerLogReceived?.Invoke(this, $"Solution dir (3 levels up): {solutionDir}");
            
            if (solutionDir != null)
            {
                var webAppPath = Path.Combine(solutionDir, "KometaGUIv3.Web");
                ServerLogReceived?.Invoke(this, $"Checking path: {webAppPath}");
                if (Directory.Exists(webAppPath))
                {
                    ServerLogReceived?.Invoke(this, $"Found web app at: {webAppPath}");
                    return webAppPath;
                }
            }

            // Strategy 2: 4 levels up (in case we're in bin/Debug/net8.0-windows)  
            var solutionDir4 = Directory.GetParent(currentDir)?.Parent?.Parent?.Parent?.FullName;
            ServerLogReceived?.Invoke(this, $"Solution dir (4 levels up): {solutionDir4}");
            
            if (solutionDir4 != null)
            {
                var webAppPath4 = Path.Combine(solutionDir4, "KometaGUIv3.Web");
                ServerLogReceived?.Invoke(this, $"Checking path: {webAppPath4}");
                if (Directory.Exists(webAppPath4))
                {
                    ServerLogReceived?.Invoke(this, $"Found web app at: {webAppPath4}");
                    return webAppPath4;
                }
            }

            // Strategy 3: Look for solution file and use its directory
            var currentSearch = new DirectoryInfo(currentDir);
            while (currentSearch != null && currentSearch.Parent != null)
            {
                var slnFile = currentSearch.GetFiles("*.sln").FirstOrDefault();
                if (slnFile != null)
                {
                    var webAppPath = Path.Combine(currentSearch.FullName, "KometaGUIv3.Web");
                    ServerLogReceived?.Invoke(this, $"Found solution at: {currentSearch.FullName}");
                    ServerLogReceived?.Invoke(this, $"Checking path: {webAppPath}");
                    if (Directory.Exists(webAppPath))
                    {
                        ServerLogReceived?.Invoke(this, $"Found web app at: {webAppPath}");
                        return webAppPath;
                    }
                }
                currentSearch = currentSearch.Parent;
            }
            
            // Final fallback - return the first attempted path for error reporting
            var fallbackPath = solutionDir != null ? Path.Combine(solutionDir, "KometaGUIv3.Web") : Path.Combine(currentDir, "KometaGUIv3.Web");
            ServerLogReceived?.Invoke(this, $"All strategies failed, using fallback: {fallbackPath}");
            return fallbackPath;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Task.Run(async () =>
                {
                    await StopServerAsync();
                });
                
                _disposed = true;
            }
        }
    }
}