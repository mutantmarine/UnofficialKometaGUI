using System.Collections.Generic;

namespace KometaGUIv3.Models
{
    public static class OverlayDefaults
    {
        public static readonly Dictionary<string, OverlayInfo> AllOverlays = new Dictionary<string, OverlayInfo>
        {
            // Media Characteristics
            ["resolution"] = new OverlayInfo("resolution", "Resolution", "4K HDR, 1080P FHD, etc.", 
                new[] { 1, 4 }, new[] { "show", "season", "episode" }, "Top positioning"),
            ["audio_codec"] = new OverlayInfo("audio_codec", "Audio Codec", "FLAC, DTS-X, TrueHD, AAC", 
                new[] { 2 }, new[] { "show", "season", "episode" }, "Top right area"),
            ["video_format"] = new OverlayInfo("video_format", "Video Format", "Remux, Blu-Ray, DVD", 
                new[] { 9, 8 }, new[] { "show", "season", "episode" }, "Bottom left / Center positioning"),
            ["aspect"] = new OverlayInfo("aspect", "Aspect Ratio", "Widescreen, standard ratios", 
                new[] { 1 }, new[] { "show" }, "Top positioning"),
            ["languages"] = new OverlayInfo("languages", "Languages", "Available audio languages", 
                new[] { 10 }, new[] { "show" }, "Multi-language indicator"),
            ["language_count"] = new OverlayInfo("language_count", "Language Count", "Multi-audio track indicator", 
                new[] { 10 }, new[] { "show" }, "Audio track count"),

            // Ratings & Content
            ["ratings"] = new OverlayInfo("ratings", "Ratings", "Multi-rating system with custom fonts", 
                new[] { 5, 6, 7, 4 }, new[] { "show", "episode" }, "Side area with custom positioning"),
            ["content_rating_us_movie"] = new OverlayInfo("content_rating_us_movie", "US Movie Ratings", "G, PG, PG-13, R ratings", 
                new[] { 8 }, new[] { "show" }, "Content rating indicator"),
            ["content_rating_us_show"] = new OverlayInfo("content_rating_us_show", "US TV Ratings", "TV-Y, TV-G, TV-PG, TV-14, TV-MA", 
                new[] { 8 }, new[] { "show" }, "TV content rating"),
            ["content_rating_au"] = new OverlayInfo("content_rating_au", "Australian Ratings", "Australian classification system", 
                new[] { 8 }, new[] { "show" }, "AU content rating"),
            ["content_rating_de"] = new OverlayInfo("content_rating_de", "German Ratings", "German FSK rating system", 
                new[] { 8 }, new[] { "show" }, "DE content rating"),
            ["content_rating_uk"] = new OverlayInfo("content_rating_uk", "UK Ratings", "BBFC classification", 
                new[] { 8 }, new[] { "show" }, "UK content rating"),
            ["content_rating_nz"] = new OverlayInfo("content_rating_nz", "New Zealand Ratings", "NZ classification", 
                new[] { 8 }, new[] { "show" }, "NZ content rating"),
            ["commonsense"] = new OverlayInfo("commonsense", "Common Sense Media", "Age-appropriate content ratings", 
                new[] { 8 }, new[] { "show" }, "CSM content rating"),

            // Media Information
            ["episode_info"] = new OverlayInfo("episode_info", "Episode Info", "S##E## episode identifiers", 
                new[] { 6 }, new[] { "episode" }, "Episode identification"),
            ["network"] = new OverlayInfo("network", "Network", "Broadcasting network indicators", 
                new[] { 8 }, new[] { "show" }, "TV network branding"),
            ["studio"] = new OverlayInfo("studio", "Studio", "Production studio indicators", 
                new[] { 8 }, new[] { "show" }, "Studio branding"),
            ["streaming"] = new OverlayInfo("streaming", "Streaming Services", "Netflix, Disney+, HBO Max, etc.", 
                new[] { 8, 7 }, new[] { "show" }, "Service indicators"),
            ["versions"] = new OverlayInfo("versions", "Versions", "Duplicate media indicators", 
                new[] { 11 }, new[] { "show", "episode" }, "Version indicators"),
            ["status"] = new OverlayInfo("status", "Status", "Airing, Returning, Ended, Canceled", 
                new[] { 11 }, new[] { "show" }, "Show status indicator"),

            // Technical/Playback
            ["direct_play"] = new OverlayInfo("direct_play", "Direct Play", "Direct playback capability", 
                new[] { 9 }, new[] { "show" }, "Playback method indicator"),
            ["mediastinger"] = new OverlayInfo("mediastinger", "MediaStinger", "Post-credit scene indicator", 
                new[] { 3 }, new[] { "show" }, "Post-credits indicator"),
            ["runtimes"] = new OverlayInfo("runtimes", "Runtimes", "Episode/movie duration display", 
                new[] { 7 }, new[] { "episode" }, "Runtime information"),

            // Visual Elements
            ["ribbon"] = new OverlayInfo("ribbon", "Ribbon", "Bottom right sash with priority weighting", 
                new[] { 11, 12, 13 }, new[] { "show" }, "Decorative ribbon element")
        };

