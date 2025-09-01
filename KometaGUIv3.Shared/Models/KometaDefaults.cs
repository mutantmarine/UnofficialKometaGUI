using System.Collections.Generic;

namespace KometaGUIv3.Shared.Models
{
    public static class KometaDefaults
    {
        public static readonly Dictionary<string, List<DefaultCollection>> ChartCollections = new Dictionary<string, List<DefaultCollection>>
        {
            ["Charts"] = new List<DefaultCollection>
            {
                new DefaultCollection("basic", "Basic Charts", "Essential chart collections including trending and popular content"),
                new DefaultCollection("tmdb", "TMDb Charts", "The Movie Database trending, popular, and top-rated collections"),
                new DefaultCollection("imdb", "IMDb Charts", "IMDb Top 250, popular titles, and ratings-based collections"),
                new DefaultCollection("trakt", "Trakt Charts", "Trakt trending, popular, and most-watched collections"),
                new DefaultCollection("tautulli", "Tautulli Charts", "Local server statistics and most-played collections"),
                new DefaultCollection("letterboxd", "Letterboxd Charts", "Letterboxd popular films and curated lists"),
                new DefaultCollection("anilist", "AniList Charts", "Anime trending, popular, and top-rated series"),
                new DefaultCollection("myanimelist", "MyAnimeList Charts", "MAL top anime, popular seasonal releases"),
                new DefaultCollection("other_chart", "Other Charts", "Additional miscellaneous chart collections"),
                new DefaultCollection("separator_chart", "Chart Separators", "Visual separators for organizing chart collections")
            }
        };

        public static readonly Dictionary<string, List<DefaultCollection>> AwardCollections = new Dictionary<string, List<DefaultCollection>>
        {
            ["Awards"] = new List<DefaultCollection>
            {
                new DefaultCollection("oscars", "Academy Awards", "Oscar winners and nominees by year"),
                new DefaultCollection("golden", "Golden Globe Awards", "Golden Globe winners and nominees"),
                new DefaultCollection("bafta", "BAFTA Awards", "British Academy Film Awards"),
                new DefaultCollection("emmy", "Emmy Awards", "Television Emmy Awards"),
                new DefaultCollection("sag", "Screen Actors Guild Awards", "SAG Award winners and nominees"),
                new DefaultCollection("cannes", "Cannes Film Festival", "Palme d'Or and competition selections"),
                new DefaultCollection("venice", "Venice Film Festival", "Golden Lion and official selections"),
                new DefaultCollection("berlinale", "Berlin International Film Festival", "Golden Bear and competition films"),
                new DefaultCollection("sundance", "Sundance Film Festival", "Award winners and notable premieres"),
                new DefaultCollection("tiff", "Toronto International Film Festival", "People's Choice and selections"),
                new DefaultCollection("spirit", "Independent Spirit Awards", "Independent film awards"),
                new DefaultCollection("choice", "Choice Awards", "Teen Choice and People's Choice Awards"),
                new DefaultCollection("pca", "People's Choice Awards", "Popular voted awards"),
                new DefaultCollection("cesar", "César Awards", "French film industry awards"),
                new DefaultCollection("razzie", "Golden Raspberry Awards", "Worst film awards"),
                new DefaultCollection("nfr", "National Film Registry", "Culturally significant films"),
                new DefaultCollection("separator_award", "Award Separators", "Visual separators for award collections")
            }
        };

        public static readonly Dictionary<string, List<DefaultCollection>> MovieCollections = new Dictionary<string, List<DefaultCollection>>
        {
            ["Movies Only"] = new List<DefaultCollection>
            {
                new DefaultCollection("content_rating_us", "US Content Ratings", "G, PG, PG-13, R rated collections"),
                new DefaultCollection("continent", "By Continent", "Collections organized by continent of origin"),
                new DefaultCollection("country", "By Country", "Collections by country of production"),
                new DefaultCollection("decade", "By Decade", "Movies organized by release decade"),
                new DefaultCollection("director", "By Director", "Collections featuring prominent directors"),
                new DefaultCollection("franchise", "Movie Franchises", "Movie series and franchise collections"),
                new DefaultCollection("producer", "By Producer", "Collections by notable producers"),
                new DefaultCollection("region", "By Region", "Regional movie collections"),
                new DefaultCollection("seasonal", "Seasonal Movies", "Holiday and seasonal themed movies"),
                new DefaultCollection("writer", "By Writer", "Collections featuring notable screenwriters")
            }
        };

