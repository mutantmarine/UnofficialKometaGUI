using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KometaGUIv3.Models;
using KometaGUIv3.Utils;

namespace KometaGUIv3.Forms
{
    public partial class OptionalServicesPage : UserControl
    {
        private KometaProfile profile;
        private Dictionary<string, TextBox> serviceInputs;
        private Dictionary<string, CheckBox> serviceCheckboxes;
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
            this.serviceCheckboxes = new Dictionary<string, CheckBox>();
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
            // Enable checkbox (disabled by default)
            var enableCheckbox = new CheckBox
            {
                Size = new Size(15, 15),
                Location = new Point(15, y + 2),
                Name = $"{serviceId}_enabled",
                Checked = false // Default to disabled
            };
            enableCheckbox.CheckedChanged += (s, e) => UpdateServiceControlsState(serviceId, enableCheckbox.Checked);
            serviceCheckboxes[serviceId] = enableCheckbox;

            // Service name label (moved right to accommodate checkbox)
            var nameLabel = new Label
            {
                Text = config.Name,
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(120, 20),
                Location = new Point(40, y)
            };

            // Service description (moved right to accommodate checkbox)
            var descLabel = new Label
            {
                Text = config.Description,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray,
                Size = new Size(300, 30),
                Location = new Point(40, y + 20)
            };

            var controls = new List<Control> { enableCheckbox, nameLabel, descLabel };

            // Special handling for Trakt - show all fields on main page
            if (serviceId == "trakt")
            {
                return CreateTraktServiceRow(parent, controls, y);
            }
            // Special handling for MAL - show Client ID/Secret on main page, keep Advanced button  
            else if (serviceId == "mal")
            {
                return CreateMalServiceRow(parent, controls, config, y);
            }
            else
            {
                return CreateStandardServiceRow(parent, controls, serviceId, config, y);
            }
        }

        private int CreateTraktServiceRow(Panel parent, List<Control> controls, int y)
        {
            // Client ID field
            var clientIdLabel = new Label
            {
                Text = "Client ID:",
                Size = new Size(60, 20),
                Location = new Point(175, y),
                ForeColor = DarkTheme.TextColor,
                Name = "trakt_client_id_label"
            };

            var clientIdTextBox = new TextBox
            {
                Size = new Size(150, 25),
                Location = new Point(240, y - 2),
                Name = "trakt_client_id",
                Enabled = false
            };

            // Client Secret field
            var clientSecretLabel = new Label
            {
                Text = "Secret:",
                Size = new Size(45, 20),
                Location = new Point(400, y),
                ForeColor = DarkTheme.TextColor,
                Name = "trakt_client_secret_label"
            };

            var clientSecretTextBox = new TextBox
            {
                Size = new Size(150, 25),
                Location = new Point(450, y - 2),
                Name = "trakt_client_secret",
                UseSystemPasswordChar = true,
                Enabled = false
            };

            // PIN field
            var pinLabel = new Label
            {
                Text = "PIN:",
                Size = new Size(30, 20),
                Location = new Point(610, y),
                ForeColor = DarkTheme.TextColor,
                Name = "trakt_pin_label"
            };

            var pinTextBox = new TextBox
            {
                Size = new Size(80, 25),
                Location = new Point(645, y - 2),
                Name = "trakt_pin",
                Enabled = false
            };

            // API Link button
            var linkButton = new Button
            {
                Text = "Get API Key",
                Size = new Size(100, 25),
                Location = new Point(735, y - 2),
                Name = "trakt_link",
                Enabled = false
            };
            linkButton.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://trakt.tv/oauth/applications") { UseShellExecute = true });

            serviceInputs["trakt_client_id"] = clientIdTextBox;
            serviceInputs["trakt_client_secret"] = clientSecretTextBox;
            serviceInputs["trakt_pin"] = pinTextBox;

            controls.AddRange(new Control[] { clientIdLabel, clientIdTextBox, clientSecretLabel, clientSecretTextBox, pinLabel, pinTextBox, linkButton });
            parent.Controls.AddRange(controls.ToArray());

            UpdateServiceControlsState("trakt", false);
            return y + 50;
        }

