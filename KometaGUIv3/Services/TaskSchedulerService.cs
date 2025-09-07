using System;
using System.Diagnostics;
using System.IO;
using KometaGUIv3.Shared.Models;

namespace KometaGUIv3.Services
{
    public class TaskSchedulerService
    {
        public bool CreateScheduledTask(KometaProfile profile, string configPath, ScheduleFrequency frequency, int interval, string time = "02:00")
        {
            try
            {
                var taskName = $"Kometa_{profile.Name}";
                
                // Validate Kometa directory exists
                if (string.IsNullOrEmpty(profile.KometaDirectory) || !Directory.Exists(profile.KometaDirectory))
                {
                    throw new Exception("Kometa directory not found or not set");
                }

                // Build the command to run using virtual environment Python
                var kometaDirectory = profile.KometaDirectory;
                var venvPythonPath = Path.Combine(kometaDirectory, "kometa-venv", "Scripts", "python.exe");
                var kometaPyPath = Path.Combine(kometaDirectory, "kometa.py");
                
                string command = venvPythonPath;
                string arguments = $"\"{kometaPyPath}\" -r";
                
                // Set working directory for the task
                var workingDirectory = kometaDirectory;

                // Convert time from HHMM to HH:MM format
                var formattedTime = time.Length == 4 ? $"{time.Substring(0, 2)}:{time.Substring(2, 2)}" : time;

                // Build schtasks command
                var scheduleType = frequency switch
                {
                    ScheduleFrequency.Daily => "DAILY",
                    ScheduleFrequency.Weekly => "WEEKLY",
                    ScheduleFrequency.Monthly => "MONTHLY",
                    _ => "DAILY"
                };

                var intervalParam = frequency switch
                {
                    ScheduleFrequency.Daily => interval > 1 ? $"/MO {interval}" : "",
                    ScheduleFrequency.Weekly => interval > 1 ? $"/MO {interval}" : "",
                    ScheduleFrequency.Monthly => $"/M {interval}",
                    _ => ""
                };

                var schtasksArgs = $"/CREATE /TN \"{taskName}\" /TR \"\\\"{command}\\\" {arguments}\" " +
                                 $"/SC {scheduleType} {(string.IsNullOrEmpty(intervalParam) ? "" : " " + intervalParam)} /ST {formattedTime} /SD {DateTime.Today:MM/dd/yyyy} /F";

                // Debug output
                System.Diagnostics.Debug.WriteLine($"Task Name: {taskName}");
                System.Diagnostics.Debug.WriteLine($"Command: {command}");
                System.Diagnostics.Debug.WriteLine($"Arguments: {arguments}");
                System.Diagnostics.Debug.WriteLine($"Full schtasks args: {schtasksArgs}");

                var processInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = schtasksArgs,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    
                    if (process.ExitCode != 0)
                    {
                        var error = process.StandardError.ReadToEnd();
                        var output = process.StandardOutput.ReadToEnd();
                        var errorDetails = !string.IsNullOrEmpty(error) ? error : output;
                        throw new Exception($"schtasks failed with exit code {process.ExitCode}: {errorDetails}");
                    }
                    
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create scheduled task: {ex.Message}");
            }
        }

        public bool DeleteScheduledTask(string profileName)
        {
            try
            {
                var taskName = $"Kometa_{profileName}";
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/DELETE /TN \"{taskName}\" /F",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete scheduled task: {ex.Message}");
            }
        }

        public bool TaskExists(string profileName)
        {
            try
            {
                var taskName = $"Kometa_{profileName}";
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = "schtasks.exe",
                    Arguments = $"/QUERY /TN \"{taskName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private string FindKometaExecutable(string kometaDirectory)
        {
            if (string.IsNullOrEmpty(kometaDirectory) || !System.IO.Directory.Exists(kometaDirectory))
                return null;

            var possibleExecutables = new[]
            {
                System.IO.Path.Combine(kometaDirectory, "kometa.exe"),
                System.IO.Path.Combine(kometaDirectory, "kometa.py"),
                System.IO.Path.Combine(kometaDirectory, "main.py"),
                System.IO.Path.Combine(kometaDirectory, "plex_meta_manager.py")
            };

            foreach (var executable in possibleExecutables)
            {
                if (System.IO.File.Exists(executable))
                {
                    return executable;
                }
            }

            return null;
        }
    }

    public enum ScheduleFrequency
    {
        Daily,
        Weekly, 
        Monthly
    }
}