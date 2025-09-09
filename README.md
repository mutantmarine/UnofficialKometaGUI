# Unofficial Kometa GUI

A user-friendly Windows interface for managing [Kometa](https://github.com/Kometa-Team/Kometa) media library configurations.

## 🚀 Features

- **Profile-Based Management**: Create and manage multiple Kometa configurations
- **Easy Setup Wizard**: Step-by-step configuration for Plex, TMDb, and other services
- **Visual Overlay Designer**: Interactive overlay positioning with 77+ default collections
- **Comprehensive Collections**: Charts, awards, seasonal, and streaming service collections
- **Service Integrations**: Support for Tautulli, Radarr, Sonarr, and optional APIs
- **YAML Generation**: Automatic configuration file creation
- **Task Scheduling**: Built-in Windows Task Scheduler integration
- **Dark Theme Interface**: Modern, easy-on-the-eyes design

## 📋 Requirements

- **Windows 7 or later** (x64)
- **No additional software required** - .NET runtime included

## 📦 Installation

1. Download the latest release: `UnofficialKometaGUI-v1.0.0-win-x64.zip`
2. Extract the ZIP file to any folder (e.g., `C:\UnofficialKometaGUI\`)
3. Run `KometaGUIv3.exe`
4. Follow the welcome wizard to set up your first profile

## 🔧 Setup Guide

### Step 1: Profile Creation
- Create a new profile or select an existing one
- Each profile maintains separate configurations

### Step 2: Required Services
- **Kometa Directory**: Point to your Kometa installation folder
- **Plex Authentication**: Enter credentials to generate access token
- **TMDb API Key**: Add your The Movie Database API key

### Step 3: Collections & Overlays
- Choose from 77+ default collections (charts, awards, genres, etc.)
- Configure overlay positioning for movies and TV shows
- Enable advanced variables for mass rating operations

### Step 4: Optional Services
- Configure Tautulli, Radarr, Sonarr connections
- Add API keys for additional services (MDBList, Trakt, etc.)

### Step 5: Generate & Run
- Generate YAML configuration files
- Run Kometa directly from the interface
- Schedule automated runs with Windows Task Scheduler

## 📚 Key Components

### Collections Support
- **Charts**: Basic, TMDb, IMDb, Letterboxd, Tautulli
- **Awards**: Oscars, Golden Globes, BAFTA, Emmy, and more
- **Seasonal**: Christmas, Halloween with regional scheduling
- **Streaming**: Netflix, Disney+, Amazon Prime, HBO Max
- **Genres**: All standard movie and TV show genres
- **Networks**: ABC, NBC, CBS, FOX (TV Shows)

### Overlay System
- **Movies**: 13 positioned overlay types (resolution, audio, ratings, etc.)
- **TV Shows**: Multi-level overlays (show/season/episode)
- **Ratings**: User, critic, and audience ratings with custom fonts
- **Advanced Variables**: Mass rating operations for large libraries

### Task Scheduling
- **Flexible Scheduling**: Daily, weekly, or monthly execution
- **Custom Times**: Set specific execution times (HHMM format)
- **Background Execution**: Silent operation via Windows Task Scheduler

## 🔗 Integration

### Required APIs
- **Plex Media Server**: Direct integration with authentication
- **TMDb**: The Movie Database for metadata

### Optional APIs
- **Tautulli**: Media server statistics
- **MDBList**: Additional rating sources
- **Trakt**: Media tracking and recommendations
- **Radarr/Sonarr**: Media management automation

## 📁 Data Storage

User data is stored locally in:
```
%AppData%\UnofficialKometaGUI\
├── app-settings.json       # Application settings
└── Profiles\               # User profiles
    └── [ProfileName].json  # Individual profile configurations
```

## 🛠️ Troubleshooting

### Common Issues

**Task Scheduler Permission Errors**
- Run the application as Administrator when creating scheduled tasks

**Plex Authentication Fails**
- Verify Plex credentials and server accessibility
- Check that Plex Media Server is running
- Verify you have Two Factor Authentication disabled on your Plex server

**Kometa Directory Not Found**
- Ensure you point to the folder containing `kometa.py`
- Verify Kometa is properly installed with virtual environment

**Overlays Not Appearing**
- Enable "Reapply Overlays" in library operations
- Check that overlay files are selected in the Overlays tab

## 🤝 Support

This is an **unofficial** community tool for Kometa users. For:

- **Kometa Issues**: Visit [Kometa GitHub](https://github.com/Kometa-Team/Kometa)
- **GUI Issues**: Create an issue in this repository
- **General Help**: Join the [Kometa Discord](https://discord.gg/kometa)

## ⚖️ License

This project is provided as-is for the Kometa community. Not affiliated with the official Kometa team.

## 🙏 Acknowledgments

- **Kometa Team**: For the amazing media management tool
- **Community**: For feedback and testing
- **Contributors**: Thanks to all who help improve this interface

---

**Note**: This GUI generates standard Kometa YAML configurations. All credit for the underlying functionality goes to the [Kometa project](https://github.com/Kometa-Team/Kometa).