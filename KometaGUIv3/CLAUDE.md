# Unofficial Kometa GUI - Application Requirements

## Overview
A dark-themed Windows GUI application that provides all Kometa capabilities through a user-friendly interface. The app generates YAML configuration files, creates Windows scheduled tasks, and executes the Kometa Python program.

## Core Architecture
- Multiple C# files with different methods for maintainability
- Modular design to avoid one giant convoluted file
- Dark theme throughout the entire application

## Page Flow Structure

### 1. Welcome Page
- Brief overview of what the app does
- "Let's Go" button to proceed

### 2. Profile Management Page
- Create new profile
- Select existing profile  
- Delete existing profile
- "Next" button (only enabled after profile selection)

### 3. Connections Page (Required Services)
- **Directory Chooser:** Select Kometa installation directory (REQUIRED)
- **Plex Authentication:**
  - Email/password fields
  - Generates authenticated Plex token
  - Retrieves Plex libraries and server IP address
  - Library selection with checkboxes + Select/Unselect All
  - Editable IP address field (auto-populated)
- **TMDb API Key:**
  - API key input field
  - Link button to TMDb API key generation URL
- **Validation:** Cannot proceed until ALL are valid (directory + Plex auth + TMDb key)

### 4. Charts Page
- Tab-based interface for all available charts
- Charts sourced from: https://github.com/Kometa-Team/Kometa/tree/master/defaults

### 5. Overlays Page  
- Tab-based interface for all available overlay options
- Overlays sourced from: https://github.com/Kometa-Team/Kometa/tree/master/defaults/overlays

### 6. Optional Services Page
- **API Services** (with links to API key URLs):
  - Tautulli, GitHub, MDBList, Notifiarr, OMDb, Gotify, ntfy, AniDB, Trakt, MAL
- **Local Services** (auto-populated with Plex IP + respective ports, but editable):
  - Tautulli (port 8181)
  - Radarr (port 7878) 
  - Sonarr (port 8989)

### 7. Default Settings Page
- All default YAML configuration settings
- Pre-selected based on default template
- Toggleable checkboxes/true-false options
- User can modify as needed

### 8. Final Actions Page
- **Generate Config YAML** button
- **Run Kometa** button  
- **Schedule/Remove Scheduled Task** (with frequency options: days/weeks/months)
- **PayPal** button (links to paypal.com)
- **Log Area** - Shows Kometa execution output in real-time

## Default Configuration Template

Based on the provided YAML template, the app needs to handle:

### Required Services
- **Plex:** URL, token, timeout, db_cache, clean_bundles, empty_trash, optimize, verify_ssl
- **TMDb:** API key, cache_expiration, language, region

### Libraries Support
- Movies, TV Shows, Anime, Music
- Collection files and overlay files for each
- Remove_overlays option per library

### Optional Services Configuration
- **Tautulli:** URL, API key
- **GitHub:** Personal access token
- **OMDb:** API key, cache expiration
- **MDBList:** API key, cache expiration  
- **Notifiarr:** API key
- **Gotify:** URL, token
- **ntfy:** URL, token, topic
- **AniDB:** Username, password, client, language, version
- **Radarr:** URL, token, various add/monitor/upgrade settings, paths
- **Sonarr:** URL, token, various add/monitor/upgrade settings, paths
- **Trakt:** Client ID, client secret, pin, authorization section
- **MAL:** Client ID, client secret, cache expiration

### Settings Configuration
Extensive settings including:
- Run order, cache settings, asset directories
- Sync modes, collection orders, missing item handling
- Display options, SSL verification, custom repos
- Overlay artwork settings, webhook configurations

## Key Features
- Dark theme throughout
- Tab-based organization for complex sections
- Auto-population of local service IPs with standard ports
- All fields remain editable despite auto-population  
- Real-time validation and log viewing
- Profile-based configuration management
- Windows Task Scheduler integration

## Advanced Configuration Features

Based on the enhanced config file, the GUI needs to support:

### Per-Library Advanced Settings
- **Report Path:** Custom missing reports per library
- **Template Variables:** 
  - Separator styles (purple, plum, etc.)
  - Collection modes (hide, show, etc.)
  - Placeholder IMDb IDs for different libraries
  - Custom styling options (standards, compact, etc.)

### Advanced Collection Files
- **Award Collections:** BAFTA, Golden Globes, Oscars with date ranges
- **Chart Collections:** Basic, TMDb, Audio Language, Resolution, Studio
- **Seasonal Collections:** Christmas, Halloween with region-specific scheduling
- **Streaming Collections:** Disney+, Netflix with originals-only options
- **Universe Collections:** Marvel, Wizarding World, etc.
- **Network Collections:** ABC, CBC, NBC, FOX (TV Shows)
- **Template Variables per Collection:**
  - Date ranges (starting: latest-10, ending: latest)
  - Style options (standards, compact)
  - Schedule controls (never, always, seasonal)
  - Content filters (originals_only: true/false)