        private int CreateMalServiceRow(Panel parent, List<Control> controls, ServiceConfig config, int y)
        {
            // Client ID field
            var clientIdLabel = new Label
            {
                Text = "Client ID:",
                Size = new Size(60, 20),
                Location = new Point(175, y),
                ForeColor = DarkTheme.TextColor,
                Name = "mal_client_id_label"
            };

            var clientIdTextBox = new TextBox
            {
                Size = new Size(150, 25),
                Location = new Point(240, y - 2),
                Name = "mal_client_id",
                Enabled = false
            };

            // Client Secret field
            var clientSecretLabel = new Label
            {
                Text = "Secret:",
                Size = new Size(45, 20),
                Location = new Point(400, y),
                ForeColor = DarkTheme.TextColor,
                Name = "mal_client_secret_label"
            };

            var clientSecretTextBox = new TextBox
            {
                Size = new Size(150, 25),
                Location = new Point(450, y - 2),
                Name = "mal_client_secret",
                UseSystemPasswordChar = true,
                Enabled = false
            };

            // API Link button
            var linkButton = new Button
            {
                Text = "Get API Key",
                Size = new Size(100, 25),
                Location = new Point(610, y - 2),
                Name = "mal_link",
                Enabled = false
            };
            linkButton.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(config.ApiUrl) { UseShellExecute = true });

            // Advanced button (for cache expiration and localhost URL)
            var advancedButton = new Button
            {
                Text = "Advanced...",
                Size = new Size(80, 25),
                Location = new Point(720, y - 2),
                Name = "mal_advanced",
                Enabled = false
            };
            advancedButton.Click += (s, e) => ShowAdvancedConfig("mal", config.Name);

            serviceInputs["mal_client_id"] = clientIdTextBox;
            serviceInputs["mal_client_secret"] = clientSecretTextBox;

            controls.AddRange(new Control[] { clientIdLabel, clientIdTextBox, clientSecretLabel, clientSecretTextBox, linkButton, advancedButton });
            parent.Controls.AddRange(controls.ToArray());

