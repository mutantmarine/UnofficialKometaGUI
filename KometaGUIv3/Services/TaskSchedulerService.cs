using System;
using System.Diagnostics;
using KometaGUIv3.Shared.Models;

namespace KometaGUIv3.Services
{
    public class TaskSchedulerService
    {
        public bool CreateScheduledTask(KometaProfile profile, string configPath, ScheduleFrequency frequency, int interval)
        {
            try
            {
                var taskName = $"Kometa_{profile.Name}";
                var kometaPath = FindKometaExecutable(profile.KometaDirectory);
                
                if (string.IsNullOrEmpty(kometaPath))
                {
                    throw new Exception("Kometa executable not found");
                }

                // Build the command to run
                string command;
                string arguments;
                
                if (kometaPath.EndsWith(".py"))
                {
                    command = "python";
                    arguments = $"\"{kometaPath}\" --config \"{configPath}\"";
                }
                else
                {
                    command = kometaPath;
                    arguments = $"--config \"{configPath}\"";
                }

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
                    ScheduleFrequency.Daily => $"/RI {interval}",
                    ScheduleFrequency.Weekly => $"/RI {interval}",
                    ScheduleFrequency.Monthly => $"/M {interval}",
                    _ => "/RI 1"
                };

                var schtasksArgs = $"/CREATE /TN \"{taskName}\" /TR \"\\\"{command}\\\" {arguments}\" " +
                                 $"/SC {scheduleType} {intervalParam} /ST 02:00 /F";

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
                    return process.ExitCode == 0;
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