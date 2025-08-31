using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using KometaGUIv3.Models;

namespace KometaGUIv3.Services
{
    public class KometaRunner
    {
        public event EventHandler<string> LogReceived;
        private Process kometaProcess;

        public KometaRunner()
        {
        }

        public async Task<bool> RunKometaAsync(KometaProfile profile, string configPath)
        {
            try
            {
                // Basic validation - just check if Kometa directory exists and has kometa.py
                if (!Directory.Exists(profile.KometaDirectory))
                {
                    LogReceived?.Invoke(this, "Error: Kometa directory not found.");
                    return false;
                }

                var kometaScript = Path.Combine(profile.KometaDirectory, "kometa.py");
                if (!File.Exists(kometaScript))
                {
                    LogReceived?.Invoke(this, "Error: kometa.py not found in the specified directory.");
                    return false;
                }

                LogReceived?.Invoke(this, "Starting Kometa execution...");
                LogReceived?.Invoke(this, $"Kometa Directory: {profile.KometaDirectory}");
                LogReceived?.Invoke(this, "=====================================");

                // Use PowerShell with your proven script sequence
                var scriptPath = profile.KometaDirectory.Replace("'", "''"); // Escape single quotes for PowerShell
                var powershellCommand = $@"
                    Set-Location -Path '{scriptPath}'
                    if (-not (Test-Path 'kometa-venv')) {{
                        Write-Output 'Creating virtual environment...'
                        python -m venv kometa-venv
                    }}
                    Write-Output 'Activating virtual environment...'
                    & '{scriptPath}\kometa-venv\Scripts\Activate.ps1'
                    Write-Output 'Installing/updating requirements...'
                    python -m pip install -r requirements.txt
                    Write-Output 'Running Kometa...'
                    python kometa.py -r
                    Write-Output 'Deactivating virtual environment...'
                    deactivate
                ";

                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{powershellCommand}\"",
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
                        LogReceived?.Invoke(this, e.Data);
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
                    
                    // Deactivate virtual environment after stopping
                    LogReceived?.Invoke(this, "Deactivating virtual environment...");
                    DeactivateVirtualEnvironment();
                }
                catch (Exception ex)
                {
                    LogReceived?.Invoke(this, $"Error stopping Kometa: {ex.Message}");
                }
            }
        }

        private async void DeactivateVirtualEnvironment()
        {
            try
            {
                var deactivateCommand = "deactivate";
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"{deactivateCommand}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    await Task.Run(() => process.WaitForExit());
                    LogReceived?.Invoke(this, "Virtual environment deactivated.");
                }
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke(this, $"Error deactivating virtual environment: {ex.Message}");
            }
        }

        public bool IsRunning => kometaProcess != null && !kometaProcess.HasExited;
    }
}