### Advanced Overlay System
- **Multi-Level Overlays:** Show, Season, Episode levels
- **Overlay Types with Positioning:**
  - **Resolution (Position 1, 4):** 4K HDR, 1080P FHD, etc. - Top positioning
  - **Audio Codec (Position 2):** FLAC, DTS-X, TrueHD, AAC - Top right area
  - **MediaStinger (Position 3):** Post-credit scene indicator - Top right
  - **Ratings (Positions 5, 6, 7):** Multi-rating system with custom fonts
    - **Rating 1 (Position 5):** User ratings (Rotten Tomatoes) with custom font support
    - **Rating 2 (Position 6):** Critic ratings (IMDb) with custom font support  
    - **Rating 3 (Position 7):** Audience ratings (TMDb) with custom font support
    - **Horizontal Position:** Left or Right placement
    - **Custom Fonts:** Local font file support with adjustable font sizes
  - **Streaming (Position 8):** Service indicators (Netflix, Amazon, etc.)
  - **Video Format (Position 9):** Remux, Blu-Ray, DVD - Bottom left
  - **Language Count (Position 10):** Multi-audio track indicator
  - **Ribbon (Positions 11, 12, 13):** Bottom right sash with priority weighting
  - **Episode Info:** S##E## information (TV Shows)
  - **Status:** Airing, Returning, Ended, Canceled (TV Shows)
  - **Versions:** Duplicate media indicators
- **Builder Levels:** episode, season, show with independent configurations

### Advanced Rating Operations
- **Mass Rating Updates:** Automated rating synchronization
  - **Movies/Shows:**
    - **mass_user_rating_update:** mdb_tomatoes (requires MDBList config)
    - **mass_critic_rating_update:** imdb (uses IMDb ratings)
    - **mass_audience_rating_update:** tmdb (uses TMDb ratings)
  - **Episodes (TV Shows):**
    - **mass_episode_critic_rating_update:** imdb (episode-specific IMDb ratings)
    - **mass_episode_audience_rating_update:** tmdb (episode-specific TMDb ratings)
- **Custom Rating Display:**
  - **Rating Images:** rt_tomato, imdb, tmdb logos
  - **Custom Fonts:** Local font file paths with size adjustment
  - **Font Sizes:** Adjustable per rating type (63, 70, etc.)
  - **Positioning:** Horizontal left/right placement control

### TV Show Specific Overlays
- **Show Level (Default):** Applied to show posters
  - **Resolution (1):** 1080P FHD, 4K, etc.
  - **Audio Codec (2):** DOLBY DIGITAL+, DD+ATMOS, etc.
  - **Ratings (4,5,6):** User/Critic/Audience with custom fonts
  - **Streaming (7):** Service indicators (Amazon WEB, Disney+, HBO Max, etc.)
  - **Video Format (8):** WEB, Blu-Ray, etc.
  - **Ribbon (10,11):** Bottom right sash elements

- **Season Level:** Applied to season posters
  - **Resolution (1):** Season-specific quality indicators
  - **Audio Codec (2):** Season-level audio formats
  - **Video Format (3):** Season-specific source formats
  - **Builder Level:** Explicitly set to "season"

- **Episode Level:** Applied to individual episodes
  - **Resolution (1):** Episode-specific quality
  - **Audio Codec (2):** Episode audio formats  
  - **Ratings (3,4):** Critic/Audience ratings per episode
  - **Video Format (5):** Episode source format
  - **Episode Info (6):** S##E## episode identifiers
  - **Runtimes (7):** Episode duration display
  - **Builder Level:** Explicitly set to "episode"

### Builder Level System
- **Default (Show):** No builder_level specified - applies to show posters
- **Season Level:** `builder_level: season` - applies to season posters  
- **Episode Level:** `builder_level: episode` - applies to individual episodes
- **Multi-Level Support:** Same overlay type can be applied at multiple levels simultaneously
- **Independent Configuration:** Each level can have different settings and positioning

### Per-Library Operations
- **Split Duplicates:** true/false toggle
- **Assets for All:** true/false toggle  
- **Remove Overlays:** true/false toggle
- **Reapply Overlays:** true/false toggle (with warnings about image bloat)
- **Reset Overlays:** Optional TMDb reset (with bloat warnings)

### Enhanced Playlist Support
- **Default Playlists** with library targeting
- **Cross-Library Playlists** (Movies, TV Shows combined)

## GUI Implementation Requirements

### Charts/Collections Page Updates
- **Award Collections Tab:** BAFTA, Golden Globes, Oscars with year range selectors
- **Charts Tab:** Basic, TMDb, Audio Language, Resolution, Studio options
- **Seasonal Tab:** Holiday collections with regional scheduling controls
- **Streaming Tab:** Service selection with originals-only toggles  
- **Universe/Network Tabs:** Franchise and network-based collections
- **Template Variables:** Dynamic options based on selected collections

### Overlays Page Updates
- **Visual Reference Images:** Display example overlay images for both Movies and TV Shows (including show/season/episode examples)
- **Media Type Selection:** Movies vs TV Shows with different overlay options
- **Positioned Overlay Map:** Interactive visual showing numbered overlay positions (varies by media type)
- **Builder Level Selection:** Show/Season/Episode radio buttons (TV Shows only)
  - **Show Level:** Default overlay application to show posters
  - **Season Level:** Apply overlays to season posters  
  - **Episode Level:** Apply overlays to individual episode thumbnails
