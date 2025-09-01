using System;
using System.Collections.Generic;
using System.Linq;

namespace KometaGUIv3.Shared.Models
{
    public class KometaProfile
    {
        public string Name { get; set; }
        public string KometaDirectory { get; set; }
        public PlexConfiguration Plex { get; set; }
        public TMDbConfiguration TMDb { get; set; }
        public List<string> SelectedLibraries { get; set; }
        public Dictionary<string, bool> SelectedCharts { get; set; }
        public Dictionary<string, OverlayConfiguration> OverlaySettings { get; set; }
        public Dictionary<string, string> OptionalServices { get; set; }
        public Dictionary<string, bool> EnabledServices { get; set; }
        public KometaSettings Settings { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }

        public KometaProfile()
        {
            Name = string.Empty;
            KometaDirectory = string.Empty;
            Plex = new PlexConfiguration();
            TMDb = new TMDbConfiguration();
            SelectedLibraries = new List<string>();
            SelectedCharts = new Dictionary<string, bool>();
            OverlaySettings = new Dictionary<string, OverlayConfiguration>();
            OptionalServices = new Dictionary<string, string>();
            EnabledServices = new Dictionary<string, bool>();
            Settings = new KometaSettings();
            CreatedDate = DateTime.Now;
            LastModified = DateTime.Now;
        }
    }

    public class PlexConfiguration
    {
        public string Url { get; set; }
        public string Token { get; set; }
        public string Email { get; set; }
        public bool IsAuthenticated { get; set; }
        public List<PlexLibrary> AvailableLibraries { get; set; }
        
        // Advanced Plex Settings
        public int Timeout { get; set; }
        public int DbCache { get; set; }
        public bool CleanBundles { get; set; }
        public bool EmptyTrash { get; set; }
        public bool Optimize { get; set; }
        public bool VerifySSL { get; set; }

        public PlexConfiguration()
        {
            Url = string.Empty;
            Token = string.Empty;
            Email = string.Empty;
            IsAuthenticated = false;
            AvailableLibraries = new List<PlexLibrary>();
            
            // Set defaults
            Timeout = 60;
            DbCache = 40;
            CleanBundles = false;
            EmptyTrash = false;
            Optimize = false;
            VerifySSL = true;
        }
    }