        public static readonly Dictionary<string, List<DefaultCollection>> ShowCollections = new Dictionary<string, List<DefaultCollection>>
        {
            ["TV Shows Only"] = new List<DefaultCollection>
            {
                new DefaultCollection("content_rating_us", "US TV Ratings", "TV-Y, TV-G, TV-PG, TV-14, TV-MA rated shows"),
                new DefaultCollection("continent", "By Continent", "TV shows organized by continent of origin"),
                new DefaultCollection("country", "By Country", "TV shows by country of production"),
                new DefaultCollection("decade", "By Decade", "TV shows organized by premiere decade"),
                new DefaultCollection("franchise", "TV Franchises", "Connected TV series and universes"),
                new DefaultCollection("network", "By Network", "Collections by broadcasting network"),
                new DefaultCollection("region", "By Region", "Regional TV show collections")
            }
        };

        public static readonly Dictionary<string, List<DefaultCollection>> BothCollections = new Dictionary<string, List<DefaultCollection>>
        {
            ["Movies & TV Shows"] = new List<DefaultCollection>
            {
                new DefaultCollection("actor", "By Actor", "Collections featuring specific actors"),
                new DefaultCollection("aspect", "By Aspect Ratio", "Widescreen, standard, and other aspect ratios"),
                new DefaultCollection("audio_language", "By Audio Language", "Collections by spoken language"),
                new DefaultCollection("based", "Based On...", "Adaptations from books, comics, true stories"),
                new DefaultCollection("collectionless", "Standalone Titles", "Movies/shows without series connections"),
                new DefaultCollection("genre", "By Genre", "Action, Comedy, Drama, Horror, etc."),
                new DefaultCollection("resolution", "By Resolution", "4K, 1080p, 720p quality collections"),
                new DefaultCollection("streaming", "Streaming Services", "Netflix, Disney+, HBO Max, etc."),
                new DefaultCollection("studio", "By Studio", "Production company collections"),
                new DefaultCollection("subtitle_language", "By Subtitle Language", "Collections by available subtitles"),
                new DefaultCollection("universe", "Shared Universes", "MCU, DC, Star Wars, etc."),
                new DefaultCollection("year", "By Year", "Collections organized by release year"),
                new DefaultCollection("content_rating_au", "Australian Ratings", "Australian classification system"),
                new DefaultCollection("content_rating_de", "German Ratings", "German FSK rating system"),
                new DefaultCollection("content_rating_uk", "UK Ratings", "British Board of Film Classification"),
                new DefaultCollection("content_rating_nz", "New Zealand Ratings", "New Zealand classification"),
                new DefaultCollection("content_rating_mal", "MAL Content Ratings", "MyAnimeList content ratings")
            }
        };

        public static readonly List<DefaultCollection> AllChartCollections = new List<DefaultCollection>();
        public static readonly List<DefaultCollection> AllAwardCollections = new List<DefaultCollection>();
        public static readonly List<DefaultCollection> AllMovieCollections = new List<DefaultCollection>();
        public static readonly List<DefaultCollection> AllShowCollections = new List<DefaultCollection>();
        public static readonly List<DefaultCollection> AllBothCollections = new List<DefaultCollection>();

        static KometaDefaults()
        {
            // Populate combined lists
            foreach (var category in ChartCollections.Values)
                AllChartCollections.AddRange(category);

            foreach (var category in AwardCollections.Values)
                AllAwardCollections.AddRange(category);

            foreach (var category in MovieCollections.Values)
                AllMovieCollections.AddRange(category);

            foreach (var category in ShowCollections.Values)
                AllShowCollections.AddRange(category);

            foreach (var category in BothCollections.Values)
                AllBothCollections.AddRange(category);
        }
    }

    public class DefaultCollection
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSelected { get; set; }
        public Dictionary<string, object> TemplateVariables { get; set; }

        public DefaultCollection(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
            IsSelected = false;
            TemplateVariables = new Dictionary<string, object>();
        }
    }
}