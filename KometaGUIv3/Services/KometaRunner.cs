using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using KometaGUIv3.Shared.Models;

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
                    LogReceived?.Invoke(this, "Stopping Kometa and all related processes...");
                    
                    // Kill the entire process tree to ensure all child processes are terminated
                    KillProcessTree(kometaProcess.Id);
                    
                    // Also kill any remaining python processes that might be running kometa.py
                    KillKometaPythonProcesses();
                    
                    LogReceived?.Invoke(this, "Kometa execution stopped by user.");
                }
                catch (Exception ex)
                {
                    LogReceived?.Invoke(this, $"Error stopping Kometa: {ex.Message}");
                }
            }
        }

        private void KillProcessTree(int processId)
        {
            try
            {
                // Use WMI to find and kill all child processes
                using (var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_Process WHERE ParentProcessId={processId}"))
                {
                    using (var results = searcher.Get())
                    {
                        foreach (ManagementObject mo in results)
                        {
                            var childProcessId = Convert.ToInt32(mo["ProcessId"]);
                            KillProcessTree(childProcessId); // Recursively kill child processes
                            
                            try
                            {
                                var childProcess = Process.GetProcessById(childProcessId);
                                if (!childProcess.HasExited)
                                {
                                    childProcess.Kill();
                                    LogReceived?.Invoke(this, $"Terminated child process: {childProcess.ProcessName} (ID: {childProcessId})");
                                }
                            }
                            catch (Exception ex)
                            {
                                LogReceived?.Invoke(this, $"Could not terminate child process {childProcessId}: {ex.Message}");
                            }
                        }
                    }
                }
                
                // Kill the parent process
                try
                {
                    var process = Process.GetProcessById(processId);
                    if (!process.HasExited)
                    {
                        process.Kill();
                        LogReceived?.Invoke(this, $"Terminated parent process: {process.ProcessName} (ID: {processId})");
                    }
                }
                catch (Exception ex)
                {
                    LogReceived?.Invoke(this, $"Could not terminate parent process {processId}: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke(this, $"Error killing process tree: {ex.Message}");
            }
        }

        private void KillKometaPythonProcesses()
        {
            try
            {
                // Find any python processes that might be running kometa.py
                var pythonProcesses = Process.GetProcessesByName("python");
                
                foreach (var process in pythonProcesses)
                {
                    try
                    {
                        // Check if this python process has kometa in its command line
                        using (var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
                        {
                            using (var results = searcher.Get())
                            {
                                foreach (ManagementObject mo in results)
                                {
                                    var commandLine = mo["CommandLine"]?.ToString() ?? "";
                                    if (commandLine.Contains("kometa.py"))
                                    {
                                        if (!process.HasExited)
                                        {
                                            process.Kill();
                                            LogReceived?.Invoke(this, $"Terminated Kometa Python process (ID: {process.Id})");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogReceived?.Invoke(this, $"Could not check/terminate Python process {process.Id}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke(this, $"Error killing Kometa Python processes: {ex.Message}");
            }
        }

        public bool IsRunning => kometaProcess != null && !kometaProcess.HasExited;
    }
}