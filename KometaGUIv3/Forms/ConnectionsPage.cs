using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using KometaGUIv3.Models;
using KometaGUIv3.Services;
using KometaGUIv3.Utils;

namespace KometaGUIv3.Forms
{
    public partial class ConnectionsPage : UserControl
    {
        private PlexService plexService;
        private KometaProfile profile;
        private bool isValidated = false;

        // Controls
        private TextBox txtKometaDirectory, txtPlexEmail, txtPlexPassword, txtPlexUrl, txtTMDbApiKey;
        private Button btnBrowseDirectory, btnAuthenticatePlex, btnTMDbLink;
        private CheckedListBox clbLibraries;
        private Button btnSelectAll, btnUnselectAll;
        private Label lblValidationStatus;
        private GroupBox grpKometaDirectory, grpPlexSetup, grpLibrarySelection, grpTMDbSetup;

        public event EventHandler ValidationChanged;
        public bool IsPageValid => isValidated;

        public ConnectionsPage(KometaProfile profile)
        {
            this.profile = profile;
            plexService = new PlexService();
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
                Text = "Required Connections",
                Font = DarkTheme.GetTitleFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(400, 40),
                Location = new Point(30, 20)
            };

            // Kometa Directory Group
            grpKometaDirectory = new GroupBox
            {
                Text = "Kometa Installation Directory",
                Size = new Size(700, 80),
                Location = new Point(30, 70),
                ForeColor = DarkTheme.TextColor
            };

            txtKometaDirectory = new TextBox
            {
                Size = new Size(500, 25),
                Location = new Point(15, 30),
                PlaceholderText = "Select your Kometa installation directory..."
            };

            btnBrowseDirectory = new Button
            {
                Text = "Browse...",
                Size = new Size(80, 30),
                Location = new Point(530, 27)
            };

            grpKometaDirectory.Controls.AddRange(new Control[] { txtKometaDirectory, btnBrowseDirectory });

            // Plex Setup Group
            grpPlexSetup = new GroupBox
            {
                Text = "Plex Server Configuration",
                Size = new Size(700, 200),
                Location = new Point(30, 170),
                ForeColor = DarkTheme.TextColor
            };

            var lblPlexEmail = new Label
            {
                Text = "Plex Email:",
                Size = new Size(100, 20),
                Location = new Point(15, 35),
                ForeColor = DarkTheme.TextColor
            };

            txtPlexEmail = new TextBox
            {
                Size = new Size(200, 25),
                Location = new Point(120, 32)
            };

            var lblPlexPassword = new Label
            {
                Text = "Password:",
                Size = new Size(100, 20),
                Location = new Point(340, 35),
                ForeColor = DarkTheme.TextColor
            };

            txtPlexPassword = new TextBox
            {
                Size = new Size(200, 25),
                Location = new Point(420, 32),
                UseSystemPasswordChar = true
            };

            btnAuthenticatePlex = new Button
            {
                Text = "Authenticate",
                Size = new Size(100, 30),
                Location = new Point(15, 70)
            };

            var lblPlexUrl = new Label
            {
                Text = "Server URL:",
                Size = new Size(100, 20),
                Location = new Point(15, 115),
                ForeColor = DarkTheme.TextColor
            };

            txtPlexUrl = new TextBox
            {
                Size = new Size(300, 25),
                Location = new Point(120, 112),
                Text = "http://192.168.1.12:32400"
            };

            lblValidationStatus = new Label
            {
                Text = "Status: Not authenticated",
                Size = new Size(500, 20),
                Location = new Point(15, 155),
                ForeColor = Color.Orange
            };

            grpPlexSetup.Controls.AddRange(new Control[] {
                lblPlexEmail, txtPlexEmail, lblPlexPassword, txtPlexPassword,
                btnAuthenticatePlex, lblPlexUrl, txtPlexUrl, lblValidationStatus
            });

