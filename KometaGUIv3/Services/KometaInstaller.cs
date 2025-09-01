using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using KometaGUIv3.Shared.Models;

namespace KometaGUIv3.Services
{
    public class KometaInstaller
    {
        public event EventHandler<string> LogReceived;
        public event EventHandler<int> ProgressChanged;

        private readonly SystemRequirements systemRequirements;

        public KometaInstaller()
        {
            systemRequirements = new SystemRequirements();
            systemRequirements.LogReceived += (s, message) => LogMessage(message);
        }

        public class InstallationStatus
        {
            public bool IsKometaInstalled { get; set; }
            public bool IsVirtualEnvironmentReady { get; set; }
            public bool AreDependenciesInstalled { get; set; }
            public bool IsPythonReady { get; set; }
            public bool IsGitReady { get; set; }
            public string KometaVersion { get; set; }
            public string InstallationPath { get; set; }
            public string VirtualEnvironmentPath { get; set; }
            public string ErrorMessage { get; set; }
        }

        public async Task<InstallationStatus> CheckInstallationStatusAsync(string kometaDirectory)
        {
            LogMessage("Checking Kometa installation status...");
            
            var status = new InstallationStatus
            {
                InstallationPath = kometaDirectory
            };

            try
            {
                // Check system requirements first
                var systemCheck = await systemRequirements.CheckSystemRequirementsAsync();
                status.IsPythonReady = systemCheck.IsPythonInstalled;
                status.IsGitReady = systemCheck.IsGitInstalled;

                if (string.IsNullOrEmpty(kometaDirectory) || !Directory.Exists(kometaDirectory))
                {
                    status.ErrorMessage = "Kometa directory not specified or doesn't exist";
                    return status;
                }

                // Check for Kometa files
                var kometaMainFile = Path.Combine(kometaDirectory, "kometa.py");
                var requirementsFile = Path.Combine(kometaDirectory, "requirements.txt");
                var gitFolder = Path.Combine(kometaDirectory, ".git");
                var versionFile = Path.Combine(kometaDirectory, "VERSION");

                // Check if it's a valid Kometa installation (either git clone or release)
                status.IsKometaInstalled = File.Exists(kometaMainFile) && 
                                         File.Exists(requirementsFile) && 
                                         (Directory.Exists(gitFolder) || File.Exists(versionFile));

                if (status.IsKometaInstalled)
                {
                    // Try to get Kometa version
                    status.KometaVersion = await GetKometaVersionAsync(kometaDirectory);
                }

                // Check virtual environment
                var venvPath = Path.Combine(kometaDirectory, "kometa-venv");
                var venvPython = Path.Combine(venvPath, "Scripts", "python.exe");
                status.VirtualEnvironmentPath = venvPath;
                status.IsVirtualEnvironmentReady = Directory.Exists(venvPath) && File.Exists(venvPython);

                // Check dependencies if venv exists
                if (status.IsVirtualEnvironmentReady)
                {
                    status.AreDependenciesInstalled = await CheckDependenciesAsync(venvPython);
                }

                LogMessage($"Installation status - Kometa: {(status.IsKometaInstalled ? "✓" : "✗")}, VEnv: {(status.IsVirtualEnvironmentReady ? "✓" : "✗")}, Dependencies: {(status.AreDependenciesInstalled ? "✓" : "✗")}");
            }
            catch (Exception ex)
            {
                status.ErrorMessage = $"Error checking installation: {ex.Message}";
                LogMessage(status.ErrorMessage);
            }

            return status;
        }

        public async Task<bool> InstallKometaAsync(string kometaDirectory, bool forceReinstall = false)
        {
            try
            {
                LogMessage("Starting Kometa installation process...");
                ReportProgress(0);

                // Step 1: Check and install system requirements (20%)
                LogMessage("Step 1/5: Checking system requirements...");
                var systemCheck = await systemRequirements.CheckSystemRequirementsAsync();
                
                if (!systemCheck.IsPythonInstalled)
                {
                    LogMessage("Python not found. Installing Python...");
                    var pythonInstalled = await systemRequirements.InstallPythonAsync();
                    if (!pythonInstalled)
                    {
                        LogMessage("Failed to install Python. Installation cannot continue.");
                        return false;
                    }
                    
                    // Re-check after installation
                    systemCheck = await systemRequirements.CheckSystemRequirementsAsync();
                }

                if (!systemCheck.IsGitInstalled)
                {
                    LogMessage("Git not found. Installing Git...");
                    var gitInstalled = await systemRequirements.InstallGitAsync();
                    if (!gitInstalled)
                    {
                        LogMessage("Failed to install Git. Installation cannot continue.");
                        return false;
                    }
                }

                ReportProgress(20);

                // Step 2: Create directory and clone repository (40%)
                LogMessage("Step 2/5: Setting up Kometa directory...");
                if (!await SetupKometaDirectoryAsync(kometaDirectory, forceReinstall))
                {
                    return false;
                }
                ReportProgress(40);

                // Step 3: Clone Kometa repository (60%)
                LogMessage("Step 3/5: Cloning Kometa repository...");
                if (!await CloneKometaRepositoryAsync(kometaDirectory, forceReinstall))
                {
                    return false;
                }
                ReportProgress(60);

                // Step 4: Create virtual environment (80%)
                LogMessage("Step 4/5: Creating virtual environment...");
                if (!await CreateVirtualEnvironmentAsync(kometaDirectory))
                {
                    return false;
                }
                ReportProgress(80);

                // Step 5: Install dependencies (100%)
                LogMessage("Step 5/5: Installing dependencies...");
                if (!await InstallDependenciesAsync(kometaDirectory))
                {
                    return false;
                }
                ReportProgress(100);

                LogMessage("Kometa installation completed successfully!");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"Installation failed: {ex.Message}");
                return false;
            }
        }

