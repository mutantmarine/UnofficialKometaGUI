using System;
using System.Drawing;
using System.Windows.Forms;
using KometaGUIv3.Utils;
using KometaGUIv3.Services;
using KometaGUIv3.Models;

namespace KometaGUIv3.Forms
{
    public partial class MainForm : Form
    {
        private Panel navigationPanel;
        private Panel contentPanel;
        private Button btnBack, btnNext;
        private Label lblCurrentPage;
        private ProfileManager profileManager;
        private KometaProfile currentProfile;
        private int currentPageIndex = 0;
        
        private readonly string[] pageNames = {
            "Welcome",
            "Profile Management", 
            "Connections",
            "Collections", // Renamed from Charts
            "Overlays", 
            "Optional Services",
            "Settings",
            "Final Actions"
        };

        public MainForm()
        {
            InitializeComponent();
            profileManager = new ProfileManager();
            SetupForm();
            SetupNavigation();
            ShowPage(0); // Start with welcome page
        }

        private void SetupForm()
        {
            this.Text = "Kometa GUI v3";
            this.Size = new Size(1500, 1000); // Optimal size for all content
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1500, 1000); // Lock to optimal size
            this.MaximumSize = new Size(1500, 1000); // Prevent resizing beyond optimal
            this.FormBorderStyle = FormBorderStyle.FixedSingle; // Disable resize handle
            this.MaximizeBox = false; // Disable maximize button
            
            // Apply dark theme
            DarkTheme.ApplyDarkTheme(this);
        }

        private void SetupNavigation()
        {
            // Navigation panel at bottom
            navigationPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Bottom,
                BackColor = DarkTheme.PanelColor
            };

            btnBack = new Button
            {
                Text = "← Back",
                Size = new Size(100, 35),
                Location = new Point(20, 12),
                Enabled = false
            };
            
            btnNext = new Button
            {
                Text = "Next →",
                Size = new Size(100, 35),
                Location = new Point(130, 12),
                Name = "btnNext" // For primary button styling
            };

            lblCurrentPage = new Label
            {
                Text = "Welcome (1/8)",
                Size = new Size(200, 35),
                Location = new Point(250, 17),
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor
            };

            navigationPanel.Controls.AddRange(new Control[] { btnBack, btnNext, lblCurrentPage });
            
