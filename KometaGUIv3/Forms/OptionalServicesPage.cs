using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using KometaGUIv3.Models;
using KometaGUIv3.Utils;

namespace KometaGUIv3.Forms
{
    public partial class OptionalServicesPage : UserControl
    {
        private KometaProfile profile;
        private Dictionary<string, TextBox> serviceInputs;
        private string plexServerIp;

        // Service configurations
        private readonly Dictionary<string, ServiceConfig> LocalServices = new Dictionary<string, ServiceConfig>
        {
            ["tautulli"] = new ServiceConfig("Tautulli", 8181, "API Key", "Statistics and monitoring for Plex", true),
            ["radarr"] = new ServiceConfig("Radarr", 7878, "API Key", "Movie collection manager", true),
            ["sonarr"] = new ServiceConfig("Sonarr", 8989, "API Key", "TV series collection manager", true),
            ["gotify"] = new ServiceConfig("Gotify", 80, "Token", "Push notification service", false),
            ["ntfy"] = new ServiceConfig("ntfy", 80, "Token", "Simple push notification service", false)
        };

        private readonly Dictionary<string, ServiceConfig> ApiServices = new Dictionary<string, ServiceConfig>
        {
            ["github"] = new ServiceConfig("GitHub", 0, "Personal Access Token", "GitHub integration for custom configs"),
            ["omdb"] = new ServiceConfig("OMDb", 0, "API Key", "Open Movie Database", "http://www.omdbapi.com/apikey.aspx"),
            ["mdblist"] = new ServiceConfig("MDBList", 0, "API Key", "Movie/TV database service", "https://mdblist.com/api/"),
            ["notifiarr"] = new ServiceConfig("Notifiarr", 0, "API Key", "Notification service for media apps", "https://notifiarr.com/"),
            ["anidb"] = new ServiceConfig("AniDB", 0, "Username/Password", "Anime database", "https://anidb.net/"),
            ["trakt"] = new ServiceConfig("Trakt", 0, "Client ID/Secret", "Movie and TV show tracking", "https://trakt.tv/oauth/applications"),
            ["mal"] = new ServiceConfig("MyAnimeList", 0, "Client ID/Secret", "Anime and manga database", "https://myanimelist.net/apiconfig")
        };

        public OptionalServicesPage(KometaProfile profile)
        {
            this.profile = profile;
            this.serviceInputs = new Dictionary<string, TextBox>();
            this.plexServerIp = ExtractIpFromUrl(profile?.Plex?.Url ?? "http://192.168.1.12:32400");
            
            InitializeComponent();
            SetupControls();
            LoadProfileData();
        }

        private void SetupControls()
        {
            this.BackColor = DarkTheme.BackgroundColor;
            this.Dock = DockStyle.Fill;

            // Page title
            var titleLabel = new Label
            {
                Text = "Optional Service Configuration",
                Font = DarkTheme.GetTitleFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(500, 40),
                Location = new Point(30, 20)
            };

            var descriptionLabel = new Label
            {
                Text = "Configure optional services to enhance your Kometa experience. Local services are auto-populated with your Plex server IP address.",
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(900, 30),
                Location = new Point(30, 65)
            };

            // Scrollable panel for all services
            var scrollPanel = new Panel
            {
                Size = new Size(1340, 730), // Increased for bigger window
                Location = new Point(30, 110),
                AutoScroll = true,
                BackColor = DarkTheme.BackgroundColor
            };

            int yPosition = 10;

            // Local Services Section
            yPosition = CreateServiceSection(scrollPanel, "Local Services", 
                "These services typically run on your local network. URLs are auto-populated with your Plex server IP address.",
                LocalServices, yPosition);

            yPosition += 30; // Spacing between sections

            // API Services Section
            yPosition = CreateServiceSection(scrollPanel, "API Services", 
                "External API services that require registration and API keys.",
                ApiServices, yPosition);

            this.Controls.AddRange(new Control[] { titleLabel, descriptionLabel, scrollPanel });
            DarkTheme.ApplyDarkTheme(this);
        }