- **Media-Specific Overlay Categories:**
  - **Movies:** Positions 1-13 (Resolution, Audio, MediaStinger, Ratings, Streaming, Video Format, Language Count, Ribbon)
  - **TV Shows - Show Level:** Positions 1,2,4-8,10-11 (Resolution, Audio, Ratings, Streaming, Video Format, Ribbon)
  - **TV Shows - Season Level:** Positions 1-3 (Resolution, Audio, Video Format)
  - **TV Shows - Episode Level:** Positions 1-7 (Resolution, Audio, Ratings, Video Format, Episode Info, Runtimes)
- **Advanced Rating Configuration:**
  - **Rating Source Selection:** User (RT), Critic (IMDb), Audience (TMDb)
  - **Rating Image Selection:** rt_tomato, imdb, tmdb logos
  - **Custom Font Support:** File browser for local font files (.ttf)
  - **Font Size Sliders:** Adjustable sizing (50-100 range)
  - **Position Controls:** Horizontal left/right radio buttons
  - **Episode-Specific Ratings:** Additional options for episode-level critic/audience ratings
- **TV Show Specific Features:**
  - **Episode Info Overlay:** S##E## episode identifiers
  - **Runtime Overlay:** Episode duration display
  - **Multi-Level Mass Rating Updates:** Show vs Episode rating operations
- **Multi-Instance Support:** Same overlay at different builder levels with independent configuration
- **Priority Weighting:** For overlays sharing positions (ribbon overlays)
- **Dynamic Position Mapping:** Overlay positions change based on media type and builder level

### Settings Page Updates
- **Per-Library Settings:** Report paths, asset directories, operations
- **Template Variables:** Separator styles, collection modes, placeholder IDs
- **Advanced Options:** Split duplicates, assets for all, overlay management

## Complete Kometa Defaults Catalog

### Chart Collections (defaults/chart/)
- **anilist.yml** - AniList anime tracking charts
- **basic.yml** - Basic/generic chart collections
- **imdb.yml** - IMDb movie/TV data charts  
- **letterboxd.yml** - Letterboxd movie tracking charts
- **myanimelist.yml** - MyAnimeList anime tracking charts
- **other_chart.yml** - Miscellaneous chart collections
- **separator_chart.yml** - Visual separator charts
- **tautulli.yml** - Tautulli media server tracking charts
- **tmdb.yml** - The Movie Database (TMDb) charts
- **trakt.yml** - Trakt media tracking platform charts

### Overlay Collections (defaults/overlays/)
**Media Characteristics:**
- aspect.yml, audio_codec.yml, resolution.yml, video_format.yml, languages.yml, language_count.yml

**Ratings & Content:**
- content_rating_au.yml, content_rating_de.yml, content_rating_nz.yml, content_rating_uk.yml
- content_rating_us_movie.yml, content_rating_us_show.yml, ratings.yml

**Media Information:**
- episode_info.yml, network.yml, studio.yml, streaming.yml, versions.yml

**Technical/Playback:**
- direct_play.yml, mediastinger.yml, runtimes.yml

**Miscellaneous:**
- commonsense.yml, ribbon.yml, status.yml, templates.yml

### Award Collections (defaults/award/)
- **bafta.yml** - BAFTA Awards
- **berlinale.yml** - Berlin International Film Festival
- **cannes.yml** - Cannes Film Festival
- **cesar.yml** - César Awards
- **choice.yml** - Choice Awards
- **emmy.yml** - Emmy Awards
- **golden.yml** - Golden Globe Awards
- **nfr.yml** - National Film Registry
- **oscars.yml** - Academy Awards
- **pca.yml** - People's Choice Awards
- **razzie.yml** - Golden Raspberry Awards
- **sag.yml** - Screen Actors Guild Awards
- **separator_award.yml** - Award separators
- **spirit.yml** - Independent Spirit Awards
- **sundance.yml** - Sundance Film Festival
- **tiff.yml** - Toronto International Film Festival
- **venice.yml** - Venice Film Festival

### Movie-Specific Collections (defaults/movie/)
- content_rating_us.yml, continent.yml, country.yml, decade.yml
- director.yml, franchise.yml, producer.yml, region.yml, seasonal.yml, writer.yml

### TV Show-Specific Collections (defaults/show/)
- content_rating_us.yml, continent.yml, country.yml, decade.yml
- franchise.yml, network.yml, region.yml

### Both Movies & TV Shows (defaults/both/)
- actor.yml, aspect.yml, audio_language.yml, based.yml, collectionless.yml
- content_rating (au, cs, de, mal, nz, uk variants), genre.yml, resolution.yml
- streaming.yml, studio.yml, subtitle_language.yml, universe.yml, year.yml


## Technical Requirements
- Windows Forms application
- YAML file generation and parsing
- HTTP client for API authentication
- Process management for running Kometa
- Windows Task Scheduler COM interface
- Secure credential storage per profile
- Dynamic UI generation based on selected options
- Template variable management system
- Multi-level overlay configuration support