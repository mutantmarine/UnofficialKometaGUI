using System;
#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using KometaGUIv3.Shared.Models;
using KometaGUIv3.Shared.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace KometaGUIv3.Services
{
    public class LocalServerHost : IDisposable
    {
        private readonly ProfileManager profileManager;
        private readonly KometaRunner kometaRunner;
        private readonly YamlGenerator yamlGenerator;
        private readonly TaskSchedulerService taskScheduler;
        private readonly KometaInstaller kometaInstaller;
        private readonly SystemRequirements systemRequirements;
        private readonly PlexService plexService;
        private readonly PlexOAuthService plexOauthService;
        private readonly SemaphoreSlim profileLock = new(1, 1);
        private readonly object logLock = new();
        private readonly List<LocalServerLogEntry> logBuffer = new();
        private readonly string baseUrl;

        private WebApplication? app;
        private CancellationTokenSource? cancellationSource;
        private Task? runTask;
        private KometaProfile? activeProfile;
        private int lastInstallerProgress;
        private bool disposed;

        public event EventHandler<bool>? StatusChanged;
        public event EventHandler<LocalServerLogEntry>? LogBroadcast;
        public event EventHandler<KometaProfile>? ActiveProfileChanged;

        public bool IsRunning => app != null;
        public string BaseUrl => baseUrl;

        public LocalServerHost(
            ProfileManager profileManager,
            KometaRunner kometaRunner,
            YamlGenerator yamlGenerator,
            TaskSchedulerService taskScheduler,
            KometaInstaller kometaInstaller,
            SystemRequirements systemRequirements)
        {
            this.profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
            this.kometaRunner = kometaRunner ?? throw new ArgumentNullException(nameof(kometaRunner));
            this.yamlGenerator = yamlGenerator ?? throw new ArgumentNullException(nameof(yamlGenerator));
            this.taskScheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));
            this.kometaInstaller = kometaInstaller ?? throw new ArgumentNullException(nameof(kometaInstaller));
            this.systemRequirements = systemRequirements ?? throw new ArgumentNullException(nameof(systemRequirements));

            plexService = new PlexService();
            plexOauthService = new PlexOAuthService();
            baseUrl = "http://localhost:5757";

            kometaRunner.LogReceived += OnKometaRunnerLog;
            systemRequirements.LogReceived += OnSystemRequirementsLog;
            kometaInstaller.LogReceived += OnInstallerLog;
            kometaInstaller.ProgressChanged += OnInstallerProgress;
        }

        public void AttachProfile(KometaProfile profile)
        {
            activeProfile = profile ?? throw new ArgumentNullException(nameof(profile));
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (IsRunning)
            {
                return Task.CompletedTask;
            }

            if (activeProfile == null)
            {
                throw new InvalidOperationException("An active profile must be assigned before starting the server.");
            }

            cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls(baseUrl);
            builder.Logging.ClearProviders();

            builder.Services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

            var webRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Web");
            if (!Directory.Exists(webRoot))
            {
                Directory.CreateDirectory(webRoot);
            }

            var application = builder.Build();

            ConfigureStaticFiles(application, webRoot);
            ConfigureEndpoints(application);

            runTask = application.RunAsync(cancellationSource.Token);
            app = application;

            StatusChanged?.Invoke(this, true);
            RecordLog("Server", $"Local server started at {baseUrl}");

            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            if (!IsRunning)
            {
                return;
            }

            try
            {
                cancellationSource?.Cancel();

                if (app != null)
                {
                    await app.StopAsync();
                    await app.DisposeAsync();
                }

                if (runTask != null)
                {
                    await Task.WhenAny(runTask, Task.Delay(1000));
                    runTask = null;
                }
            }
            finally
            {
                cancellationSource?.Dispose();
                cancellationSource = null;
                app = null;

                StatusChanged?.Invoke(this, false);
                RecordLog("Server", "Local server stopped");
            }
        }

        public void RecordLog(string source, string message, bool broadcast = true)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            var entry = new LocalServerLogEntry(DateTime.UtcNow, source, message);

            lock (logLock)
            {
                logBuffer.Add(entry);
                if (logBuffer.Count > 500)
                {
                    logBuffer.RemoveRange(0, logBuffer.Count - 500);
                }
            }

            if (broadcast)
            {
                LogBroadcast?.Invoke(this, entry);
            }
        }

        public int GetLogCount()
        {
            lock (logLock)
            {
                return logBuffer.Count;
            }
        }

        public async Task UpdateActiveProfileAsync(KometaProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            await profileLock.WaitAsync();
            try
            {
                activeProfile = profile;
                profileManager.SaveProfile(activeProfile);
            }
            finally
            {
                profileLock.Release();
            }

            ActiveProfileChanged?.Invoke(this, profile);
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            kometaRunner.LogReceived -= OnKometaRunnerLog;
            systemRequirements.LogReceived -= OnSystemRequirementsLog;
            kometaInstaller.LogReceived -= OnInstallerLog;
            kometaInstaller.ProgressChanged -= OnInstallerProgress;

            profileLock.Dispose();
            plexOauthService.Dispose();

            try
            {
                StopAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // Suppress dispose errors
            }
        }

        private void ConfigureStaticFiles(WebApplication application, string webRoot)
        {
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".json"] = "application/json";
            provider.Mappings[".wasm"] = "application/wasm";

            application.UseDefaultFiles(new DefaultFilesOptions
            {
                DefaultFileNames = new List<string> { "profile.html" },
                FileProvider = new PhysicalFileProvider(webRoot)
            });

            application.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(webRoot),
                ContentTypeProvider = provider,
                ServeUnknownFileTypes = true
            });
        }

        private void ConfigureEndpoints(WebApplication application)
        {
            application.MapGet("/", context =>
            {
                context.Response.Redirect("/profile.html");
                return Task.CompletedTask;
            });

            application.MapGet("/api/status", () =>
            {
                return Results.Json(new
                {
                    running = true,
                    baseUrl,
                    profile = activeProfile?.Name,
                    logCount = GetLogCount(),
                    installerProgress = lastInstallerProgress,
                    scheduleEnabled = activeProfile != null && taskScheduler.TaskExists(activeProfile.Name)
                });
            });

            application.MapGet("/api/profiles", () =>
            {
                var profiles = profileManager.GetProfileNames();
                return Results.Json(new { profiles, active = activeProfile?.Name });
            });

            application.MapPost("/api/profiles", async (CreateProfileRequest request) =>
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                {
                    return Results.BadRequest(new { error = "Profile name is required." });
                }

                try
                {
                    var profile = profileManager.CreateProfile(request.Name.Trim());
                    await UpdateActiveProfileAsync(profile);
                    RecordLog("Server", $"Profile '{profile.Name}' created.");
                    return Results.Ok(new { profile = profile.Name });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            });

            application.MapDelete("/api/profiles/{name}", (string name) =>
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Results.BadRequest(new { error = "Profile name is required." });
                }

                if (activeProfile != null && string.Equals(activeProfile.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return Results.BadRequest(new { error = "Cannot delete the active profile." });
                }

                profileManager.DeleteProfile(name);
                RecordLog("Server", $"Profile '{name}' deleted.");
                return Results.Ok();
            });

            application.MapPost("/api/profiles/select", async (SelectProfileRequest request) =>
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Name))
                {
                    return Results.BadRequest(new { error = "Profile name is required." });
                }

                var profile = profileManager.LoadProfile(request.Name.Trim());
                if (profile == null)
                {
                    return Results.NotFound(new { error = $"Profile '{request.Name}' not found." });
                }

                await UpdateActiveProfileAsync(profile);
                RecordLog("Server", $"Switched to profile '{profile.Name}'.");
                return Results.Ok();
            });

            application.MapGet("/api/profile", async () =>
            {
                if (activeProfile == null)
                {
                    return Results.NotFound(new { error = "No active profile." });
                }

                var snapshot = await CloneProfileAsync();
                return Results.Json(snapshot);
            });

            application.MapPut("/api/profile", async (KometaProfile updatedProfile) =>
            {
                if (updatedProfile == null)
                {
                    return Results.BadRequest(new { error = "Profile payload is required." });
                }

                if (activeProfile == null)
                {
                    return Results.NotFound(new { error = "No active profile to update." });
                }

                await ApplyProfileUpdateAsync(updatedProfile);
                RecordLog("Server", $"Profile '{activeProfile.Name}' updated.");
                return Results.Ok();
            });

            application.MapGet("/api/defaults/collections", () =>
            {
                return Results.Json(new
                {
                    charts = KometaDefaults.ChartCollections,
                    awards = KometaDefaults.AwardCollections,
                    movies = KometaDefaults.MovieCollections,
                    shows = KometaDefaults.ShowCollections,
                    both = KometaDefaults.BothCollections
                });
            });

            application.MapGet("/api/defaults/overlays", () =>
            {
                return Results.Json(new
                {
                    overlays = OverlayDefaults.AllOverlays,
                    mediaMap = OverlayDefaults.MediaTypeOverlays,
                    positions = OverlayDefaults.PositionDescriptions,
                    builderLevels = OverlayDefaults.BuilderLevels,
                    defaultRatings = OverlayDefaults.DefaultRatingConfigs
                });
            });

            application.MapPost("/api/plex/validate-token", async (ValidateTokenRequest request) =>
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Token))
                {
                    return Results.BadRequest(new { error = "Token is required." });
                }

                var isValid = await plexService.ValidateToken(request.Token.Trim());
                return Results.Ok(new { isValid });
            });

            application.MapPost("/api/plex/servers", async (ValidateTokenRequest request) =>
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Token))
                {
                    return Results.BadRequest(new { error = "Token is required." });
                }

                try
                {
                    var servers = await plexService.GetServerList(request.Token.Trim());
                    return Results.Json(servers);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            });

            application.MapPost("/api/plex/oauth/start", async () =>
            {
                try
                {
                    var token = await plexOauthService.AuthenticateWithBrowser();
                    return Results.Ok(new { token });
                }
                catch (Exception ex)
                {
                    RecordLog("Plex OAuth", ex.Message);
                    return Results.BadRequest(new { error = ex.Message });
                }
            });

            application.MapPost("/api/plex/oauth/cancel", () =>
            {
                plexOauthService.CancelCurrentAuthentication();
                RecordLog("Plex OAuth", "Authentication cancelled by request.");
                return Results.Ok();
            });

            application.MapPost("/api/plex/libraries", async (LibrariesRequest request) =>
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.ServerUrl))
                {
                    return Results.BadRequest(new { error = "Token and serverUrl are required." });
                }

                try
                {
                    var libraries = await plexService.GetLibraries(request.ServerUrl.Trim(), request.Token.Trim());
                    return Results.Json(libraries);
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            });

            application.MapGet("/api/logs", (long? sinceTicks) =>
            {
                IEnumerable<LocalServerLogEntry> entries;
                lock (logLock)
                {
                    entries = logBuffer
                        .Where(log => !sinceTicks.HasValue || log.Timestamp.Ticks > sinceTicks.Value)
                        .ToList();
                }

                return Results.Json(entries);
            });

            application.MapPost("/api/actions/generate-yaml", (GenerateYamlRequest request) =>
            {
                if (activeProfile == null)
                {
                    return Results.NotFound(new { error = "No active profile." });
                }

                var targetPath = ResolveYamlTargetPath(request?.TargetPath);
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                var yamlContent = yamlGenerator.GenerateKometaConfig(activeProfile);
                yamlGenerator.SaveConfigToFile(yamlContent, targetPath);

                RecordLog("Server", $"YAML configuration saved to {targetPath}");
                return Results.Ok(new { path = targetPath });
            });

            application.MapPost("/api/actions/run-kometa", async (RunKometaRequest request) =>
            {
                if (activeProfile == null)
                {
                    return Results.NotFound(new { error = "No active profile." });
                }

                var configPath = string.IsNullOrWhiteSpace(request?.ConfigPath)
                    ? ResolveYamlTargetPath(null)
                    : request.ConfigPath!;

                if (!File.Exists(configPath))
                {
                    var yamlContent = yamlGenerator.GenerateKometaConfig(activeProfile);
                    Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                    yamlGenerator.SaveConfigToFile(yamlContent, configPath);
                }

                var success = await kometaRunner.RunKometaAsync(activeProfile, configPath);
                return Results.Ok(new { success });
            });

            application.MapPost("/api/actions/stop-kometa", () =>
            {
                kometaRunner.StopKometa();
                RecordLog("Server", "Kometa execution stop requested.");
                return Results.Ok();
            });

            application.MapPost("/api/actions/create-schedule", (ScheduleRequest request) =>
            {
                if (activeProfile == null)
                {
                    return Results.NotFound(new { error = "No active profile." });
                }

                if (request == null || string.IsNullOrWhiteSpace(request.Time) || request.Interval < 1)
                {
                    return Results.BadRequest(new { error = "Invalid schedule payload." });
                }

                if (!Enum.TryParse<ScheduleFrequency>(request.Frequency, true, out var frequency))
                {
                    return Results.BadRequest(new { error = $"Unsupported frequency '{request.Frequency}'." });
                }

                var configPath = ResolveYamlTargetPath(null);
                var created = taskScheduler.CreateScheduledTask(activeProfile, configPath, frequency, request.Interval, request.Time);
                RecordLog("Server", $"Scheduled task {(created ? "created" : "failed to create")} for profile '{activeProfile.Name}'.");
                return Results.Ok(new { success = created });
            });

            application.MapPost("/api/actions/remove-schedule", () =>
            {
                if (activeProfile == null)
                {
                    return Results.NotFound(new { error = "No active profile." });
                }

                var removed = taskScheduler.DeleteScheduledTask(activeProfile.Name);
                RecordLog("Server", $"Scheduled task removal for '{activeProfile.Name}' {(removed ? "succeeded" : "failed") }.");
                return Results.Ok(new { success = removed });
            });

            application.MapGet("/api/actions/schedule-status", () =>
            {
                if (activeProfile == null)
                {
                    return Results.NotFound(new { error = "No active profile." });
                }

                var exists = taskScheduler.TaskExists(activeProfile.Name);
                return Results.Ok(new { exists });
            });

            application.MapGet("/api/actions/installation-status", async () =>
            {
                if (activeProfile == null)
                {
                    return Results.NotFound(new { error = "No active profile." });
                }

                var status = await kometaInstaller.CheckInstallationStatusAsync(activeProfile.KometaDirectory);
                return Results.Json(status);
            });

            application.MapPost("/api/actions/install-kometa", async (InstallRequest request) =>
            {
                if (activeProfile == null)
                {
                    return Results.NotFound(new { error = "No active profile." });
                }

                var success = await kometaInstaller.InstallKometaAsync(activeProfile.KometaDirectory, request?.Force ?? false);
                return Results.Ok(new { success });
            });

            application.MapPost("/api/actions/update-kometa", async () =>
            {
                if (activeProfile == null)
                {
                    return Results.NotFound(new { error = "No active profile." });
                }

                var success = await kometaInstaller.UpdateKometaAsync(activeProfile.KometaDirectory);
                return Results.Ok(new { success });
            });

            application.MapPost("/api/logs/append", (ManualLogRequest request) =>
            {
                if (request == null || string.IsNullOrWhiteSpace(request.Message))
                {
                    return Results.BadRequest(new { error = "Message is required." });
                }

                var source = string.IsNullOrWhiteSpace(request.Source) ? "Client" : request.Source;
                RecordLog(source, request.Message);
                return Results.Ok();
            });
        }

        private async Task ApplyProfileUpdateAsync(KometaProfile updatedProfile)
        {
            await profileLock.WaitAsync();
            try
            {
                if (activeProfile == null)
                {
                    throw new InvalidOperationException("No active profile available.");
                }

                var json = JsonConvert.SerializeObject(updatedProfile);
                JsonConvert.PopulateObject(json, activeProfile);
                activeProfile.LastModified = DateTime.Now;
                profileManager.SaveProfile(activeProfile);
            }
            finally
            {
                profileLock.Release();
            }

            ActiveProfileChanged?.Invoke(this, activeProfile!);
        }

        private async Task<KometaProfile> CloneProfileAsync()
        {
            await profileLock.WaitAsync();
            try
            {
                if (activeProfile == null)
                {
                    throw new InvalidOperationException("No active profile available.");
                }

                var json = JsonConvert.SerializeObject(activeProfile);
                return JsonConvert.DeserializeObject<KometaProfile>(json)!;
            }
            finally
            {
                profileLock.Release();
            }
        }

        private string ResolveYamlTargetPath(string? providedPath)
        {
            if (!string.IsNullOrWhiteSpace(providedPath))
            {
                return Path.GetFullPath(providedPath);
            }

            if (activeProfile == null)
            {
                throw new InvalidOperationException("No active profile available.");
            }

            var baseDirectory = activeProfile.KometaDirectory;
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Kometa");
            }

            var configDirectory = Path.Combine(baseDirectory, "config");
            var fileName = $"config_{SanitizeFileName(activeProfile.Name)}.yml";
            return Path.Combine(configDirectory, fileName);
        }

        private static string SanitizeFileName(string name)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }

            return name;
        }

        private void OnKometaRunnerLog(object? sender, string message) => RecordLog("Kometa", message);
        private void OnSystemRequirementsLog(object? sender, string message) => RecordLog("System", message);
        private void OnInstallerLog(object? sender, string message) => RecordLog("Installer", message);
        private void OnInstallerProgress(object? sender, int progress) => lastInstallerProgress = progress;

        private record CreateProfileRequest(string Name);
        private record SelectProfileRequest(string Name);
        private record ValidateTokenRequest(string Token);
        private record LibrariesRequest(string Token, string ServerUrl);
        private record GenerateYamlRequest(string? TargetPath);
        private record RunKometaRequest(string? ConfigPath);
        private record ScheduleRequest(int Interval, string Frequency, string Time);
        private record InstallRequest(bool Force);
        private record ManualLogRequest(string? Source, string Message);
    }

    public record LocalServerLogEntry(DateTime Timestamp, string Source, string Message);
}