        private int CreateServiceSection(Panel parent, string sectionTitle, string sectionDescription, 
            Dictionary<string, ServiceConfig> services, int startY)
        {
            // Section header
            var sectionLabel = new Label
            {
                Text = sectionTitle,
                Font = DarkTheme.GetHeaderFont(),
                ForeColor = DarkTheme.AccentColor,
                Size = new Size(300, 30),
                Location = new Point(15, startY)
            };

            var sectionDesc = new Label
            {
                Text = sectionDescription,
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(800, 30),
                Location = new Point(15, startY + 30)
            };

            parent.Controls.Add(sectionLabel);
            parent.Controls.Add(sectionDesc);

            int y = startY + 70;

            foreach (var service in services)
            {
                y = CreateServiceRow(parent, service.Key, service.Value, y);
                y += 60; // Spacing between services
            }

            return y;
        }

        private int CreateServiceRow(Panel parent, string serviceId, ServiceConfig config, int y)
        {
            // Service name label
            var nameLabel = new Label
            {
                Text = config.Name,
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(120, 20),
                Location = new Point(15, y)
            };

            // Service description
            var descLabel = new Label
            {
                Text = config.Description,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray,
                Size = new Size(300, 30),
                Location = new Point(15, y + 20)
            };

            // URL field (for local services)
            if (config.IsLocal)
            {
                var urlLabel = new Label
                {
                    Text = "URL:",
                    Size = new Size(35, 20),
                    Location = new Point(150, y),
                    ForeColor = DarkTheme.TextColor
                };

                var urlTextBox = new TextBox
                {
                    Text = $"http://{plexServerIp}:{config.DefaultPort}",
                    Size = new Size(200, 25),
                    Location = new Point(190, y - 2),
                    Name = $"{serviceId}_url"
                };
                
                serviceInputs[$"{serviceId}_url"] = urlTextBox;
                parent.Controls.AddRange(new Control[] { urlLabel, urlTextBox });
            }

            // API Key/Token field
            var keyLabel = new Label
            {
                Text = $"{config.CredentialType}:",
                Size = new Size(80, 20),
                Location = new Point(config.IsLocal ? 420 : 150, y),
                ForeColor = DarkTheme.TextColor
            };

            var keyTextBox = new TextBox
            {
                Size = new Size(200, 25),
                Location = new Point(config.IsLocal ? 500 : 230, y - 2),
                Name = $"{serviceId}_key",
                UseSystemPasswordChar = true // Hide sensitive keys
            };

            serviceInputs[$"{serviceId}_key"] = keyTextBox;

            // API Link button (for API services)
            Button linkButton = null;
            if (!string.IsNullOrEmpty(config.ApiUrl))
            {
                linkButton = new Button
                {
                    Text = "Get API Key",
                    Size = new Size(100, 25),
                    Location = new Point(config.IsLocal ? 720 : 450, y - 2)
                };
                
                linkButton.Click += (s, e) => System.Diagnostics.Process.Start(config.ApiUrl);
            }

            // Advanced configuration button (for complex services)
            Button advancedButton = null;
            if (serviceId == "radarr" || serviceId == "sonarr" || serviceId == "trakt" || serviceId == "mal")
            {
                advancedButton = new Button
                {
                    Text = "Advanced...",
                    Size = new Size(80, 25),
                    Location = new Point(config.IsLocal ? 840 : 570, y - 2)
                };
                
                advancedButton.Click += (s, e) => ShowAdvancedConfig(serviceId, config.Name);
            }

            var controls = new List<Control> { nameLabel, descLabel, keyLabel, keyTextBox };
            if (linkButton != null) controls.Add(linkButton);
            if (advancedButton != null) controls.Add(advancedButton);

            parent.Controls.AddRange(controls.ToArray());

            return y + 50;
        }

        private void ShowAdvancedConfig(string serviceId, string serviceName)
        {
            var advancedForm = new Form
            {
                Text = $"{serviceName} Advanced Configuration",
                Size = new Size(500, 400),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = DarkTheme.BackgroundColor,
                ForeColor = DarkTheme.TextColor
            };

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = DarkTheme.BackgroundColor
            };

            var y = 20;