        public static readonly Dictionary<string, string[]> MediaTypeOverlays = new Dictionary<string, string[]>
        {
            ["Movies"] = new[] { 
                "resolution", "audio_codec", "mediastinger", "ratings", "streaming", 
                "video_format", "language_count", "ribbon", "content_rating_us_movie",
                "content_rating_au", "content_rating_de", "content_rating_uk", "content_rating_nz"
            },
            ["TV Shows"] = new[] { 
                "resolution", "audio_codec", "aspect", "ratings", "streaming", "video_format",
                "languages", "language_count", "ribbon", "episode_info", "runtimes", 
                "network", "studio", "status", "versions", "direct_play", "mediastinger",
                "content_rating_us_show", "content_rating_au", "content_rating_de", 
                "content_rating_uk", "content_rating_nz", "commonsense"
            }
        };

        public static readonly Dictionary<int, string> PositionDescriptions = new Dictionary<int, string>
        {
            [1] = "Top Left - Resolution/Quality",
            [2] = "Top Center-Left - Audio Codec", 
            [3] = "Top Center-Right - MediaStinger",
            [4] = "Top Right - Additional Resolution",
            [5] = "Middle Right - User Rating",
            [6] = "Middle Right - Critic Rating", 
            [7] = "Middle Right - Audience Rating/Runtime",
            [8] = "Center - Streaming/Network/Studio",
            [9] = "Bottom Left - Video Format",
            [10] = "Bottom Center - Language Count",
            [11] = "Bottom Right - Primary Ribbon",
            [12] = "Bottom Right - Secondary Ribbon",
            [13] = "Bottom Right - Tertiary Ribbon"
        };

        public static readonly string[] BuilderLevels = { "show", "season", "episode" };

        public static readonly Dictionary<string, RatingConfiguration> DefaultRatingConfigs = new Dictionary<string, RatingConfiguration>
        {
            ["user"] = new RatingConfiguration 
            { 
                Source = "user", 
                Image = "rt_tomato", 
                FontSize = 63,
                MassUpdateOperation = "mass_user_rating_update: mdb_tomatoes"
            },
            ["critic"] = new RatingConfiguration 
            { 
                Source = "critic", 
                Image = "imdb", 
                FontSize = 70,
                MassUpdateOperation = "mass_critic_rating_update: imdb"
            },
            ["audience"] = new RatingConfiguration 
            { 
                Source = "audience", 
                Image = "tmdb", 
                FontSize = 70,
                MassUpdateOperation = "mass_audience_rating_update: tmdb"
            }
        };
    }

    public class OverlayInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int[] Positions { get; set; }
        public string[] SupportedLevels { get; set; }
        public string PositionDescription { get; set; }
        public bool IsEnabled { get; set; }
        public string BuilderLevel { get; set; }
        public Dictionary<string, object> TemplateVariables { get; set; }

        public OverlayInfo(string id, string name, string description, int[] positions, string[] supportedLevels, string positionDescription)
        {
            Id = id;
            Name = name;
            Description = description;
            Positions = positions;
            SupportedLevels = supportedLevels;
            PositionDescription = positionDescription;
            IsEnabled = false;
            BuilderLevel = "show";
            TemplateVariables = new Dictionary<string, object>();
        }
    }

    public class RatingConfiguration
    {
        public string Source { get; set; }
        public string Image { get; set; }
        public string CustomFont { get; set; }
        public int FontSize { get; set; }
        public string HorizontalPosition { get; set; } = "right";
        public string MassUpdateOperation { get; set; }
    }
}