            // Library Selection Group
            grpLibrarySelection = new GroupBox
            {
                Text = "Plex Library Selection",
                Size = new Size(700, 200),
                Location = new Point(30, 390),
                ForeColor = DarkTheme.TextColor
            };

            clbLibraries = new CheckedListBox
            {
                Size = new Size(500, 150),
                Location = new Point(15, 30),
                CheckOnClick = true
            };

            btnSelectAll = new Button
            {
                Text = "Select All",
                Size = new Size(80, 30),
                Location = new Point(530, 40),
                Enabled = false
            };

            btnUnselectAll = new Button
            {
                Text = "Unselect All",
                Size = new Size(80, 30),
                Location = new Point(530, 80),
                Enabled = false
            };

            grpLibrarySelection.Controls.AddRange(new Control[] { clbLibraries, btnSelectAll, btnUnselectAll });

            // TMDb Setup Group
            grpTMDbSetup = new GroupBox
            {
                Text = "The Movie Database (TMDb) Configuration",
                Size = new Size(700, 80),
                Location = new Point(750, 170),
                ForeColor = DarkTheme.TextColor
            };

            var lblTMDbApiKey = new Label
            {
                Text = "API Key:",
                Size = new Size(70, 20),
                Location = new Point(15, 35),
                ForeColor = DarkTheme.TextColor
            };

            txtTMDbApiKey = new TextBox
            {
                Size = new Size(300, 25),
                Location = new Point(90, 32),
                PlaceholderText = "Enter your TMDb API key..."
            };

            btnTMDbLink = new Button
            {
                Text = "Get API Key",
                Size = new Size(100, 30),
                Location = new Point(410, 29)
            };

            grpTMDbSetup.Controls.AddRange(new Control[] { lblTMDbApiKey, txtTMDbApiKey, btnTMDbLink });

            // Add all groups to the page
            this.Controls.AddRange(new Control[] {
                titleLabel, grpKometaDirectory, grpPlexSetup, grpLibrarySelection, grpTMDbSetup
            });

            // Apply dark theme
            DarkTheme.ApplyDarkTheme(this);

            // Event handlers
            btnBrowseDirectory.Click += BtnBrowseDirectory_Click;
            btnAuthenticatePlex.Click += BtnAuthenticatePlex_Click;
            btnSelectAll.Click += BtnSelectAll_Click;
            btnUnselectAll.Click += BtnUnselectAll_Click;
            btnTMDbLink.Click += BtnTMDbLink_Click;
            txtKometaDirectory.TextChanged += ValidatePageInputs;
            txtTMDbApiKey.TextChanged += ValidatePageInputs;
            clbLibraries.ItemCheck += ClbLibraries_ItemCheck;
        }