            if (serviceId == "radarr")
            {
                y = AddAdvancedField(panel, "Root Folder Path:", "S:/Movies", y);
                y = AddAdvancedField(panel, "Quality Profile:", "HD-1080p", y);
                y = AddAdvancedField(panel, "Monitor:", "true", y);
                y = AddAdvancedField(panel, "Availability:", "announced", y);
                y = AddAdvancedCheckbox(panel, "Add Missing", false, y);
                y = AddAdvancedCheckbox(panel, "Add Existing", false, y);
                y = AddAdvancedCheckbox(panel, "Upgrade Existing", false, y);
            }
            else if (serviceId == "sonarr")
            {
                y = AddAdvancedField(panel, "Root Folder Path:", "S:/TV Shows", y);
                y = AddAdvancedField(panel, "Quality Profile:", "HD-1080p", y);
                y = AddAdvancedField(panel, "Language Profile:", "English", y);
                y = AddAdvancedField(panel, "Series Type:", "standard", y);
                y = AddAdvancedField(panel, "Monitor:", "all", y);
                y = AddAdvancedCheckbox(panel, "Season Folder", true, y);
                y = AddAdvancedCheckbox(panel, "Add Missing", false, y);
                y = AddAdvancedCheckbox(panel, "Add Existing", false, y);
            }
            else if (serviceId == "trakt")
            {
                y = AddAdvancedField(panel, "Client ID:", "", y);
                y = AddAdvancedField(panel, "Client Secret:", "", y);
                y = AddAdvancedField(panel, "PIN:", "", y);
                
                var authLabel = new Label
                {
                    Text = "Authorization section will be auto-filled after authentication",
                    Font = new Font("Segoe UI", 8F),
                    ForeColor = Color.Gray,
                    Size = new Size(400, 30),
                    Location = new Point(20, y)
                };
                panel.Controls.Add(authLabel);
            }
            else if (serviceId == "mal")
            {
                y = AddAdvancedField(panel, "Client ID:", "", y);
                y = AddAdvancedField(panel, "Client Secret:", "", y);
                y = AddAdvancedField(panel, "Cache Expiration:", "60", y);
                y = AddAdvancedField(panel, "Localhost URL:", "", y);
            }

            var okButton = new Button
            {
                Text = "OK",
                Size = new Size(80, 30),
                Location = new Point(200, y + 20),
                DialogResult = DialogResult.OK
            };

            panel.Controls.Add(okButton);
            advancedForm.Controls.Add(panel);
            DarkTheme.ApplyDarkTheme(advancedForm);
            
            advancedForm.ShowDialog();
        }

        private int AddAdvancedField(Panel parent, string label, string defaultValue, int y)
        {
            var lbl = new Label
            {
                Text = label,
                Size = new Size(120, 20),
                Location = new Point(20, y),
                ForeColor = DarkTheme.TextColor
            };

            var txt = new TextBox
            {
                Text = defaultValue,
                Size = new Size(200, 25),
                Location = new Point(150, y - 2)
            };

            parent.Controls.AddRange(new Control[] { lbl, txt });
            return y + 35;
        }

        private int AddAdvancedCheckbox(Panel parent, string label, bool defaultValue, int y)
        {
            var chk = new CheckBox
            {
                Text = label,
                Checked = defaultValue,
                Size = new Size(200, 20),
                Location = new Point(20, y),
                ForeColor = DarkTheme.TextColor
            };

            parent.Controls.Add(chk);
            return y + 30;
        }

        private void LoadProfileData()
        {
            foreach (var serviceInput in serviceInputs)
            {
                if (profile.OptionalServices.ContainsKey(serviceInput.Key))
                {
                    serviceInput.Value.Text = profile.OptionalServices[serviceInput.Key];
                    serviceInput.Value.UseSystemPasswordChar = false; // Show existing values
                }
            }
        }

        public void SaveProfileData()
        {
            profile.OptionalServices.Clear();
            
            foreach (var serviceInput in serviceInputs)
            {
                if (!string.IsNullOrWhiteSpace(serviceInput.Value.Text))
                {
                    profile.OptionalServices[serviceInput.Key] = serviceInput.Value.Text;
                }
            }
        }

        private string ExtractIpFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host;
            }
            catch
            {
                return "192.168.1.12";
            }
        }
    }

    public class ServiceConfig
    {
        public string Name { get; set; }
        public int DefaultPort { get; set; }
        public string CredentialType { get; set; }
        public string Description { get; set; }
        public bool IsLocal { get; set; }
        public string ApiUrl { get; set; }

        public ServiceConfig(string name, int defaultPort, string credentialType, string description, bool isLocal = false, string apiUrl = null)
        {
            Name = name;
            DefaultPort = defaultPort;
            CredentialType = credentialType;
            Description = description;
            IsLocal = isLocal;
            ApiUrl = apiUrl;
        }

        public ServiceConfig(string name, int defaultPort, string credentialType, string description, string apiUrl)
            : this(name, defaultPort, credentialType, description, false, apiUrl)
        {
        }
    }
}