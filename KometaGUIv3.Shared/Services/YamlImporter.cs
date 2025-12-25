using System;
using System.Collections.Generic;
using System.Linq;
using KometaGUIv3.Shared.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace KometaGUIv3.Shared.Services
{
    public class YamlImporter
    {
        private readonly IDeserializer deserializer;

        public YamlImporter()
        {
            deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        public ImportResult ParseConfigYaml(string yamlContent)
        {
            var result = new ImportResult
            {
                Success = false,
                Profile = new KometaProfile(),
                Warnings = new List<ImportWarning>()
            };

            try
            {
                // Deserialize YAML to dynamic object
                var yaml = deserializer.Deserialize<dynamic>(yamlContent);

                if (yaml == null)
                {
                    result.ErrorMessage = "Failed to parse YAML: Empty or invalid content.";
                    return result;
                }

                // Validate required sections
                var validationWarnings = ValidateConfig(yaml);
                result.Warnings.AddRange(validationWarnings);

                // Check for critical errors
                if (result.Warnings.Any(w => w.Severity == "error"))
                {
                    result.ErrorMessage = "Configuration contains critical errors. Please fix them before importing.";
                    return result;
                }

                // Parse all sections
                result.Profile.Plex = ParsePlexSection(yaml);
                result.Profile.TMDb = ParseTMDbSection(yaml);
                result.Profile.SelectedLibraries = ParseLibraries(yaml);
                result.Profile.Settings = ParseSettings(yaml);

                // Parse collections and overlays from libraries
                var libraryConfigs = ParseLibraryConfigurations(yaml);
                result.Profile.SelectedCharts = libraryConfigs.Item1;
                result.Profile.OverlaySettings = libraryConfigs.Item2;

                // Parse optional services
                var serviceConfigs = ParseOptionalServices(yaml);
                result.Profile.OptionalServices = serviceConfigs.Item1;
                result.Profile.EnabledServices = serviceConfigs.Item2;

                // Generate preview
                result.Preview = GeneratePreview(result.Profile, result.Warnings);

                result.Success = true;
            }
            catch (YamlDotNet.Core.YamlException ex)
            {
                result.ErrorMessage = $"YAML parsing error: {ex.Message}";
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Unexpected error: {ex.Message}";
            }

            return result;
        }

        private PlexConfiguration ParsePlexSection(dynamic yaml)
        {
            var config = new PlexConfiguration();

            if (yaml == null || !HasProperty(yaml, "plex"))
            {
                return config;
            }

            var plex = yaml["plex"];

            config.Url = GetStringValue(plex, "url") ?? "";
            config.Token = GetStringValue(plex, "token") ?? "";
            config.Timeout = GetIntValue(plex, "timeout", 60);
            config.DbCache = GetIntValue(plex, "db_cache", 40);
            config.CleanBundles = GetBoolValue(plex, "clean_bundles", false);
            config.EmptyTrash = GetBoolValue(plex, "empty_trash", false);
            config.Optimize = GetBoolValue(plex, "optimize", false);
            config.VerifySSL = GetBoolValue(plex, "verify_ssl", true);
            config.IsAuthenticated = !string.IsNullOrEmpty(config.Url) && !string.IsNullOrEmpty(config.Token);

            return config;
        }

        private TMDbConfiguration ParseTMDbSection(dynamic yaml)
        {
            var config = new TMDbConfiguration();

            if (yaml == null || !HasProperty(yaml, "tmdb"))
            {
                return config;
            }

            var tmdb = yaml["tmdb"];

            config.ApiKey = GetStringValue(tmdb, "apikey") ?? "";
            config.CacheExpiration = GetIntValue(tmdb, "cache_expiration", 60);
            config.Language = GetStringValue(tmdb, "language") ?? "en";
            config.Region = GetStringValue(tmdb, "region") ?? "";
            config.IsAuthenticated = !string.IsNullOrEmpty(config.ApiKey);

            return config;
        }

        private List<string> ParseLibraries(dynamic yaml)
        {
            var libraries = new List<string>();

            if (yaml == null || !HasProperty(yaml, "libraries"))
            {
                return libraries;
            }

            var librariesSection = yaml["libraries"];

            if (librariesSection is IDictionary<object, object> librariesDict)
            {
                foreach (var lib in librariesDict)
                {
                    libraries.Add(lib.Key.ToString());
                }
            }

            return libraries;
        }

        private (Dictionary<string, bool>, Dictionary<string, OverlayConfiguration>) ParseLibraryConfigurations(dynamic yaml)
        {
            var collections = new Dictionary<string, bool>();
            var overlays = new Dictionary<string, OverlayConfiguration>();

            if (yaml == null || !HasProperty(yaml, "libraries"))
            {
                return (collections, overlays);
            }

            var librariesSection = yaml["libraries"];

            if (librariesSection is IDictionary<object, object> librariesDict)
            {
                foreach (var lib in librariesDict)
                {
                    var libraryName = lib.Key?.ToString() ?? "";
                    dynamic libraryConfig = lib.Value;

                    // Determine library type (default to "Both" if unknown)
                    // In a real scenario, we'd need to query Plex to get this info
                    // For import purposes, we'll try to infer from context or default to "Both"
                    string libraryType = "Both"; // This will be refined in a real implementation

                    // Parse collection files
                    if (HasProperty(libraryConfig, "collection_files"))
                    {
                        dynamic collectionFiles = libraryConfig["collection_files"];
                        if (collectionFiles is IEnumerable<object> collectionList)
                        {
                            foreach (var item in collectionList)
                            {
                                if (item is IDictionary<object, object> itemDict && itemDict.ContainsKey("default"))
                                {
                                    var collectionId = itemDict["default"].ToString();

                                    // Add with appropriate prefix - for now we'll add both movie_ and show_ variants
                                    // since we don't know the library type for certain
                                    collections[$"movie_{collectionId}"] = true;
                                    collections[$"show_{collectionId}"] = true;
                                }
                            }
                        }
                    }

                    // Parse overlay files
                    if (HasProperty(libraryConfig, "overlay_files"))
                    {
                        dynamic overlayFiles = libraryConfig["overlay_files"];
                        if (overlayFiles is IEnumerable<object> overlayList)
                        {
                            foreach (var item in overlayList)
                            {
                                if (item is IDictionary<object, object> itemDict && itemDict.ContainsKey("default"))
                                {
                                    var overlayType = itemDict["default"].ToString();
                                    var builderLevel = "show"; // Default
                                    var templateVars = new Dictionary<string, object>();

                                    // Parse template_variables if present
                                    if (itemDict.ContainsKey("template_variables"))
                                    {
                                        dynamic vars = itemDict["template_variables"];
                                        if (vars is IDictionary<object, object> varsDict)
                                        {
                                            if (varsDict.ContainsKey("builder_level"))
                                            {
                                                builderLevel = varsDict["builder_level"].ToString();
                                            }

                                            // Store all template variables
                                            foreach (var v in varsDict)
                                            {
                                                templateVars[v.Key.ToString()] = v.Value;
                                            }
                                        }
                                    }

                                    // Create overlay configuration
                                    // Key format: overlayType_MediaType (e.g., "resolution_Movies")
                                    // Since we don't know for certain, we'll create for both
                                    foreach (var mediaType in new[] { "Movies", "TV Shows" })
                                    {
                                        var key = $"{overlayType}_{mediaType}";

                                        if (!overlays.ContainsKey(key))
                                        {
                                            overlays[key] = new OverlayConfiguration
                                            {
                                                OverlayType = overlayType,
                                                BuilderLevel = builderLevel,
                                                IsEnabled = true,
                                                UseAdvancedVariables = templateVars.Count > 0,
                                                TemplateVariables = templateVars
                                            };
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Parse operations section (mass rating updates)
                    if (HasProperty(libraryConfig, "operations"))
                    {
                        var operations = libraryConfig["operations"];

                        // Note: Rating operations will be linked to rating overlays
                        // This is handled separately as it requires matching with overlay configs
                    }
                }
            }

            return (collections, overlays);
        }

        private KometaSettings ParseSettings(dynamic yaml)
        {
            var settings = new KometaSettings();

            if (yaml == null || !HasProperty(yaml, "settings"))
            {
                return settings;
            }

            var settingsSection = yaml["settings"];

            // Parse all settings with defaults
            settings.Cache = GetBoolValue(settingsSection, "cache", true);
            settings.CacheExpiration = GetIntValue(settingsSection, "cache_expiration", 60);
            settings.SyncMode = GetStringValue(settingsSection, "sync_mode") ?? "append";
            settings.MinimumItems = GetIntValue(settingsSection, "minimum_items", 1);
            settings.DeleteBelowMinimum = GetBoolValue(settingsSection, "delete_below_minimum", true);
            settings.RunAgainDelay = GetIntValue(settingsSection, "run_again_delay", 2);
            settings.DeleteNotScheduled = GetBoolValue(settingsSection, "delete_not_scheduled", false);
            settings.MissingOnlyReleased = GetBoolValue(settingsSection, "missing_only_released", false);
            settings.OnlyFilterMissing = GetBoolValue(settingsSection, "only_filter_missing", false);
            settings.ShowUnmanaged = GetBoolValue(settingsSection, "show_unmanaged", true);
            settings.ShowUnconfigured = GetBoolValue(settingsSection, "show_unconfigured", true);
            settings.ShowMissing = GetBoolValue(settingsSection, "show_missing", true);
            settings.ShowMissingAssets = GetBoolValue(settingsSection, "show_missing_assets", true);
            settings.ShowMissingSeasonAssets = GetBoolValue(settingsSection, "show_missing_season_assets", false);
            settings.ShowMissingEpisodeAssets = GetBoolValue(settingsSection, "show_missing_episode_assets", false);
            settings.ShowFiltered = GetBoolValue(settingsSection, "show_filtered", false);
            settings.ShowUnfiltered = GetBoolValue(settingsSection, "show_unfiltered", false);
            settings.ShowOptions = GetBoolValue(settingsSection, "show_options", true);
            settings.ShowAssetNotNeeded = GetBoolValue(settingsSection, "show_asset_not_needed", true);
            settings.SaveReport = GetBoolValue(settingsSection, "save_report", false);
            settings.AssetFolders = GetBoolValue(settingsSection, "asset_folders", true);
            settings.AssetDepth = GetIntValue(settingsSection, "asset_depth", 0);
            settings.CreateAssetFolders = GetBoolValue(settingsSection, "create_asset_folders", false);
            settings.PrioritizeAssets = GetBoolValue(settingsSection, "prioritize_assets", false);
            settings.DimensionalAssetRename = GetBoolValue(settingsSection, "dimensional_asset_rename", false);
            settings.DownloadUrlAssets = GetBoolValue(settingsSection, "download_url_assets", false);
            settings.ItemRefreshDelay = GetIntValue(settingsSection, "item_refresh_delay", 0);
            settings.PlaylistReport = GetBoolValue(settingsSection, "playlist_report", false);
            settings.VerifySSL = GetBoolValue(settingsSection, "verify_ssl", true);
            settings.DefaultCollectionOrder = GetStringValue(settingsSection, "default_collection_order") ?? "release";
            settings.TvdbLanguage = GetStringValue(settingsSection, "tvdb_language") ?? "eng";
            settings.OverlayArtworkFiletype = GetStringValue(settingsSection, "overlay_artwork_filetype") ?? "webp_lossy";
            settings.OverlayArtworkQuality = GetIntValue(settingsSection, "overlay_artwork_quality", 90);
            settings.CustomRepo = GetStringValue(settingsSection, "custom_repo") ?? "";

            return settings;
        }

        private (Dictionary<string, string>, Dictionary<string, bool>) ParseOptionalServices(dynamic yaml)
        {
            var services = new Dictionary<string, string>();
            var enabledServices = new Dictionary<string, bool>();

            if (yaml == null)
            {
                return (services, enabledServices);
            }

            // Tautulli
            if (HasProperty(yaml, "tautulli"))
            {
                var tautulli = yaml["tautulli"];
                services["tautulli_url"] = GetStringValue(tautulli, "url") ?? "";
                services["tautulli_key"] = GetStringValue(tautulli, "apikey") ?? "";
                enabledServices["tautulli"] = true;
            }

            // Radarr
            if (HasProperty(yaml, "radarr"))
            {
                var radarr = yaml["radarr"];
                services["radarr_url"] = GetStringValue(radarr, "url") ?? "";
                services["radarr_key"] = GetStringValue(radarr, "token") ?? "";
                enabledServices["radarr"] = true;
            }

            // Sonarr
            if (HasProperty(yaml, "sonarr"))
            {
                var sonarr = yaml["sonarr"];
                services["sonarr_url"] = GetStringValue(sonarr, "url") ?? "";
                services["sonarr_key"] = GetStringValue(sonarr, "token") ?? "";
                enabledServices["sonarr"] = true;
            }

            // GitHub
            if (HasProperty(yaml, "github"))
            {
                var github = yaml["github"];
                services["github_key"] = GetStringValue(github, "token") ?? "";
                enabledServices["github"] = true;
            }

            // OMDb
            if (HasProperty(yaml, "omdb"))
            {
                var omdb = yaml["omdb"];
                services["omdb_key"] = GetStringValue(omdb, "apikey") ?? "";
                enabledServices["omdb"] = true;
            }

            // MDBList
            if (HasProperty(yaml, "mdblist"))
            {
                var mdblist = yaml["mdblist"];
                services["mdblist_key"] = GetStringValue(mdblist, "apikey") ?? "";
                enabledServices["mdblist"] = true;
            }

            // Notifiarr
            if (HasProperty(yaml, "notifiarr"))
            {
                var notifiarr = yaml["notifiarr"];
                services["notifiarr_key"] = GetStringValue(notifiarr, "apikey") ?? "";
                enabledServices["notifiarr"] = true;
            }

            // Gotify
            if (HasProperty(yaml, "gotify"))
            {
                var gotify = yaml["gotify"];
                services["gotify_url"] = GetStringValue(gotify, "url") ?? "";
                services["gotify_key"] = GetStringValue(gotify, "token") ?? "";
                enabledServices["gotify"] = true;
            }

            // ntfy
            if (HasProperty(yaml, "ntfy"))
            {
                var ntfy = yaml["ntfy"];
                services["ntfy_url"] = GetStringValue(ntfy, "url") ?? "";
                services["ntfy_key"] = GetStringValue(ntfy, "token") ?? "";
                enabledServices["ntfy"] = true;
            }

            // AniDB
            if (HasProperty(yaml, "anidb"))
            {
                var anidb = yaml["anidb"];
                services["anidb_key"] = GetStringValue(anidb, "password") ?? "";
                enabledServices["anidb"] = true;
            }

            // Trakt
            if (HasProperty(yaml, "trakt"))
            {
                var trakt = yaml["trakt"];
                services["trakt_client_id"] = GetStringValue(trakt, "client_id") ?? "";
                services["trakt_client_secret"] = GetStringValue(trakt, "client_secret") ?? "";
                services["trakt_pin"] = GetStringValue(trakt, "pin") ?? "";
                enabledServices["trakt"] = true;
            }

            // MAL
            if (HasProperty(yaml, "mal"))
            {
                var mal = yaml["mal"];
                services["mal_client_id"] = GetStringValue(mal, "client_id") ?? "";
                services["mal_client_secret"] = GetStringValue(mal, "client_secret") ?? "";
                services["mal_cache_expiration"] = GetStringValue(mal, "cache_expiration") ?? "60";
                services["mal_localhost_url"] = GetStringValue(mal, "localhost_url") ?? "";
                enabledServices["mal"] = true;
            }

            return (services, enabledServices);
        }

        private List<ImportWarning> ValidateConfig(dynamic yaml)
        {
            var warnings = new List<ImportWarning>();

            // Check for required Plex section
            if (!HasProperty(yaml, "plex"))
            {
                warnings.Add(new ImportWarning
                {
                    Section = "Plex",
                    Message = "Plex configuration is missing. This is required.",
                    Severity = "error"
                });
            }
            else
            {
                var plex = yaml["plex"];
                if (string.IsNullOrEmpty(GetStringValue(plex, "url")))
                {
                    warnings.Add(new ImportWarning
                    {
                        Section = "Plex",
                        Message = "Plex URL is missing.",
                        Severity = "error"
                    });
                }
                if (string.IsNullOrEmpty(GetStringValue(plex, "token")))
                {
                    warnings.Add(new ImportWarning
                    {
                        Section = "Plex",
                        Message = "Plex token is missing.",
                        Severity = "error"
                    });
                }
            }

            // Check for required TMDb section
            if (!HasProperty(yaml, "tmdb"))
            {
                warnings.Add(new ImportWarning
                {
                    Section = "TMDb",
                    Message = "TMDb configuration is missing. This is required.",
                    Severity = "error"
                });
            }
            else
            {
                var tmdb = yaml["tmdb"];
                if (string.IsNullOrEmpty(GetStringValue(tmdb, "apikey")))
                {
                    warnings.Add(new ImportWarning
                    {
                        Section = "TMDb",
                        Message = "TMDb API key is missing.",
                        Severity = "error"
                    });
                }
            }

            // Check for libraries section
            if (!HasProperty(yaml, "libraries"))
            {
                warnings.Add(new ImportWarning
                {
                    Section = "Libraries",
                    Message = "No libraries section found. No collections or overlays will be imported.",
                    Severity = "warning"
                });
            }

            // Info about settings
            if (!HasProperty(yaml, "settings"))
            {
                warnings.Add(new ImportWarning
                {
                    Section = "Settings",
                    Message = "No settings section found. Default settings will be used.",
                    Severity = "info"
                });
            }

            return warnings;
        }

        private ImportPreview GeneratePreview(KometaProfile profile, List<ImportWarning> warnings)
        {
            var preview = new ImportPreview
            {
                PlexUrl = profile.Plex?.Url ?? "Not configured",
                HasTMDbKey = !string.IsNullOrEmpty(profile.TMDb?.ApiKey),
                LibraryCount = profile.SelectedLibraries?.Count ?? 0,
                LibraryNames = profile.SelectedLibraries ?? new List<string>(),
                CollectionCount = profile.SelectedCharts?.Count(c => c.Value) ?? 0,
                CollectionTypes = profile.SelectedCharts?
                    .Where(c => c.Value)
                    .Select(c => c.Key)
                    .Distinct()
                    .ToList() ?? new List<string>(),
                OverlayCount = profile.OverlaySettings?.Count ?? 0,
                OverlayTypes = profile.OverlaySettings?
                    .Select(o => o.Value.OverlayType)
                    .Distinct()
                    .ToList() ?? new List<string>(),
                EnabledServices = profile.EnabledServices?
                    .Where(s => s.Value)
                    .Select(s => s.Key)
                    .ToList() ?? new List<string>(),
                OverlaysByBuilderLevel = new Dictionary<string, int>()
            };

            // Count overlays by builder level
            if (profile.OverlaySettings != null)
            {
                foreach (var overlay in profile.OverlaySettings)
                {
                    var level = overlay.Value.BuilderLevel ?? "show";
                    if (!preview.OverlaysByBuilderLevel.ContainsKey(level))
                    {
                        preview.OverlaysByBuilderLevel[level] = 0;
                    }
                    preview.OverlaysByBuilderLevel[level]++;
                }
            }

            return preview;
        }

        // Helper methods
        private bool HasProperty(dynamic obj, string propertyName)
        {
            if (obj == null)
                return false;

            if (obj is IDictionary<object, object> dict)
                return dict.ContainsKey(propertyName);

            return false;
        }

        private string GetStringValue(dynamic obj, string key)
        {
            if (obj == null || !HasProperty(obj, key))
                return null;

            var value = obj[key];
            return value?.ToString();
        }

        private int GetIntValue(dynamic obj, string key, int defaultValue)
        {
            var stringValue = GetStringValue(obj, key);
            if (int.TryParse(stringValue, out int result))
                return result;
            return defaultValue;
        }

        private bool GetBoolValue(dynamic obj, string key, bool defaultValue)
        {
            var stringValue = GetStringValue(obj, key);
            if (bool.TryParse(stringValue, out bool result))
                return result;
            return defaultValue;
        }
    }

    // Data Models
    public class ImportResult
    {
        public bool Success { get; set; }
        public KometaProfile Profile { get; set; } = null!;
        public ImportPreview Preview { get; set; } = null!;
        public List<ImportWarning> Warnings { get; set; } = null!;
        public string? ErrorMessage { get; set; }
    }

    public class ImportPreview
    {
        public string PlexUrl { get; set; } = null!;
        public bool HasTMDbKey { get; set; }
        public int LibraryCount { get; set; }
        public List<string> LibraryNames { get; set; } = null!;
        public int CollectionCount { get; set; }
        public List<string> CollectionTypes { get; set; } = null!;
        public int OverlayCount { get; set; }
        public List<string> OverlayTypes { get; set; } = null!;
        public List<string> EnabledServices { get; set; } = null!;
        public Dictionary<string, int> OverlaysByBuilderLevel { get; set; } = null!;
    }

    public class ImportWarning
    {
        public string Section { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Severity { get; set; } = null!; // "error", "warning", "info"
    }
}
