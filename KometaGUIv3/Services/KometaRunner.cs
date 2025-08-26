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

        public async Task<bool> RunKometaAsync(KometaProfile profile, string configPath)
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    LogReceived?.Invoke(this, "Error: Configuration file not found.");
                    return false;
                }

                var kometaPath = FindKometaExecutable(profile.KometaDirectory);
                if (string.IsNullOrEmpty(kometaPath))
                {
                    LogReceived?.Invoke(this, "Error: Kometa executable not found.");
                    return false;
                }

                LogReceived?.Invoke(this, "Starting Kometa execution...");
                LogReceived?.Invoke(this, $"Kometa Path: {kometaPath}");
                LogReceived?.Invoke(this, $"Config Path: {configPath}");
                LogReceived?.Invoke(this, "=====================================");

                var startInfo = new ProcessStartInfo
                {
                    FileName = kometaPath,
                    Arguments = $"--config \"{configPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = profile.KometaDirectory
                };

                // Add Python path if using .py file
                if (kometaPath.EndsWith(".py"))
                {
                    startInfo.FileName = "python";
                    startInfo.Arguments = $"\"{kometaPath}\" --config \"{configPath}\"";
                }

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
                        LogReceived?.Invoke(this, $"ERROR: {e.Data}");
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

        private string FindKometaExecutable(string kometaDirectory)
        {
            if (string.IsNullOrEmpty(kometaDirectory) || !Directory.Exists(kometaDirectory))
                return null;

            // Check for common Kometa executable names
            var possibleExecutables = new[]
            {
                Path.Combine(kometaDirectory, "kometa.exe"),
                Path.Combine(kometaDirectory, "kometa.py"),
                Path.Combine(kometaDirectory, "main.py"),
                Path.Combine(kometaDirectory, "plex_meta_manager.py")
            };

            foreach (var executable in possibleExecutables)
            {
                if (File.Exists(executable))
                {
                    return executable;
                }
            }

            return null;
        }

        public bool IsRunning => kometaProcess != null && !kometaProcess.HasExited;
    }
}