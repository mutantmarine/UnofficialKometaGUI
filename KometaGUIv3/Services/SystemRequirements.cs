using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace KometaGUIv3.Services
{
    public class SystemRequirements
    {
        public event EventHandler<string> LogReceived;
        private readonly HttpClient httpClient;

        public SystemRequirements()
        {
            httpClient = new HttpClient();
        }

        public class SystemCheck
        {
            public bool IsPythonInstalled { get; set; }
            public string PythonVersion { get; set; }
            public string PythonPath { get; set; }
            public bool IsGitInstalled { get; set; }
            public string GitVersion { get; set; }
            public string GitPath { get; set; }
            public bool IsPowerShellAvailable { get; set; }
        }

        public async Task<SystemCheck> CheckSystemRequirementsAsync()
        {
            try
            {
                LogMessage("Checking system requirements...");
                
                var check = new SystemCheck
                {
                    IsPowerShellAvailable = await CheckPowerShellAsync()
                };

                // Check Python
                await CheckPythonInstallation(check);
                
                // Check Git
                await CheckGitInstallation(check);

                LogMessage($"System check complete - Python: {(check.IsPythonInstalled ? "✓" : "✗")}, Git: {(check.IsGitInstalled ? "✓" : "✗")}");
                
                return check;
            }
            catch (Exception ex)
            {
                LogMessage($"Error during system requirements check: {ex.Message}");
                // Return a safe default state
                return new SystemCheck
                {
                    IsPythonInstalled = false,
                    IsGitInstalled = false,
                    IsPowerShellAvailable = false
                };
            }
        }

        private async Task CheckPythonInstallation(SystemCheck check)
        {
            if (check == null)
            {
                LogMessage("Error: SystemCheck object is null");
                return;
            }

            try
            {
                LogMessage("Checking Python installation...");
                
                // First try to run python --version
                var result = await RunCommandAsync("python", "--version");
                if (result.success && !string.IsNullOrEmpty(result.output))
                {
                    var match = Regex.Match(result.output, @"Python (\d+\.\d+\.\d+)");
                    if (match.Success)
                    {
                        var version = match.Groups[1].Value;
                        var versionParts = version.Split('.');
                        if (versionParts.Length >= 2)
                        {
                            var major = int.Parse(versionParts[0]);
                            var minor = int.Parse(versionParts[1]);
                            
                            // Check if version is 3.9 - 3.13
                            if (major == 3 && minor >= 9 && minor <= 13)
                            {
                                check.IsPythonInstalled = true;
                                check.PythonVersion = version;
                                check.PythonPath = await GetPythonPath();
                                LogMessage($"Python {version} found and compatible");
                                return;
                            }
                            else
                            {
                                LogMessage($"Python {version} found but not compatible (requires 3.9-3.13)");
                            }
                        }
                    }
                }

                // Try py launcher as fallback
                result = await RunCommandAsync("py", "--version");
                if (result.success && !string.IsNullOrEmpty(result.output))
                {
                    var match = Regex.Match(result.output, @"Python (\d+\.\d+\.\d+)");
                    if (match.Success)
                    {
                        var version = match.Groups[1].Value;
                        var versionParts = version.Split('.');
                        if (versionParts.Length >= 2)
                        {
                            var major = int.Parse(versionParts[0]);
                            var minor = int.Parse(versionParts[1]);
                            
                            if (major == 3 && minor >= 9 && minor <= 13)
                            {
                                check.IsPythonInstalled = true;
                                check.PythonVersion = version;
                                check.PythonPath = await GetPythonPath("py");
                                LogMessage($"Python {version} found via py launcher and compatible");
                                return;
                            }
                        }
                    }
                }

                // Check registry as final fallback
                CheckPythonInRegistry(check);
            }
            catch (Exception ex)
            {
                LogMessage($"Error checking Python installation: {ex.Message}");
            }
        }

        private void CheckPythonInRegistry(SystemCheck check)
        {
            try
            {
                // Check common Python installation paths in registry
                var registryPaths = new[]
                {
                    @"SOFTWARE\Python\PythonCore",
                    @"SOFTWARE\WOW6432Node\Python\PythonCore"
                };

                foreach (var registryPath in registryPaths)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(registryPath))
                    {
                        if (key != null)
                        {
                            foreach (var versionKeyName in key.GetSubKeyNames())
                            {
                                if (versionKeyName.StartsWith("3."))
                                {
                                    var versionParts = versionKeyName.Split('.');
                                    if (versionParts.Length >= 2 && int.TryParse(versionParts[1], out int minor))
                                    {
                                        if (minor >= 9 && minor <= 13)
                                        {
                                            using (var versionKey = key.OpenSubKey($"{versionKeyName}\\InstallPath"))
                                            {
                                                if (versionKey != null)
                                                {
                                                    var installPath = versionKey.GetValue("") as string;
                                                    if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
                                                    {
                                                        var pythonExe = Path.Combine(installPath, "python.exe");
                                                        if (File.Exists(pythonExe))
                                                        {
                                                            check.IsPythonInstalled = true;
                                                            check.PythonVersion = versionKeyName;
                                                            check.PythonPath = pythonExe;
                                                            LogMessage($"Python {versionKeyName} found in registry");
                                                            return;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error checking registry for Python: {ex.Message}");
            }
        }

        private async Task CheckGitInstallation(SystemCheck check)
        {
            if (check == null)
            {
                LogMessage("Error: SystemCheck object is null for Git check");
                return;
            }

            try
            {
                LogMessage("Checking Git installation...");
                
                var result = await RunCommandAsync("git", "--version");
                if (result.success && !string.IsNullOrEmpty(result.output))
                {
                    var match = Regex.Match(result.output, @"git version ([\d\.]+)");
                    if (match.Success)
                    {
                        check.IsGitInstalled = true;
                        check.GitVersion = match.Groups[1].Value;
                        check.GitPath = await GetGitPath();
                        LogMessage($"Git {check.GitVersion} found");
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error checking Git installation: {ex.Message}");
            }
        }

        private async Task<string> GetPythonPath(string command = "python")
        {
            try
            {
                var result = await RunCommandAsync("where", command);
                if (result.success && !string.IsNullOrEmpty(result.output))
                {
                    var lines = result.output.Split('\n');
                    return lines[0].Trim();
                }
            }
            catch { }
            return command;
        }

        private async Task<string> GetGitPath()
        {
            try
            {
                var result = await RunCommandAsync("where", "git");
                if (result.success && !string.IsNullOrEmpty(result.output))
                {
                    var lines = result.output.Split('\n');
                    return lines[0].Trim();
                }
            }
            catch { }
            return "git";
        }

        private async Task<bool> CheckPowerShellAsync()
        {
            try
            {
                var result = await RunCommandAsync("powershell", "-Command \"Write-Output 'test'\"");
                return result.success;
            }
            catch (Exception ex)
            {
                LogMessage($"PowerShell check failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> InstallPythonAsync()
        {
            try
            {
                LogMessage("Starting Python installation...");
                LogMessage("Downloading Python installer from python.org...");

                // Download Python installer (latest 3.12.x)
                var pythonUrl = "https://www.python.org/ftp/python/3.12.7/python-3.12.7-amd64.exe";
                var tempPath = Path.GetTempFileName() + ".exe";

                using (var response = await httpClient.GetAsync(pythonUrl))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (var fileStream = File.Create(tempPath))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                        LogMessage("Python installer downloaded successfully");
                    }
                    else
                    {
                        LogMessage($"Failed to download Python installer: {response.StatusCode}");
                        return false;
                    }
                }

                // Run installer silently
                LogMessage("Installing Python (this may take a few minutes)...");
                var installArgs = "/quiet InstallAllUsers=1 PrependPath=1 Include_test=0 Include_launcher=1";
                
                var result = await RunCommandAsync(tempPath, installArgs);
                
                // Clean up installer
                try { File.Delete(tempPath); } catch { }

                if (result.success)
                {
                    LogMessage("Python installation completed successfully");
                    // Wait a moment for system to update PATH
                    await Task.Delay(2000);
                    return true;
                }
                else
                {
                    LogMessage($"Python installation failed: {result.output}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error installing Python: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> InstallGitAsync()
        {
            try
            {
                LogMessage("Starting Git installation...");
                LogMessage("Downloading Git installer...");

                // Download Git installer
                var gitUrl = "https://github.com/git-for-windows/git/releases/download/v2.46.0.windows.1/Git-2.46.0-64-bit.exe";
                var tempPath = Path.GetTempFileName() + ".exe";

                using (var response = await httpClient.GetAsync(gitUrl))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (var fileStream = File.Create(tempPath))
                        {
                            await response.Content.CopyToAsync(fileStream);
                        }
                        LogMessage("Git installer downloaded successfully");
                    }
                    else
                    {
                        LogMessage($"Failed to download Git installer: {response.StatusCode}");
                        return false;
                    }
                }

                // Run installer silently
                LogMessage("Installing Git (this may take a few minutes)...");
                var installArgs = "/SILENT /COMPONENTS=\"icons,ext\\reg\\shellhere,assoc,assoc_sh\"";
                
                var result = await RunCommandAsync(tempPath, installArgs);
                
                // Clean up installer
                try { File.Delete(tempPath); } catch { }

                if (result.success)
                {
                    LogMessage("Git installation completed successfully");
                    // Wait a moment for system to update PATH
                    await Task.Delay(2000);
                    return true;
                }
                else
                {
                    LogMessage($"Git installation failed: {result.output}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error installing Git: {ex.Message}");
                return false;
            }
        }

        private async Task<(bool success, string output)> RunCommandAsync(string fileName, string arguments)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return (false, "Filename cannot be null or empty");
                }

                LogMessage($"Running command: {fileName} {arguments}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments ?? "",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return (false, "Failed to start process");
                    }

                    // Add timeout to prevent hanging
                    var outputTask = process.StandardOutput.ReadToEndAsync();
                    var errorTask = process.StandardError.ReadToEndAsync();
                    
                    // Wait for process with 30-second timeout
                    var processTask = Task.Run(() => process.WaitForExit(30000));
                    
                    var output = await outputTask;
                    var error = await errorTask;
                    var processCompleted = await processTask;
                    
                    if (!processCompleted)
                    {
                        try
                        {
                            process.Kill();
                            LogMessage($"Process {fileName} timed out and was killed");
                        }
                        catch { }
                        return (false, "Process timed out after 30 seconds");
                    }
                    
                    var fullOutput = output + error;
                    var success = process.ExitCode == 0;
                    
                    LogMessage($"Command completed with exit code: {process.ExitCode}");
                    
                    return (success, fullOutput);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Command execution failed: {ex.Message}");
                return (false, ex.Message);
            }
        }

        private void LogMessage(string message)
        {
            try
            {
                if (!string.IsNullOrEmpty(message))
                {
                    LogReceived?.Invoke(this, message);
                }
            }
            catch
            {
                // Ignore logging errors to prevent cascade failures
            }
        }

        public void Dispose()
        {
            httpClient?.Dispose();
        }
    }
}