        private async void BtnAuthenticatePlex_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPlexEmail.Text) || string.IsNullOrWhiteSpace(txtPlexPassword.Text))
            {
                MessageBox.Show("Please enter both email and password.", "Authentication Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnAuthenticatePlex.Enabled = false;
            btnAuthenticatePlex.Text = "Authenticating...";
            lblValidationStatus.Text = "Status: Authenticating...";
            lblValidationStatus.ForeColor = Color.Orange;

            try
            {
                // Step 1: Authenticate user
                var token = await plexService.AuthenticateUser(txtPlexEmail.Text, txtPlexPassword.Text);
                
                if (string.IsNullOrEmpty(token))
                {
                    MessageBox.Show("Authentication failed. Please check your credentials.", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                profile.Plex.Token = token;
                profile.Plex.Email = txtPlexEmail.Text;
                profile.Plex.IsAuthenticated = true;
                
                // Step 2: Discover servers
                btnAuthenticatePlex.Text = "Discovering servers...";
                lblValidationStatus.Text = "Status: Discovering Plex servers...";
                
                try
                {
                    var servers = await plexService.GetServerList(token);
                    var bestServer = plexService.FindBestServer(servers);
                    
                    if (bestServer != null)
                    {
                        // Auto-populate server URL
                        txtPlexUrl.Text = bestServer.GetUrl();
                        profile.Plex.Url = bestServer.GetUrl();
                        
                        lblValidationStatus.Text = $"Status: Found server - {bestServer.Name} ({bestServer.Address}:{bestServer.Port})";
                        lblValidationStatus.ForeColor = Color.LightBlue;
                        
                        // Step 3: Load libraries automatically
                        btnAuthenticatePlex.Text = "Loading libraries...";
                        lblValidationStatus.Text = "Status: Loading Plex libraries...";
                        
                        await LoadPlexLibraries();
                        
                        // Step 4: Complete setup
                        lblValidationStatus.Text = "Status: Connected and ready!";
                        lblValidationStatus.ForeColor = Color.LightGreen;
                        
                        MessageBox.Show($"Successfully connected to {bestServer.Name}!\nFound {clbLibraries.Items.Count} libraries.", 
                            "Connection Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // No servers found - graceful fallback
                        await HandleServerDiscoveryFallback();
                    }
                }
                catch (Exception serverEx)
                {
                    // Server discovery failed - graceful fallback
                    System.Diagnostics.Debug.WriteLine($"Server discovery failed: {serverEx.Message}");
                    await HandleServerDiscoveryFallback();
                }
            }
            catch (Exception ex)
            {
                lblValidationStatus.Text = "Status: Connection error";
                lblValidationStatus.ForeColor = Color.Red;
                MessageBox.Show($"Connection error: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnAuthenticatePlex.Enabled = true;
                btnAuthenticatePlex.Text = "Authenticate";
                ValidatePageInputs(null, null);
            }
        }


        private async Task HandleServerDiscoveryFallback()
        {
            // Authentication succeeded but server discovery failed
            lblValidationStatus.Text = "Status: Authenticated - Manual server setup required";
            lblValidationStatus.ForeColor = Color.Yellow;
            
            // Try with the existing URL in the text box
            if (!string.IsNullOrWhiteSpace(txtPlexUrl.Text))
            {
                try
                {
                    profile.Plex.Url = txtPlexUrl.Text;
                    
                    // Attempt to connect with manual URL
                    btnAuthenticatePlex.Text = "Testing manual server...";
                    lblValidationStatus.Text = "Status: Testing manual server connection...";
                    
                    var isValid = await plexService.ValidateConnection(profile.Plex.Url, profile.Plex.Token);
                    if (isValid)
                    {
                        // Manual connection successful - load libraries
                        await LoadPlexLibraries();
                        
                        lblValidationStatus.Text = "Status: Connected via manual server URL!";
                        lblValidationStatus.ForeColor = Color.LightGreen;
                        
                        MessageBox.Show($"Successfully connected to your Plex server!\nFound {clbLibraries.Items.Count} libraries.", 
                            "Connection Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }
                catch
                {
                    // Manual connection also failed
                }
            }
            
            // All connection attempts failed
            lblValidationStatus.Text = "Status: Authentication successful - Please verify server URL";
            lblValidationStatus.ForeColor = Color.Orange;
            
            MessageBox.Show("Authentication successful! However, server discovery failed.\n\nPlease verify your server URL and ensure your Plex server is accessible.", 
                "Server Connection Needed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private async Task LoadPlexLibraries()
        {
            try
            {
                // Get libraries from server
                var libraries = await plexService.GetLibraries(profile.Plex.Url, profile.Plex.Token);
                
                clbLibraries.Items.Clear();
                profile.Plex.AvailableLibraries.Clear();

                foreach (var library in libraries)
                {
                    profile.Plex.AvailableLibraries.Add(library);
                    clbLibraries.Items.Add($"{library.Name} ({library.Type})", library.IsSelected);
                }

                btnSelectAll.Enabled = clbLibraries.Items.Count > 0;
                btnUnselectAll.Enabled = clbLibraries.Items.Count > 0;
                
                // Auto-select all libraries by default for better UX
                if (clbLibraries.Items.Count > 0)
                {
                    for (int i = 0; i < clbLibraries.Items.Count; i++)
                    {
                        clbLibraries.SetItemChecked(i, true);
                    }
                    UpdateSelectedLibraries();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading libraries: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBrowseDirectory_Click(object sender, EventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select your Kometa installation directory";
                folderDialog.ShowNewFolderButton = false;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    if (IsValidKometaDirectory(folderDialog.SelectedPath))
                    {
                        txtKometaDirectory.Text = folderDialog.SelectedPath;
                        profile.KometaDirectory = folderDialog.SelectedPath;
                        ValidatePageInputs(null, null);
                    }
                    else
                    {
                        MessageBox.Show("Selected directory does not appear to contain a valid Kometa installation.\n\nPlease ensure the directory contains kometa.py or Kometa executable files.", 
                            "Invalid Directory", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
        }

        private bool IsValidKometaDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return false;

            // Check for common Kometa files
            var kometaFiles = new[] { "kometa.py", "kometa.exe", "requirements.txt", "defaults" };
            
            return kometaFiles.Any(file => 
                File.Exists(Path.Combine(path, file)) || 
                Directory.Exists(Path.Combine(path, file)));
        }

        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbLibraries.Items.Count; i++)
            {
                clbLibraries.SetItemChecked(i, true);
            }
            UpdateSelectedLibraries();
        }

        private void BtnUnselectAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < clbLibraries.Items.Count; i++)
            {
                clbLibraries.SetItemChecked(i, false);
            }
            UpdateSelectedLibraries();
        }

        private void BtnTMDbLink_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.themoviedb.org/settings/api");
        }

        private void ClbLibraries_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // Use BeginInvoke to ensure the check state is updated before processing
            this.BeginInvoke(new MethodInvoker(() =>
            {
                UpdateSelectedLibraries();
                ValidatePageInputs(null, null);
            }));
        }

        private void UpdateSelectedLibraries()
        {
            profile.SelectedLibraries.Clear();
            
            for (int i = 0; i < clbLibraries.Items.Count; i++)
            {
                if (clbLibraries.GetItemChecked(i) && i < profile.Plex.AvailableLibraries.Count)
                {
                    var library = profile.Plex.AvailableLibraries[i];
                    library.IsSelected = true;
                    profile.SelectedLibraries.Add(library.Name);
                }
            }
        }

        private void ValidatePageInputs(object sender, EventArgs e)
        {
            bool wasValid = isValidated;
            
            isValidated = !string.IsNullOrWhiteSpace(txtKometaDirectory.Text) &&
                         IsValidKometaDirectory(txtKometaDirectory.Text) &&
                         profile.Plex.IsAuthenticated &&
                         !string.IsNullOrWhiteSpace(profile.Plex.Url) &&
                         profile.SelectedLibraries.Count > 0 &&
                         !string.IsNullOrWhiteSpace(txtTMDbApiKey.Text);

            if (isValidated != wasValid)
            {
                ValidationChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void LoadProfileData()
        {
            if (profile != null)
            {
                txtKometaDirectory.Text = profile.KometaDirectory ?? "";
                txtPlexEmail.Text = profile.Plex.Email ?? "";
                txtPlexUrl.Text = profile.Plex.Url ?? "http://192.168.1.12:32400";
                txtTMDbApiKey.Text = profile.TMDb.ApiKey ?? "";
                
                if (profile.Plex.IsAuthenticated)
                {
                    lblValidationStatus.Text = "Status: Previously authenticated - Ready to reconnect";
                    lblValidationStatus.ForeColor = Color.Yellow;
                }
            }
        }

        public void SaveProfileData()
        {
            if (profile != null)
            {
                profile.KometaDirectory = txtKometaDirectory.Text;
                profile.Plex.Email = txtPlexEmail.Text;
                profile.Plex.Url = txtPlexUrl.Text;
                profile.TMDb.ApiKey = txtTMDbApiKey.Text;
                profile.TMDb.IsAuthenticated = !string.IsNullOrWhiteSpace(txtTMDbApiKey.Text);
                UpdateSelectedLibraries();
            }
        }
    }
}