            UpdateServiceControlsState("mal", false);
            return y + 50;
        }

        private int CreateStandardServiceRow(Panel parent, List<Control> controls, string serviceId, ServiceConfig config, int y)
        {
            // URL field (for local services) - shifted right
            if (config.IsLocal)
            {
                var urlLabel = new Label
                {
                    Text = "URL:",
                    Size = new Size(35, 20),
                    Location = new Point(175, y),
                    ForeColor = DarkTheme.TextColor,
                    Name = $"{serviceId}_url_label"
                };

                var urlTextBox = new TextBox
                {
                    Text = $"http://{plexServerIp}:{config.DefaultPort}",
                    Size = new Size(200, 25),
                    Location = new Point(215, y - 2),
                    Name = $"{serviceId}_url",
                    Enabled = false // Start disabled
                };
                
                serviceInputs[$"{serviceId}_url"] = urlTextBox;
                controls.AddRange(new Control[] { urlLabel, urlTextBox });
            }

            // API Key/Token field - shifted right
            var keyLabel = new Label
            {
                Text = $"{config.CredentialType}:",
                Size = new Size(75, 20),
                Location = new Point(config.IsLocal ? 445 : 175, y),
                ForeColor = DarkTheme.TextColor,
                Name = $"{serviceId}_key_label"
            };

            var keyTextBox = new TextBox
            {
                Size = new Size(200, 25),
                Location = new Point(config.IsLocal ? 525 : 305, y - 2),
                Name = $"{serviceId}_key",
                UseSystemPasswordChar = true, // Hide sensitive keys
                Enabled = false // Start disabled
            };

            serviceInputs[$"{serviceId}_key"] = keyTextBox;
            controls.AddRange(new Control[] { keyLabel, keyTextBox });

            // API Link button (for API services) - shifted right
            if (!string.IsNullOrEmpty(config.ApiUrl))
            {
                var linkButton = new Button
                {
                    Text = "Get API Key",
                    Size = new Size(100, 25),
                    Location = new Point(config.IsLocal ? 745 : 515, y - 2),
                    Name = $"{serviceId}_link",
                    Enabled = false // Start disabled
                };
                
                linkButton.Click += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(config.ApiUrl) { UseShellExecute = true });
                controls.Add(linkButton);
            }

            // Advanced configuration button (for complex services) - shifted right
            if (serviceId == "radarr" || serviceId == "sonarr")
            {
                var advancedButton = new Button
                {
                    Text = "Advanced...",
                    Size = new Size(80, 25),
                    Location = new Point(config.IsLocal ? 865 : 625, y - 2),
                    Name = $"{serviceId}_advanced",
                    Enabled = false // Start disabled
                };
                
                advancedButton.Click += (s, e) => ShowAdvancedConfig(serviceId, config.Name);
                controls.Add(advancedButton);
            }

            parent.Controls.AddRange(controls.ToArray());
            
            // Initialize the disabled visual state
            UpdateServiceControlsState(serviceId, false);

            return y + 50;
        }

        private void UpdateServiceControlsState(string serviceId, bool isEnabled)
        {
            // Find all controls for this service by searching through the parent panel
            var scrollPanel = this.Controls.OfType<Panel>().FirstOrDefault(p => p.AutoScroll);
            if (scrollPanel == null) return;

            var serviceControls = scrollPanel.Controls.OfType<Control>()
                .Where(c => c.Name != null && c.Name.StartsWith($"{serviceId}_"))
                .ToList();

            foreach (var control in serviceControls)
            {
                if (control is TextBox textBox)
                {
                    textBox.Enabled = isEnabled;
                    textBox.BackColor = isEnabled ? DarkTheme.InputBackColor : Color.DarkGray;
                    textBox.ForeColor = isEnabled ? DarkTheme.TextColor : Color.Gray;
                }
                else if (control is Button button && !control.Name.Contains("_enabled"))
                {
                    button.Enabled = isEnabled;
                    button.BackColor = isEnabled ? DarkTheme.ButtonColor : Color.DarkGray;
                    button.ForeColor = isEnabled ? DarkTheme.TextColor : Color.Gray;
                }
                else if (control is Label label && control.Name.Contains("_label"))
                {
                    label.ForeColor = isEnabled ? DarkTheme.TextColor : Color.Gray;
                }
            }

            // Also find and update the main service name and description labels
            var allLabels = scrollPanel.Controls.OfType<Label>().ToList();
            var serviceConfig = LocalServices.ContainsKey(serviceId) ? LocalServices[serviceId] : 
                               ApiServices.ContainsKey(serviceId) ? ApiServices[serviceId] : null;
            
            if (serviceConfig != null)
            {
                var nameLabel = allLabels.FirstOrDefault(l => l.Text == serviceConfig.Name);
                var descLabel = allLabels.FirstOrDefault(l => l.Text == serviceConfig.Description);
                
                if (nameLabel != null)
                    nameLabel.ForeColor = isEnabled ? DarkTheme.TextColor : Color.Gray;
                if (descLabel != null)
                    descLabel.ForeColor = isEnabled ? Color.Gray : Color.DarkGray;
            }
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
            else if (serviceId == "mal")
            {
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

            // Store advanced fields in serviceInputs for MAL
            if (label.StartsWith("Cache Expiration"))
            {
                txt.Name = "mal_cache_expiration";
                serviceInputs["mal_cache_expiration"] = txt;
            }
            else if (label.StartsWith("Localhost URL"))
            {
                txt.Name = "mal_localhost_url";
                serviceInputs["mal_localhost_url"] = txt;
            }

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
            // Load service input values
            foreach (var serviceInput in serviceInputs)
            {
                if (profile.OptionalServices.ContainsKey(serviceInput.Key))
                {
                    serviceInput.Value.Text = profile.OptionalServices[serviceInput.Key];
                    serviceInput.Value.UseSystemPasswordChar = false; // Show existing values
                }
            }
            
            // Load enabled states (default to false if not found)
            foreach (var serviceCheckbox in serviceCheckboxes)
            {
                string serviceId = serviceCheckbox.Key;
                bool isEnabled = profile.EnabledServices.ContainsKey(serviceId) 
                    ? profile.EnabledServices[serviceId] 
                    : false; // Default to disabled
                    
                serviceCheckbox.Value.Checked = isEnabled;
                UpdateServiceControlsState(serviceId, isEnabled);
            }
        }

        public void SaveProfileData()
        {
            profile.OptionalServices.Clear();
            profile.EnabledServices.Clear();
            
            // Save enabled states
            foreach (var serviceCheckbox in serviceCheckboxes)
            {
                profile.EnabledServices[serviceCheckbox.Key] = serviceCheckbox.Value.Checked;
            }
            
            // Only save service input data if the service is enabled
            foreach (var serviceInput in serviceInputs)
            {
                string serviceId = ExtractServiceIdFromInputKey(serviceInput.Key);
                bool isServiceEnabled = serviceCheckboxes.ContainsKey(serviceId) && serviceCheckboxes[serviceId].Checked;
                
                if (isServiceEnabled && !string.IsNullOrWhiteSpace(serviceInput.Value.Text))
                {
                    profile.OptionalServices[serviceInput.Key] = serviceInput.Value.Text;
                }
            }
        }
        
        private string ExtractServiceIdFromInputKey(string inputKey)
        {
            // inputKey format: "serviceid_url" or "serviceid_key"
            var parts = inputKey.Split('_');
            return parts.Length > 1 ? parts[0] : inputKey;
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