            // Content panel
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkTheme.BackgroundColor
            };

            this.Controls.Add(contentPanel);
            this.Controls.Add(navigationPanel);
            
            // Apply dark theme to navigation
            DarkTheme.ApplyDarkTheme(navigationPanel);
            
            // Event handlers
            btnBack.Click += BtnBack_Click;
            btnNext.Click += BtnNext_Click;
        }

        private void ShowPage(int pageIndex)
        {
            currentPageIndex = pageIndex;
            contentPanel.Controls.Clear();
            
            // Update navigation
            btnBack.Enabled = pageIndex > 0;
            btnNext.Enabled = true; // Will be controlled by page validation
            lblCurrentPage.Text = $"{pageNames[pageIndex]} ({pageIndex + 1}/{pageNames.Length})";
            
            // Load appropriate page content
            switch (pageIndex)
            {
                case 0:
                    ShowWelcomePage();
                    break;
                case 1:
                    ShowProfilePage();
                    break;
                case 2:
                    ShowConnectionsPage();
                    break;
                case 3:
                    ShowCollectionsPage(); // Renamed method
                    break;
                case 4:
                    ShowOverlaysPage();
                    break;
                case 5:
                    ShowServicesPage();
                    break;
                case 6:
                    ShowSettingsPage();
                    break;
                case 7:
                    ShowFinalActionsPage();
                    break;
            }
        }

        private void ShowWelcomePage()
        {
            var welcomePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkTheme.BackgroundColor
            };

            var titleLabel = new Label
            {
                Text = "Welcome to Kometa GUI v3",
                Font = DarkTheme.GetTitleFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(600, 40),
                Location = new Point(50, 50),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var descriptionLabel = new Label
            {
                Text = @"Kometa GUI v3 provides a user-friendly interface for managing your Kometa media library configurations.

Key Features:
• Profile-based configuration management
• Easy Plex and TMDb setup with validation
• Visual overlay positioning with 77+ default collections
• Comprehensive chart and award collections
• Optional service integrations (Tautulli, Radarr, Sonarr, etc.)
• YAML configuration generation
• Built-in Kometa execution and scheduling
• Real-time localhost web server with full feature parity

This guided setup will help you create professional media library configurations with ease.",
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(700, 300),
                Location = new Point(50, 120)
            };

            var letsGoBtn = new Button
            {
                Text = "Let's Go!",
                Size = new Size(150, 50),
                Location = new Point(350, 450),
                Font = DarkTheme.GetHeaderFont(),
                Name = "btnPrimary"
            };

            letsGoBtn.Click += (s, e) => ShowPage(1);

            welcomePanel.Controls.AddRange(new Control[] { titleLabel, descriptionLabel, letsGoBtn });
            DarkTheme.ApplyDarkTheme(welcomePanel);
            
            contentPanel.Controls.Add(welcomePanel);
            
            // Hide Next button on welcome page since we have Let's Go
            btnNext.Visible = false;
        }

        private void ShowProfilePage()
        {
            btnNext.Visible = true;
            
            var profilePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkTheme.BackgroundColor
            };

            var titleLabel = new Label
            {
                Text = "Profile Management",
                Font = DarkTheme.GetTitleFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(400, 40),
                Location = new Point(50, 30)
            };

            // Profile selection area
            var profileListBox = new ListBox
            {
                Size = new Size(300, 200),
                Location = new Point(50, 100),
                BackColor = DarkTheme.InputBackColor,
                ForeColor = DarkTheme.TextColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Load existing profiles
            foreach (var profileName in profileManager.GetProfileNames())
            {
                profileListBox.Items.Add(profileName);
            }

            var createBtn = new Button
            {
                Text = "Create New Profile",
                Size = new Size(150, 35),
                Location = new Point(370, 100)
            };

            var deleteBtn = new Button
            {
                Text = "Delete Selected",
                Size = new Size(150, 35),
                Location = new Point(370, 145),
                Enabled = false
            };

            var newProfileTextBox = new TextBox
            {
                Size = new Size(200, 25),
                Location = new Point(370, 200),
                PlaceholderText = "New profile name..."
            };

            // Event handlers
            profileListBox.SelectedIndexChanged += (s, e) =>
            {
                deleteBtn.Enabled = profileListBox.SelectedIndex >= 0;
                if (profileListBox.SelectedIndex >= 0)
                {
                    var profileName = profileListBox.SelectedItem.ToString();
                    currentProfile = profileManager.LoadProfile(profileName);
                    btnNext.Enabled = currentProfile != null;
                }
                else
                {
                    currentProfile = null;
                    btnNext.Enabled = false;
                }
            };

            createBtn.Click += (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(newProfileTextBox.Text))
                {
                    try
                    {
                        var profile = profileManager.CreateProfile(newProfileTextBox.Text);
                        profileListBox.Items.Add(profile.Name);
                        profileListBox.SelectedIndex = profileListBox.Items.Count - 1;
                        currentProfile = profile;
                        newProfileTextBox.Clear();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error creating profile: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            deleteBtn.Click += (s, e) =>
            {
                if (profileListBox.SelectedIndex >= 0)
                {
                    var profileName = profileListBox.SelectedItem.ToString();
                    var result = MessageBox.Show($"Are you sure you want to delete profile '{profileName}'?", 
                        "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        profileManager.DeleteProfile(profileName);
                        profileListBox.Items.RemoveAt(profileListBox.SelectedIndex);
                        currentProfile = null;
                        btnNext.Enabled = false;
                    }
                }
            };

            profilePanel.Controls.AddRange(new Control[] { 
                titleLabel, profileListBox, createBtn, deleteBtn, newProfileTextBox 
            });
            
            DarkTheme.ApplyDarkTheme(profilePanel);
            contentPanel.Controls.Add(profilePanel);
            
            btnNext.Enabled = profileListBox.SelectedIndex >= 0;
        }

        private void ShowConnectionsPage()
        {
            if (currentProfile == null)
            {
                MessageBox.Show("Please select or create a profile first.", "No Profile", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowPage(1); // Go back to profile page
                return;
            }

            var connectionsPage = new ConnectionsPage(currentProfile);
            connectionsPage.ValidationChanged += (s, e) =>
            {
                btnNext.Enabled = connectionsPage.IsPageValid;
            };
            
            contentPanel.Controls.Add(connectionsPage);
            btnNext.Enabled = connectionsPage.IsPageValid;
        }

        private void ShowCollectionsPage() // Renamed method
        {
            if (currentProfile == null)
            {
                MessageBox.Show("Please complete the previous steps first.", "Profile Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowPage(currentPageIndex - 1);
                return;
            }

            var collectionsPage = new ChartsPage(currentProfile); // Still using ChartsPage class but renamed conceptually
            contentPanel.Controls.Add(collectionsPage);
            btnNext.Enabled = true; // Collections are optional, always allow proceeding
        }

        private void ShowOverlaysPage()
        {
            if (currentProfile == null)
            {
                MessageBox.Show("Please complete the previous steps first.", "Profile Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowPage(currentPageIndex - 1);
                return;
            }

            var overlaysPage = new OverlaysPage(currentProfile);
            contentPanel.Controls.Add(overlaysPage);
            btnNext.Enabled = true; // Overlays are optional, always allow proceeding
        }

        private void ShowServicesPage()
        {
            if (currentProfile == null)
            {
                MessageBox.Show("Please complete the previous steps first.", "Profile Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowPage(currentPageIndex - 1);
                return;
            }

            var servicesPage = new OptionalServicesPage(currentProfile);
            contentPanel.Controls.Add(servicesPage);
            btnNext.Enabled = true; // Services are optional, always allow proceeding
        }

        private void ShowSettingsPage()
        {
            if (currentProfile == null)
            {
                MessageBox.Show("Please complete the previous steps first.", "Profile Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowPage(currentPageIndex - 1);
                return;
            }

            var settingsPage = new SettingsPage(currentProfile);
            contentPanel.Controls.Add(settingsPage);
            btnNext.Enabled = true; // Settings have defaults, always allow proceeding
        }

        private void ShowFinalActionsPage()
        {
            if (currentProfile == null)
            {
                MessageBox.Show("Please complete the previous steps first.", "Profile Required", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowPage(currentPageIndex - 1);
                return;
            }

            var finalActionsPage = new FinalActionsPage(currentProfile, profileManager);
            contentPanel.Controls.Add(finalActionsPage);
            btnNext.Enabled = false; // This is the final page
            btnNext.Text = "Finish";
        }

        private void ShowComingSoonPage(string pageName, string features)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkTheme.BackgroundColor
            };

            var titleLabel = new Label
            {
                Text = pageName,
                Font = DarkTheme.GetTitleFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(400, 40),
                Location = new Point(50, 30)
            };

            var comingSoonLabel = new Label
            {
                Text = $"{pageName} page coming soon...\n\n{features}",
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(600, 300),
                Location = new Point(50, 100)
            };

            panel.Controls.AddRange(new Control[] { titleLabel, comingSoonLabel });
            DarkTheme.ApplyDarkTheme(panel);
            contentPanel.Controls.Add(panel);
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            // Save current page data before going back
            SaveCurrentPageData();
            
            if (currentPageIndex > 0)
            {
                ShowPage(currentPageIndex - 1);
            }
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            // Save current page data before navigating
            SaveCurrentPageData();
            
            if (currentPageIndex < pageNames.Length - 1)
            {
                ShowPage(currentPageIndex + 1);
            }
            else if (currentPageIndex == pageNames.Length - 1 && btnNext.Text == "Finish")
            {
                // Handle finish button on final page
                var result = MessageBox.Show("Configuration complete! You can continue using the application or close it.", 
                    "Configuration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Could potentially close the application or return to first page
                // For now, just disable the button
                btnNext.Enabled = false;
            }
        }
        
        private void SaveCurrentPageData()
        {
            if (currentProfile == null) return;
            
            // Get the current page control and save its data
            var currentPageControl = contentPanel.Controls.Count > 0 ? contentPanel.Controls[0] : null;
            
            switch (currentPageIndex)
            {
                case 2: // Connections
                    if (currentPageControl is ConnectionsPage connectionsPage)
                    {
                        connectionsPage.SaveProfileData();
                    }
                    break;
                case 3: // Collections (formerly Charts)
                    if (currentPageControl is ChartsPage collectionsPage)
                    {
                        collectionsPage.SaveProfileData();
                    }
                    break;
                case 4: // Overlays
                    if (currentPageControl is OverlaysPage overlaysPage)
                    {
                        overlaysPage.SaveProfileData();
                    }
                    break;
                case 5: // Optional Services
                    if (currentPageControl is OptionalServicesPage servicesPage)
                    {
                        servicesPage.SaveProfileData();
                    }
                    break;
                case 6: // Settings
                    if (currentPageControl is SettingsPage settingsPage)
                    {
                        settingsPage.SaveProfileData();
                    }
                    break;
            }
            
            // Save profile to disk
            profileManager.SaveProfile(currentProfile);
        }
    }
}