    public class PlexLibrary
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsSelected { get; set; }
    }

    public class PlexServer
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public string Address { get; set; } // Public/external address
        public string LocalAddresses { get; set; } // Local network addresses (comma-separated)
        public int Port { get; set; }
        public string MachineIdentifier { get; set; }
        public string Version { get; set; }
        
        public string GetUrl()
        {
            // Prefer local address if available
            var bestAddress = GetBestAddress();
            var bestPort = GetBestPort();
            return $"http://{bestAddress}:{bestPort}";
        }
        
        public int GetBestPort()
        {
            // If we're using a local address, use the standard Plex port (32400)
            // External ports can be different (like 25000) but local is always 32400
            if (GetBestAddress() != Address)
            {
                return 32400; // Standard Plex port for local connections
            }
            
            // If using public address, use the provided port
            return Port;
        }
        
        public string GetBestAddress()
        {
            // If we have local addresses, prefer those
            if (!string.IsNullOrWhiteSpace(LocalAddresses))
            {
                var localAddrs = LocalAddresses.Split(',');
                foreach (var addr in localAddrs)
                {
                    var cleanAddr = addr.Trim();
                    if (IsPrivateIP(cleanAddr))
                    {
                        return cleanAddr; // Return first valid local address
                    }
                }
            }
            
            // Fallback to public address
            return Address;
        }
        
        public bool IsLocal()
        {
            // Check if we have any local addresses available
            return HasLocalAddresses() || IsPrivateIP(Address);
        }
        
        public bool HasLocalAddresses()
        {
            if (string.IsNullOrWhiteSpace(LocalAddresses))
                return false;
                
            var localAddrs = LocalAddresses.Split(',');
            return localAddrs.Any(addr => IsPrivateIP(addr.Trim()));
        }
        
        private bool IsPrivateIP(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return false;
                
            // Check if IP is on local network (private IP ranges)
            return ipAddress.StartsWith("192.168.") || 
                   ipAddress.StartsWith("10.") || 
                   IsPrivate172Range(ipAddress) ||
                   ipAddress.Equals("127.0.0.1") ||
                   ipAddress.Equals("localhost");
        }
        
        private bool IsPrivate172Range(string ipAddress)
        {
            // 172.16.0.0 to 172.31.255.255 is the private range
            if (ipAddress.StartsWith("172."))
            {
                var parts = ipAddress.Split('.');
                if (parts.Length >= 2 && int.TryParse(parts[1], out int secondOctet))
                {
                    return secondOctet >= 16 && secondOctet <= 31;
                }
            }
            return false;
        }
    }

    public class TMDbConfiguration
    {
        public string ApiKey { get; set; }
        public bool IsAuthenticated { get; set; }
        
        // Advanced TMDb Settings
        public int CacheExpiration { get; set; }
        public string Language { get; set; }
        public string Region { get; set; }

        public TMDbConfiguration()
        {
            ApiKey = string.Empty;
            IsAuthenticated = false;
            
            // Set defaults
            CacheExpiration = 60;
            Language = "en";
            Region = string.Empty;
        }
    }

    public class OverlayConfiguration
    {
        public string OverlayType { get; set; }
        public string BuilderLevel { get; set; }
        public Dictionary<string, object> TemplateVariables { get; set; }
        public bool IsEnabled { get; set; } // Controls basic overlay inclusion in YAML
        public bool UseAdvancedVariables { get; set; } // Controls template_variables usage
        public RatingSettings RatingConfig { get; set; }

        public OverlayConfiguration()
        {
            OverlayType = string.Empty;
            BuilderLevel = "show";
            TemplateVariables = new Dictionary<string, object>();
            IsEnabled = false;
            UseAdvancedVariables = false;
            RatingConfig = new RatingSettings();
        }
    }

    public class RatingSettings
    {
        public bool EnableOverlay { get; set; }
        public RatingTypeConfig UserRating { get; set; }
        public RatingTypeConfig CriticRating { get; set; }
        public RatingTypeConfig AudienceRating { get; set; }
        public string HorizontalPosition { get; set; }

        public RatingSettings()
        {
            EnableOverlay = false;
            UserRating = new RatingTypeConfig { Source = "rt_tomato", DefaultFontSize = 70 };
            CriticRating = new RatingTypeConfig { Source = "imdb", DefaultFontSize = 70 };
            AudienceRating = new RatingTypeConfig { Source = "tmdb", DefaultFontSize = 70 };
            HorizontalPosition = "right";
        }
    }

    public class RatingTypeConfig
    {
        public bool IsEnabled { get; set; }
        public string Source { get; set; } // rt_tomato, imdb, tmdb
        public string CustomFont { get; set; }
        public int FontSize { get; set; }
        public int DefaultFontSize { get; set; }

        public RatingTypeConfig()
        {
            IsEnabled = false;
            Source = string.Empty;
            CustomFont = string.Empty;
            FontSize = 70;
            DefaultFontSize = 70;
        }
    }

    public class KometaSettings
    {
        // Core Settings
        public string SyncMode { get; set; } = "append";
        public int MinimumItems { get; set; } = 1;
        public bool DeleteBelowMinimum { get; set; } = true;
        public int RunAgainDelay { get; set; } = 2;

        // Cache Settings
        public bool Cache { get; set; } = true;
        public int CacheExpiration { get; set; } = 60;
        public bool VerifySSL { get; set; } = true;

        // Asset Settings
        public bool AssetFolders { get; set; } = true;
        public int AssetDepth { get; set; } = 0;
        public bool CreateAssetFolders { get; set; } = false;
        public bool PrioritizeAssets { get; set; } = false;
        public bool DimensionalAssetRename { get; set; } = false;
        public bool DownloadUrlAssets { get; set; } = false;
        public bool ShowAssetNotNeeded { get; set; } = true;

        // Display Settings
        public bool ShowUnmanaged { get; set; } = true;
        public bool ShowUnconfigured { get; set; } = true;
        public bool ShowMissing { get; set; } = true;
        public bool ShowMissingAssets { get; set; } = true;
        public bool ShowFiltered { get; set; } = false;
        public bool ShowOptions { get; set; } = true;
        public bool SaveReport { get; set; } = false;

        // Advanced Settings
        public bool DeleteNotScheduled { get; set; } = false;
        public bool MissingOnlyReleased { get; set; } = false;
        public bool OnlyFilterMissing { get; set; } = false;
        public int ItemRefreshDelay { get; set; } = 0;
        public bool PlaylistReport { get; set; } = false;
        public string OverlayArtworkFiletype { get; set; } = "webp_lossy";
        public int OverlayArtworkQuality { get; set; } = 90;

        // Additional settings for YAML generation
        public bool ShowMissingSeasonAssets { get; set; } = false;
        public bool ShowMissingEpisodeAssets { get; set; } = false;
        public bool ShowUnfiltered { get; set; } = false;
        public string DefaultCollectionOrder { get; set; } = "release";
        public string TvdbLanguage { get; set; } = "eng";
        public List<string> IgnoreIds { get; set; } = new List<string>();
        public List<string> IgnoreImdbIds { get; set; } = new List<string>();
        public List<string> PlaylistSyncToUsers { get; set; } = new List<string>();
        public List<string> PlaylistExcludeUsers { get; set; } = new List<string>();
        public string CustomRepo { get; set; } = "";
    }
}