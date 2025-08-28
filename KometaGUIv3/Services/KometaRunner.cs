using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using KometaGUIv3.Models;

namespace KometaGUIv3.Services
{
    public class KometaRunner
    {
        public event EventHandler<string> LogReceived;
        private Process kometaProcess;
        private readonly KometaInstaller kometaInstaller;

        public KometaRunner()
        {
            kometaInstaller = new KometaInstaller();
            kometaInstaller.LogReceived += (s, message) => LogReceived?.Invoke(this, message);
        }

        public async Task<bool> RunKometaAsync(KometaProfile profile, string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    LogReceived?.Invoke(this, "Error: Configuration file not found.");
                    return false;
                }

                // Check installation status first
                LogReceived?.Invoke(this, "Checking Kometa installation...");
                var installStatus = await kometaInstaller.CheckInstallationStatusAsync(profile.KometaDirectory);
                
                if (!installStatus.IsKometaInstalled)
                {
                    LogReceived?.Invoke(this, "Kometa is not installed. Please install it first using the Install button.");
                    return false;
                }

                if (!installStatus.IsVirtualEnvironmentReady)
                {
                    LogReceived?.Invoke(this, "Virtual environment is not ready. Please reinstall Kometa.");
                    return false;
                }

                if (!installStatus.AreDependenciesInstalled)
                {
                    LogReceived?.Invoke(this, "Dependencies are not installed. Please reinstall Kometa.");
                    return false;
                }

                // Use virtual environment Python
                var venvPython = Path.Combine(profile.KometaDirectory, "kometa-venv", "Scripts", "python.exe");
                var kometaScript = Path.Combine(profile.KometaDirectory, "kometa.py");

                if (!File.Exists(venvPython))
                {
                    LogReceived?.Invoke(this, "Virtual environment Python not found.");
                    return false;
                }

                if (!File.Exists(kometaScript))
                {
                    LogReceived?.Invoke(this, "Kometa script not found.");
                    return false;
                }

                LogReceived?.Invoke(this, "Starting Kometa execution with 3-step process...");
                LogReceived?.Invoke(this, $"Virtual Environment: {Path.Combine(profile.KometaDirectory, "kometa-venv")}");
                LogReceived?.Invoke(this, $"Kometa Script: {kometaScript}");
                LogReceived?.Invoke(this, $"Config Path: {configPath}");
                LogReceived?.Invoke(this, $"Working Directory: {profile.KometaDirectory}");
                LogReceived?.Invoke(this, "=====================================");
                LogReceived?.Invoke(this, "Step 1: Activating virtual environment...");
                LogReceived?.Invoke(this, "Step 2: Installing/updating requirements...");
                LogReceived?.Invoke(this, "Step 3: Running Kometa...");
                LogReceived?.Invoke(this, "=====================================");

                // Build the 3-step command sequence as requested:
                // 1. .\kometa-venv\Scripts\activate
                // 2. python -m pip install -r requirements.txt  
                // 3. python kometa.py -r
                var commandSequence = new StringBuilder();
                commandSequence.Append($"cd /d \"{profile.KometaDirectory}\" && ");
                commandSequence.Append(".\\kometa-venv\\Scripts\\activate && ");
                commandSequence.Append("python -m pip install -r requirements.txt && ");
                commandSequence.Append("python kometa.py -r");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c \"{commandSequence}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = profile.KometaDirectory
                };

                kometaProcess = new Process { StartInfo = startInfo };

                kometaProcess.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        LogReceived?.Invoke(this, e.Data);
                    }
                };

                kometaProcess.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        // Filter out common non-error messages that appear in stderr
                        var message = e.Data;
                        if (!message.Contains("UserWarning") && !message.Contains("DeprecationWarning"))
                        {
                            LogReceived?.Invoke(this, $"ERROR: {message}");
                        }
                        else
                        {
                            LogReceived?.Invoke(this, $"WARNING: {message}");
                        }
                    }
                };

                kometaProcess.Start();
                kometaProcess.BeginOutputReadLine();
                kometaProcess.BeginErrorReadLine();

                await Task.Run(() => kometaProcess.WaitForExit());

                LogReceived?.Invoke(this, "=====================================");
                LogReceived?.Invoke(this, $"Kometa execution completed. Exit code: {kometaProcess.ExitCode}");

                return kometaProcess.ExitCode == 0;
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke(this, $"Error running Kometa: {ex.Message}");
                return false;
            }
        }

        public void StopKometa()
        {
            if (kometaProcess != null && !kometaProcess.HasExited)
            {
                try
                {
                    kometaProcess.Kill();
                    LogReceived?.Invoke(this, "Kometa execution stopped by user.");
                }
                catch (Exception ex)
                {
                    LogReceived?.Invoke(this, $"Error stopping Kometa: {ex.Message}");
                }
            }
        }

        public bool IsRunning => kometaProcess != null && !kometaProcess.HasExited;
    }
}