        private bool IsExistingKometaInstallation(string kometaDirectory)
        {
            if (string.IsNullOrEmpty(kometaDirectory) || !Directory.Exists(kometaDirectory))
                return false;

            // Check for essential Kometa files
            var kometaScript = Path.Combine(kometaDirectory, "kometa.py");
            var requirementsFile = Path.Combine(kometaDirectory, "requirements.txt");

            // Core requirement: kometa.py and requirements.txt must exist
            if (!File.Exists(kometaScript) || !File.Exists(requirementsFile))
                return false;

            // Check for either git installation OR release installation
            var gitFolder = Path.Combine(kometaDirectory, ".git");
            var versionFile = Path.Combine(kometaDirectory, "VERSION");
            
            // Valid if it's either:
            // 1. A git clone (has .git folder)
            // 2. A release download (has VERSION file)
            return Directory.Exists(gitFolder) || File.Exists(versionFile);
        }

        private async Task<bool> SetupKometaDirectoryAsync(string kometaDirectory, bool forceReinstall)
        {
            try
            {
                if (Directory.Exists(kometaDirectory))
                {
                    // Check if this is an existing Kometa installation
                    if (IsExistingKometaInstallation(kometaDirectory))
                    {
                        if (forceReinstall)
                        {
                            LogMessage("Removing existing Kometa installation for clean reinstall...");
                            Directory.Delete(kometaDirectory, true);
                            await Task.Delay(1000); // Wait for deletion to complete
                        }
                        else
                        {
                            LogMessage("Existing Kometa installation detected. This will be updated/refreshed.");
                            return true; // Allow installation to proceed as update
                        }
                    }
                    else
                    {
                        // Directory exists but doesn't contain Kometa - check if it has content
                        bool hasContent = Directory.GetFiles(kometaDirectory).Length > 0 || 
                                         Directory.GetDirectories(kometaDirectory).Length > 0;
                        
                        if (hasContent && forceReinstall)
                        {
                            LogMessage("Clearing non-Kometa content from directory for clean installation...");
                            Directory.Delete(kometaDirectory, true);
                            await Task.Delay(1000);
                        }
                        else if (hasContent)
                        {
                            LogMessage("Directory contains files but no Kometa installation. Proceeding with installation...");
                            // Allow installation to proceed - Kometa files will be added alongside existing content
                        }
                        else
                        {
                            LogMessage("Empty directory found. Proceeding with installation...");
                        }
                    }
                }

                // Ensure directory exists
                Directory.CreateDirectory(kometaDirectory);
                LogMessage($"Kometa directory prepared: {kometaDirectory}");
                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"Error setting up directory: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> CloneKometaRepositoryAsync(string kometaDirectory, bool forceReinstall)
        {
            try
            {
                var kometaMainFile = Path.Combine(kometaDirectory, "kometa.py");
                if (File.Exists(kometaMainFile) && !forceReinstall)
                {
                    LogMessage("Kometa already cloned, skipping clone step");
                    return true;
                }

                LogMessage("Cloning Kometa repository from GitHub...");
                var result = await RunCommandAsync("git", $"clone https://github.com/Kometa-Team/Kometa.git \"{kometaDirectory}\"", ".");

                if (!result.success)
                {
                    // Try alternative if directory exists (pull instead of clone)
                    if (Directory.Exists(Path.Combine(kometaDirectory, ".git")))
                    {
                        LogMessage("Git repository exists, pulling latest changes...");
                        result = await RunCommandAsync("git", "pull origin master", kometaDirectory);
                    }
                }

                if (result.success)
                {
                    LogMessage("Kometa repository cloned/updated successfully");
                    return File.Exists(Path.Combine(kometaDirectory, "kometa.py"));
                }
                else
                {
                    LogMessage($"Failed to clone repository: {result.output}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error cloning repository: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> CreateVirtualEnvironmentAsync(string kometaDirectory)
        {
            try
            {
                var venvPath = Path.Combine(kometaDirectory, "kometa-venv");
                var venvPython = Path.Combine(venvPath, "Scripts", "python.exe");

                if (File.Exists(venvPython))
                {
                    LogMessage("Virtual environment already exists, skipping creation");
                    return true;
                }

                LogMessage("Creating Python virtual environment...");
                var result = await RunCommandAsync("python", $"-m venv \"{venvPath}\"", kometaDirectory);

                if (!result.success)
                {
                    // Try with py launcher
                    LogMessage("Retrying with py launcher...");
                    result = await RunCommandAsync("py", $"-m venv \"{venvPath}\"", kometaDirectory);
                }

                if (result.success && File.Exists(venvPython))
                {
                    LogMessage("Virtual environment created successfully");
                    return true;
                }
                else
                {
                    LogMessage($"Failed to create virtual environment: {result.output}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error creating virtual environment: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> InstallDependenciesAsync(string kometaDirectory)
        {
            try
            {
                var venvPython = Path.Combine(kometaDirectory, "kometa-venv", "Scripts", "python.exe");
                var requirementsFile = Path.Combine(kometaDirectory, "requirements.txt");

                if (!File.Exists(venvPython))
                {
                    LogMessage("Virtual environment not found");
                    return false;
                }

                if (!File.Exists(requirementsFile))
                {
                    LogMessage("Requirements file not found");
                    return false;
                }

                LogMessage("Installing Python dependencies (this may take several minutes)...");
                
                // Upgrade pip first
                LogMessage("Upgrading pip...");
                var pipUpgrade = await RunCommandAsync(venvPython, "-m pip install --upgrade pip", kometaDirectory);
                if (pipUpgrade.success)
                {
                    LogMessage("Pip upgraded successfully");
                }

                // Install requirements
                LogMessage("Installing requirements from requirements.txt...");
                var result = await RunCommandAsync(venvPython, $"-m pip install -r \"{requirementsFile}\"", kometaDirectory);

                if (result.success)
                {
                    LogMessage("Dependencies installed successfully");
                    return true;
                }
                else
                {
                    LogMessage($"Failed to install dependencies: {result.output}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error installing dependencies: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> CheckDependenciesAsync(string pythonPath)
        {
            try
            {
                // Try to import main Kometa modules
                var result = await RunCommandAsync(pythonPath, "-c \"import plexapi, requests, ruamel.yaml; print('Dependencies OK')\"", ".");
                return result.success && result.output.Contains("Dependencies OK");
            }
            catch
            {
                return false;
            }
        }

        private async Task<string> GetKometaVersionAsync(string kometaDirectory)
        {
            try
            {
                var versionFile = Path.Combine(kometaDirectory, "VERSION");
                if (File.Exists(versionFile))
                {
                    var version = await File.ReadAllTextAsync(versionFile);
                    return version.Trim();
                }

                // Try to get version from git
                var result = await RunCommandAsync("git", "describe --tags --abbrev=0", kometaDirectory);
                if (result.success && !string.IsNullOrWhiteSpace(result.output))
                {
                    return result.output.Trim();
                }
            }
            catch { }

            return "Unknown";
        }

        public async Task<bool> UpdateKometaAsync(string kometaDirectory)
        {
            try
            {
                LogMessage("Updating Kometa to latest version...");
                
                var gitPath = Path.Combine(kometaDirectory, ".git");
                if (!Directory.Exists(gitPath))
                {
                    LogMessage("Not a git repository, cannot update");
                    return false;
                }

                // Pull latest changes
                var result = await RunCommandAsync("git", "pull origin master", kometaDirectory);
                if (!result.success)
                {
                    LogMessage($"Failed to pull updates: {result.output}");
                    return false;
                }

                // Update dependencies if requirements.txt changed
                LogMessage("Updating dependencies...");
                var venvPython = Path.Combine(kometaDirectory, "kometa-venv", "Scripts", "python.exe");
                var requirementsFile = Path.Combine(kometaDirectory, "requirements.txt");
                
                if (File.Exists(venvPython) && File.Exists(requirementsFile))
                {
                    var updateResult = await RunCommandAsync(venvPython, $"-m pip install -r \"{requirementsFile}\" --upgrade", kometaDirectory);
                    if (updateResult.success)
                    {
                        LogMessage("Kometa updated successfully!");
                        return true;
                    }
                    else
                    {
                        LogMessage($"Failed to update dependencies: {updateResult.output}");
                        return false;
                    }
                }
                else
                {
                    LogMessage("Virtual environment or requirements file missing");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating Kometa: {ex.Message}");
                return false;
            }
        }

        private async Task<(bool success, string output)> RunCommandAsync(string fileName, string arguments, string workingDirectory)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory
                };

                using (var process = Process.Start(startInfo))
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    
                    await Task.Run(() => process.WaitForExit());
                    
                    var fullOutput = output + error;
                    
                    // Log command output for debugging
                    if (!string.IsNullOrWhiteSpace(fullOutput))
                    {
                        LogMessage($"Command output: {fullOutput.Trim()}");
                    }
                    
                    return (process.ExitCode == 0, fullOutput);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Command failed: {fileName} {arguments} - {ex.Message}");
                return (false, ex.Message);
            }
        }

        private void LogMessage(string message)
        {
            LogReceived?.Invoke(this, message);
        }

        private void ReportProgress(int percentage)
        {
            ProgressChanged?.Invoke(this, percentage);
        }
    }
}