using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using KometaGUIv3.Shared.Models;
using KometaGUIv3.Shared.Services;
using KometaGUIv3.Services;
using KometaGUIv3.Utils;

namespace KometaGUIv3.Forms
{
    public partial class ConnectionsPage : UserControl
    {
        private PlexService plexService;
        private PlexOAuthService plexOAuthService;
        private KometaProfile profile;
        private bool isValidated = false;
        private List<PlexServer> discoveredServers = new List<PlexServer>();
        private bool isManualMode = false; // Track dropdown vs manual mode

        // Controls
        private TextBox txtKometaDirectory, txtPlexToken, txtPlexUrl, txtTMDbApiKey;
        private Button btnBrowseDirectory, btnAuthenticatePlex, btnAuthenticateToken, btnTMDbLink;
        private Button btnToggleMode, btnGetLibraries;
        private ComboBox cmbServerSelection;
        private CheckedListBox clbLibraries;
        private Button btnSelectAll, btnUnselectAll;
        private Label lblValidationStatus;
        private GroupBox grpKometaDirectory, grpPlexSetup, grpLibrarySelection, grpTMDbSetup;
        
        // Advanced configuration controls
        private NumericUpDown nudTimeout, nudDbCache, nudCacheExpiration;
        private CheckBox cbCleanBundles, cbEmptyTrash, cbOptimize, cbVerifySSL;
        private TextBox txtLanguage, txtRegion;
        private ComboBox cmbLanguage;

        public event EventHandler ValidationChanged;
        public bool IsPageValid => isValidated;

        public ConnectionsPage(KometaProfile profile)
        {
            this.profile = profile;
            plexService = new PlexService();
            plexOAuthService = new PlexOAuthService();
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
                Text = "Kometa Directory",
                Size = new Size(700, 110),
                Location = new Point(30, 80),
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

            var lblDirectoryNote = new Label
            {
                Text = "Note: Directory folder must be named Kometa (e.g. C:\\path\\to\\Kometa) and may be empty or contain preexisting files.",
                Size = new Size(680, 40),
                Location = new Point(15, 60),
                ForeColor = Color.FromArgb(180, 180, 180), // Muted color
                Font = new Font(DarkTheme.GetDefaultFont().FontFamily, 8.5F, FontStyle.Regular)
            };

            grpKometaDirectory.Controls.AddRange(new Control[] { txtKometaDirectory, btnBrowseDirectory, lblDirectoryNote });

            // Plex Setup Group
            grpPlexSetup = new GroupBox
            {
                Text = "Plex Server Configuration",
                Size = new Size(700, 310),
                Location = new Point(30, 200),
                ForeColor = DarkTheme.TextColor
            };

            var lblAuthOptions = new Label
            {
                Text = "Authentication Methods:",
                Size = new Size(150, 20),
                Location = new Point(15, 35),
                ForeColor = DarkTheme.TextColor,
                Font = new Font(DarkTheme.GetDefaultFont().FontFamily, 9F, FontStyle.Bold)
            };

            btnAuthenticatePlex = new Button
            {
                Text = "Authenticate Plex Account",
                Size = new Size(170, 30),
                Location = new Point(15, 65)
            };

            var lblOr = new Label
            {
                Text = "OR",
                Size = new Size(25, 20),
                Location = new Point(200, 71),
                ForeColor = DarkTheme.TextColor,
                Font = new Font(DarkTheme.GetDefaultFont().FontFamily, 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnAuthenticateToken = new Button
            {
                Text = "Authenticate with Token",
                Size = new Size(150, 30),
                Location = new Point(240, 65)
            };

            var lblPlexToken = new Label
            {
                Text = "Plex Token:",
                Size = new Size(80, 20),
                Location = new Point(15, 105),
                ForeColor = DarkTheme.TextColor
            };

            txtPlexToken = new TextBox
            {
                Size = new Size(500, 25),
                Location = new Point(100, 102),
                PlaceholderText = "Token will appear here after authentication, or paste your existing token..."
            };

            var lblPlexUrl = new Label
            {
                Text = "Server URL:",
                Size = new Size(80, 20),
                Location = new Point(15, 140),
                ForeColor = DarkTheme.TextColor
            };

            txtPlexUrl = new TextBox
            {
                Size = new Size(300, 25),
                Location = new Point(100, 137),
                Text = "http://192.168.1.12:32400"
            };

            btnToggleMode = new Button
            {
                Text = "Enter Manually",
                Size = new Size(120, 25),
                Location = new Point(410, 137),
                Visible = false // Hidden until authentication succeeds
            };

            cmbServerSelection = new ComboBox
            {
                Size = new Size(300, 25),
                Location = new Point(100, 137),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false // Hidden initially, becomes default when authenticated
            };

            btnGetLibraries = new Button
            {
                Text = "Refresh Libraries",
                Size = new Size(110, 25),
                Location = new Point(540, 137),
                Visible = false // Hidden until authentication succeeds
            };

            lblValidationStatus = new Label
            {
                Text = "Status: Not authenticated",
                Size = new Size(500, 20),
                Location = new Point(15, 175),
                ForeColor = Color.Orange
            };

            // Advanced Plex Settings
            var lblAdvancedPlex = new Label
            {
                Text = "Advanced Settings:",
                Size = new Size(120, 20),
                Location = new Point(15, 205),
                ForeColor = DarkTheme.AccentColor,
                Font = new Font(DarkTheme.GetDefaultFont().FontFamily, 9F, FontStyle.Bold)
            };

            var lblTimeout = new Label
            {
                Text = "Timeout (sec):",
                Size = new Size(80, 20),
                Location = new Point(15, 235),
                ForeColor = DarkTheme.TextColor
            };

            nudTimeout = new NumericUpDown
            {
                Size = new Size(60, 25),
                Location = new Point(100, 232),
                Minimum = 10,
                Maximum = 300,
                Value = 60
            };

            var lblDbCache = new Label
            {
                Text = "DB Cache:",
                Size = new Size(60, 20),
                Location = new Point(180, 235),
                ForeColor = DarkTheme.TextColor
            };

            nudDbCache = new NumericUpDown
            {
                Size = new Size(60, 25),
                Location = new Point(245, 232),
                Minimum = 10,
                Maximum = 200,
                Value = 40
            };

            cbVerifySSL = new CheckBox
            {
                Text = "Verify SSL",
                Size = new Size(80, 20),
                Location = new Point(320, 235),
                Checked = true,
                ForeColor = DarkTheme.TextColor
            };

            cbCleanBundles = new CheckBox
            {
                Text = "Clean Bundles",
                Size = new Size(100, 20),
                Location = new Point(15, 270),
                ForeColor = DarkTheme.TextColor
            };

            cbEmptyTrash = new CheckBox
            {
                Text = "Empty Trash",
                Size = new Size(90, 20),
                Location = new Point(125, 270),
                ForeColor = DarkTheme.TextColor
            };

            cbOptimize = new CheckBox
            {
                Text = "Optimize DB",
                Size = new Size(90, 20),
                Location = new Point(225, 270),
                ForeColor = DarkTheme.TextColor
            };

            grpPlexSetup.Controls.AddRange(new Control[] {
                lblAuthOptions, btnAuthenticatePlex, lblOr, btnAuthenticateToken,
                lblPlexToken, txtPlexToken, lblPlexUrl, txtPlexUrl, btnToggleMode,
                cmbServerSelection, btnGetLibraries, lblValidationStatus,
                lblAdvancedPlex, lblTimeout, nudTimeout, lblDbCache, nudDbCache,
                cbVerifySSL, cbCleanBundles, cbEmptyTrash, cbOptimize
            });

            // Library Selection Group
            grpLibrarySelection = new GroupBox
            {
                Text = "Plex Library Selection",
                Size = new Size(700, 200),
                Location = new Point(30, 520),
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
                Size = new Size(700, 140),
                Location = new Point(30, 740),
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

            // Advanced TMDb Settings
            var lblAdvancedTMDb = new Label
            {
                Text = "Advanced Settings:",
                Size = new Size(120, 20),
                Location = new Point(15, 70),
                ForeColor = DarkTheme.AccentColor,
                Font = new Font(DarkTheme.GetDefaultFont().FontFamily, 9F, FontStyle.Bold)
            };

            var lblCacheExpiration = new Label
            {
                Text = "Cache (days):",
                Size = new Size(80, 20),
                Location = new Point(15, 100),
                ForeColor = DarkTheme.TextColor
            };

            nudCacheExpiration = new NumericUpDown
            {
                Size = new Size(60, 25),
                Location = new Point(100, 97),
                Minimum = 1,
                Maximum = 365,
                Value = 60
            };

            var lblLanguage = new Label
            {
                Text = "Language:",
                Size = new Size(60, 20),
                Location = new Point(180, 100),
                ForeColor = DarkTheme.TextColor
            };

            cmbLanguage = new ComboBox
            {
                Size = new Size(60, 25),
                Location = new Point(245, 97),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbLanguage.Items.AddRange(new[] { "en", "es", "fr", "de", "it", "pt", "ja", "ko", "zh", "ru" });
            cmbLanguage.SelectedItem = "en";

            var lblRegion = new Label
            {
                Text = "Region:",
                Size = new Size(50, 20),
                Location = new Point(320, 100),
                ForeColor = DarkTheme.TextColor
            };

            txtRegion = new TextBox
            {
                Size = new Size(60, 25),
                Location = new Point(375, 97),
                PlaceholderText = "US, GB, etc."
            };

            grpTMDbSetup.Controls.AddRange(new Control[] { 
                lblTMDbApiKey, txtTMDbApiKey, btnTMDbLink,
                lblAdvancedTMDb, lblCacheExpiration, nudCacheExpiration,
                lblLanguage, cmbLanguage, lblRegion, txtRegion 
            });

            // Add all groups to the page
            this.Controls.AddRange(new Control[] {
                titleLabel, grpKometaDirectory, grpPlexSetup, grpLibrarySelection, grpTMDbSetup
            });

            // Apply dark theme
            DarkTheme.ApplyDarkTheme(this);

            // Event handlers
            btnBrowseDirectory.Click += BtnBrowseDirectory_Click;
            btnAuthenticatePlex.Click += BtnAuthenticatePlex_Click;
            btnAuthenticateToken.Click += BtnAuthenticateToken_Click;
            btnToggleMode.Click += BtnToggleMode_Click;
            btnGetLibraries.Click += BtnGetLibraries_Click;
            btnSelectAll.Click += BtnSelectAll_Click;
            btnUnselectAll.Click += BtnUnselectAll_Click;
            btnTMDbLink.Click += BtnTMDbLink_Click;
            txtKometaDirectory.TextChanged += ValidatePageInputs;
            txtPlexToken.TextChanged += ValidatePageInputs;
            txtTMDbApiKey.TextChanged += ValidatePageInputs;
            txtPlexUrl.TextChanged += TxtPlexUrl_TextChanged; // Auto-save manual entries
            cmbServerSelection.SelectedIndexChanged += CmbServerSelection_SelectedIndexChanged; // Auto-save dropdown selections
            clbLibraries.ItemCheck += ClbLibraries_ItemCheck;
        }

        private async void BtnAuthenticatePlex_Click(object sender, EventArgs e)
        {
            btnAuthenticatePlex.Enabled = false;
            btnAuthenticateToken.Enabled = false;
            btnAuthenticatePlex.Text = "Authenticating...";
            lblValidationStatus.Text = "Status: Opening browser for authentication...";
            lblValidationStatus.ForeColor = Color.Orange;

            try
            {
                // Step 1: OAuth Browser Authentication
                var token = await plexOAuthService.AuthenticateWithBrowser();
                
                if (string.IsNullOrEmpty(token))
                {
                    MessageBox.Show("Authentication failed or was cancelled.", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Step 2: Populate token and complete authentication
                txtPlexToken.Text = token;
                await CompleteAuthentication(token);
            }
            catch (Exception ex)
            {
                lblValidationStatus.Text = "Status: Authentication failed";
                lblValidationStatus.ForeColor = Color.Red;
                MessageBox.Show($"Authentication error: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnAuthenticatePlex.Enabled = true;
                btnAuthenticateToken.Enabled = true;
                btnAuthenticatePlex.Text = "Authenticate Plex Account";
                ValidatePageInputs(null, null);
            }
        }

        private async void BtnAuthenticateToken_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtPlexToken.Text))
            {
                MessageBox.Show("Please enter a Plex token.", "Authentication Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnAuthenticatePlex.Enabled = false;
            btnAuthenticateToken.Enabled = false;
            btnAuthenticateToken.Text = "Authenticating...";
            lblValidationStatus.Text = "Status: Validating token...";
            lblValidationStatus.ForeColor = Color.Orange;

            try
            {
                await CompleteAuthentication(txtPlexToken.Text);
            }
            catch (Exception ex)
            {
                lblValidationStatus.Text = "Status: Authentication failed";
                lblValidationStatus.ForeColor = Color.Red;
                MessageBox.Show($"Authentication error: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnAuthenticatePlex.Enabled = true;
                btnAuthenticateToken.Enabled = true;
                btnAuthenticateToken.Text = "Authenticate with Token";
                ValidatePageInputs(null, null);
            }
        }

        private async Task CompleteAuthentication(string token)
        {
            // Validate token first
            var isValidToken = await plexOAuthService.ValidateToken(token);
            if (!isValidToken)
            {
                throw new Exception("Invalid or expired token");
            }

            profile.Plex.Token = token;
            profile.Plex.IsAuthenticated = true;
            
            // Step 2: Discover servers
            lblValidationStatus.Text = "Status: Discovering Plex servers...";
            
            try
            {
                var servers = await plexService.GetServerList(token);
                discoveredServers = servers; // Store all discovered servers
                profile.Plex.DiscoveredServers = servers; // Save discovered servers to profile
                var bestServer = plexService.FindBestServer(servers);
                
                if (bestServer != null)
                {
                    // Auto-populate server URL (temporarily disable event to prevent incorrect mode setting)
                    txtPlexUrl.TextChanged -= TxtPlexUrl_TextChanged;
                    txtPlexUrl.Text = bestServer.GetUrl();
                    txtPlexUrl.TextChanged += TxtPlexUrl_TextChanged;
                    profile.Plex.Url = bestServer.GetUrl();
                    profile.Plex.IsManualMode = false; // Ensure dropdown is default for fresh auth
                    isManualMode = false; // Update local state to match
                    
                    lblValidationStatus.Text = $"Status: Found server - {bestServer.Name} ({bestServer.Address}:{bestServer.Port})";
                    lblValidationStatus.ForeColor = Color.LightBlue;
                    
                    // Step 3: Load libraries automatically
                    lblValidationStatus.Text = "Status: Loading Plex libraries...";
                    
                    await LoadPlexLibraries();
                    
                    // Step 4: Complete setup
                    lblValidationStatus.Text = "Status: Connected and ready!";
                    lblValidationStatus.ForeColor = Color.LightGreen;
                    
                    // Show server controls after successful authentication
                    ShowServerControls();
                    
                    // Restore server selection after controls are shown
                    RestoreServerSelection();
                    
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

        private void PopulateServerDropdown()
        {
            cmbServerSelection.Items.Clear();
            
            foreach (var server in discoveredServers)
            {
                // Add entry for each available connection address
                var serverName = !string.IsNullOrEmpty(server.Name) ? server.Name : "Unnamed Server";
                
                // Add local addresses if available
                if (!string.IsNullOrWhiteSpace(server.LocalAddresses))
                {
                    var localAddrs = server.LocalAddresses.Split(',');
                    foreach (var addr in localAddrs)
                    {
                        var cleanAddr = addr.Trim();
                        if (!string.IsNullOrEmpty(cleanAddr))
                        {
                            var url = $"http://{cleanAddr}:32400";
                            var displayText = $"{serverName} - Local - {url}";
                            cmbServerSelection.Items.Add(new ServerDisplayItem { Server = server, Url = url, DisplayText = displayText });
                        }
                    }
                }
                
                // Add public/external address
                if (!string.IsNullOrEmpty(server.Address))
                {
                    var url = $"http://{server.Address}:{server.Port}";
                    var displayText = $"{serverName} - Remote - {url}";
                    cmbServerSelection.Items.Add(new ServerDisplayItem { Server = server, Url = url, DisplayText = displayText });
                }
            }
            
            // Set display and value members
            cmbServerSelection.DisplayMember = "DisplayText";
            cmbServerSelection.ValueMember = "Url";
            
            // Don't auto-select first item - let RestoreServerSelection handle selection
        }

        private class ServerDisplayItem
        {
            public PlexServer Server { get; set; }
            public string Url { get; set; }
            public string DisplayText { get; set; }
            
            public override string ToString()
            {
                return DisplayText;
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
                // Get fresh libraries from server
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
                
                // Libraries are unchecked by default - user must manually select desired libraries
                UpdateSelectedLibraries();
                
                // Update profile's last modified time to indicate fresh library data
                profile.LastModified = DateTime.Now;
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
                folderDialog.ShowNewFolderButton = true;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    var selectedPath = folderDialog.SelectedPath;
                    
                    if (IsValidKometaDirectory(selectedPath))
                    {
                        // Valid Kometa directory found
                        txtKometaDirectory.Text = selectedPath;
                        profile.KometaDirectory = selectedPath;
                        ValidatePageInputs(null, null);
                    }
                    else if (IsEmptyDirectory(selectedPath))
                    {
                        // Empty directory - offer to install Kometa here
                        var result = MessageBox.Show(
                            $"The selected directory appears to be empty:\n{selectedPath}\n\n" +
                            "Would you like to use this directory for Kometa installation?\n\n" +
                            "You can install Kometa later from the Final Actions page.",
                            "Empty Directory - Install Kometa Here?",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            txtKometaDirectory.Text = selectedPath;
                            profile.KometaDirectory = selectedPath;
                            ValidatePageInputs(null, null);
                            
                            MessageBox.Show(
                                "Directory set for Kometa installation.\n\n" +
                                "Remember to install Kometa from the Final Actions page before running it.",
                                "Installation Directory Set",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }
                    }
                    else
                    {
                        // Directory has files but no valid Kometa installation
                        var result = MessageBox.Show(
                            "Selected directory does not appear to contain a valid Kometa installation.\n\n" +
                            "The directory contains files but is missing:\n" +
                            "• kometa.py (main script)\n" +
                            "• requirements.txt (dependencies)\n" +
                            "• defaults folder (collection templates)\n\n" +
                            "Would you like to use this directory anyway?\n" +
                            "(You can install/reinstall Kometa from the Final Actions page)",
                            "Invalid Kometa Directory",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning);

                        if (result == DialogResult.Yes)
                        {
                            txtKometaDirectory.Text = selectedPath;
                            profile.KometaDirectory = selectedPath;
                            ValidatePageInputs(null, null);
                        }
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

        private bool IsEmptyDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return false;

            try
            {
                return !Directory.GetFiles(path).Any() && !Directory.GetDirectories(path).Any();
            }
            catch
            {
                return false;
            }
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
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://www.themoviedb.org/settings/api",
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open URL: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ClbLibraries_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            // Use BeginInvoke to ensure the check state is updated before processing
            if (this.IsHandleCreated)
            {
                this.BeginInvoke(new MethodInvoker(() =>
                {
                    UpdateSelectedLibraries();
                    ValidatePageInputs(null, null);
                }));
            }
            else
            {
                // Handle not created yet, use a timer to defer the call
                var timer = new System.Windows.Forms.Timer { Interval = 10 };
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    UpdateSelectedLibraries();
                    ValidatePageInputs(null, null);
                };
                timer.Start();
            }
        }

        private void UpdateSelectedLibraries()
        {
            profile.SelectedLibraries.Clear();
            
            for (int i = 0; i < clbLibraries.Items.Count; i++)
            {
                if (i < profile.Plex.AvailableLibraries.Count)
                {
                    var library = profile.Plex.AvailableLibraries[i];
                    library.IsSelected = clbLibraries.GetItemChecked(i); // Set both true AND false
                    
                    if (library.IsSelected)
                    {
                        profile.SelectedLibraries.Add(library.Name);
                    }
                }
            }
        }

        private void ValidatePageInputs(object sender, EventArgs e)
        {
            bool wasValid = isValidated;
            
            // Allow both valid Kometa directories and empty directories (for installation)
            var directoryValid = !string.IsNullOrWhiteSpace(txtKometaDirectory.Text) &&
                               Directory.Exists(txtKometaDirectory.Text) &&
                               (IsValidKometaDirectory(txtKometaDirectory.Text) || IsEmptyDirectory(txtKometaDirectory.Text));
            
            isValidated = directoryValid &&
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
                txtPlexToken.Text = profile.Plex.Token ?? "";
                
                // Temporarily disable TextChanged event to prevent overriding mode preferences
                txtPlexUrl.TextChanged -= TxtPlexUrl_TextChanged;
                txtPlexUrl.Text = profile.Plex.Url ?? "http://192.168.1.12:32400";
                txtPlexUrl.TextChanged += TxtPlexUrl_TextChanged;
                
                txtTMDbApiKey.Text = profile.TMDb.ApiKey ?? "";
                
                // Load advanced Plex settings
                nudTimeout.Value = profile.Plex.Timeout;
                nudDbCache.Value = profile.Plex.DbCache;
                cbCleanBundles.Checked = profile.Plex.CleanBundles;
                cbEmptyTrash.Checked = profile.Plex.EmptyTrash;
                cbOptimize.Checked = profile.Plex.Optimize;
                cbVerifySSL.Checked = profile.Plex.VerifySSL;
                
                // Load advanced TMDb settings
                nudCacheExpiration.Value = profile.TMDb.CacheExpiration;
                cmbLanguage.SelectedItem = profile.TMDb.Language;
                txtRegion.Text = profile.TMDb.Region ?? "";
                
                // Load cached libraries if available
                if (profile.Plex.AvailableLibraries.Count > 0)
                {
                    LoadCachedLibraries();
                }
                
                if (profile.Plex.IsAuthenticated)
                {
                    if (profile.Plex.AvailableLibraries.Count > 0)
                    {
                        lblValidationStatus.Text = "Status: Previously authenticated with cached libraries";
                        lblValidationStatus.ForeColor = Color.LightGreen;
                    }
                    else
                    {
                        lblValidationStatus.Text = "Status: Previously authenticated - Ready to reconnect";
                        lblValidationStatus.ForeColor = Color.Yellow;
                    }
                    
                    // Load discovered servers from profile
                    discoveredServers = profile.Plex.DiscoveredServers ?? new List<PlexServer>();
                    
                    // Load mode preference from profile - preserve user's actual choice
                    isManualMode = profile.Plex.IsManualMode;
                    
                    // Show server controls for previously authenticated profiles
                    ShowServerControls();
                    
                    // Restore selection after controls are shown
                    RestoreServerSelection();
                }
            }
        }

        private void LoadCachedLibraries()
        {
            clbLibraries.Items.Clear();
            
            foreach (var library in profile.Plex.AvailableLibraries)
            {
                clbLibraries.Items.Add($"{library.Name} ({library.Type})", library.IsSelected);
            }

            btnSelectAll.Enabled = clbLibraries.Items.Count > 0;
            btnUnselectAll.Enabled = clbLibraries.Items.Count > 0;

            // Load selected libraries from profile
            if (profile.SelectedLibraries.Count > 0)
            {
                for (int i = 0; i < clbLibraries.Items.Count; i++)
                {
                    var library = profile.Plex.AvailableLibraries[i];
                    clbLibraries.SetItemChecked(i, library.IsSelected);
                }
            }
            
            ValidatePageInputs(null, null);
        }

        public void SaveProfileData()
        {
            if (profile != null)
            {
                profile.KometaDirectory = txtKometaDirectory.Text;
                profile.Plex.Token = txtPlexToken.Text;
                profile.Plex.Url = txtPlexUrl.Text;
                profile.TMDb.ApiKey = txtTMDbApiKey.Text;
                profile.TMDb.IsAuthenticated = !string.IsNullOrWhiteSpace(txtTMDbApiKey.Text);
                
                // Save advanced Plex settings
                profile.Plex.Timeout = (int)nudTimeout.Value;
                profile.Plex.DbCache = (int)nudDbCache.Value;
                profile.Plex.CleanBundles = cbCleanBundles.Checked;
                profile.Plex.EmptyTrash = cbEmptyTrash.Checked;
                profile.Plex.Optimize = cbOptimize.Checked;
                profile.Plex.VerifySSL = cbVerifySSL.Checked;
                
                // Save advanced TMDb settings
                profile.TMDb.CacheExpiration = (int)nudCacheExpiration.Value;
                profile.TMDb.Language = cmbLanguage.SelectedItem?.ToString() ?? "en";
                profile.TMDb.Region = txtRegion.Text;
                
                UpdateSelectedLibraries();
            }
        }

        private async Task RefreshLibrariesWithNewServer(string serverUrl)
        {
            // Clear existing libraries
            clbLibraries.Items.Clear();
            profile.Plex.AvailableLibraries.Clear();
            
            // Force refresh libraries from the new server
            var libraries = await plexService.GetLibraries(serverUrl, profile.Plex.Token, forceRefresh: true);
            
            if (libraries != null && libraries.Count > 0)
            {
                // Update profile
                profile.Plex.AvailableLibraries = libraries.ToList();
                
                // Repopulate the library checkboxes
                foreach (var library in libraries)
                {
                    var itemText = $"{library.Name} ({library.Type})";
                    var isChecked = profile.Plex.AvailableLibraries.Any(l => l.Name == library.Name && l.Type == library.Type);
                    clbLibraries.Items.Add(itemText, isChecked);
                }
                
                // Update validation
                ValidatePageInputs(null, null);
                
                MessageBox.Show($"Successfully loaded {libraries.Count} libraries from the selected server!", 
                    "Server Changed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                throw new Exception("No libraries found on the selected server or connection failed");
            }
        }

        private void BtnToggleMode_Click(object sender, EventArgs e)
        {
            if (isManualMode)
            {
                // Switch to dropdown mode
                ShowDropdownMode();
                RestoreServerSelection(); // Ensure proper server selection after mode switch
                btnToggleMode.Text = "Enter Manually";
            }
            else
            {
                // Switch to manual mode
                ShowManualMode();
                btnToggleMode.Text = "Choose Server";
            }
            isManualMode = !isManualMode;
            
            // Save mode preference to profile
            if (profile != null)
            {
                profile.Plex.IsManualMode = isManualMode;
            }
        }

        private async void BtnGetLibraries_Click(object sender, EventArgs e)
        {
            try
            {
                btnGetLibraries.Enabled = false;
                btnGetLibraries.Text = "Loading...";
                lblValidationStatus.Text = "Status: Getting libraries...";
                lblValidationStatus.ForeColor = Color.Orange;

                // Get current server URL (from dropdown OR textbox)
                string serverUrl = GetCurrentServerUrl();
                
                if (string.IsNullOrEmpty(serverUrl))
                {
                    lblValidationStatus.Text = "Status: Please select or enter a server URL";
                    lblValidationStatus.ForeColor = Color.Red;
                    return;
                }

                // Refresh libraries with the current server
                await RefreshLibrariesWithNewServer(serverUrl);
                
                lblValidationStatus.Text = "Status: Libraries updated successfully!";
                lblValidationStatus.ForeColor = Color.LightGreen;
            }
            catch (Exception ex)
            {
                lblValidationStatus.Text = "Status: Failed to get libraries";
                lblValidationStatus.ForeColor = Color.Red;
                MessageBox.Show($"Failed to get libraries: {ex.Message}", "Library Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnGetLibraries.Enabled = true;
                btnGetLibraries.Text = "Refresh Libraries";
            }
        }

        private void ShowDropdownMode()
        {
            // Show dropdown, hide textbox
            cmbServerSelection.Visible = true;
            txtPlexUrl.Visible = false;
            
            // Populate dropdown if needed
            if (discoveredServers.Count > 0 && cmbServerSelection.Items.Count == 0)
            {
                PopulateServerDropdown();
            }
        }

        private void ShowManualMode()
        {
            // Show textbox, hide dropdown
            txtPlexUrl.Visible = true;
            cmbServerSelection.Visible = false;
        }

        private void ShowServerControls()
        {
            bool isAuthenticated = profile.Plex.IsAuthenticated && !string.IsNullOrEmpty(profile.Plex.Token);
            
            if (isAuthenticated)
            {
                btnToggleMode.Visible = true;
                btnGetLibraries.Visible = true;
                
                // Show dropdown by default (unless already in manual mode)
                if (!isManualMode)
                {
                    ShowDropdownMode();
                    btnToggleMode.Text = "Enter Manually";
                }
                else
                {
                    ShowManualMode();
                    btnToggleMode.Text = "Choose Server";
                }
            }
            else
            {
                // Hide all server controls when not authenticated
                btnToggleMode.Visible = false;
                btnGetLibraries.Visible = false;
                cmbServerSelection.Visible = false;
                txtPlexUrl.Visible = true; // Show textbox for initial setup
            }
        }

        private string GetCurrentServerUrl()
        {
            if (isManualMode)
            {
                return txtPlexUrl.Text?.Trim();
            }
            else
            {
                if (cmbServerSelection.SelectedItem is ServerDisplayItem selectedItem)
                {
                    return selectedItem.Url;
                }
                return string.Empty;
            }
        }

        private void TxtPlexUrl_TextChanged(object sender, EventArgs e)
        {
            // Auto-save manual server URL entries
            if (profile != null && !string.IsNullOrEmpty(txtPlexUrl.Text))
            {
                profile.Plex.Url = txtPlexUrl.Text;
                profile.Plex.IsManualMode = true; // User is using manual mode
            }
        }

        private void CmbServerSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Auto-save dropdown server selections
            if (profile != null && cmbServerSelection.SelectedItem is ServerDisplayItem selectedItem)
            {
                profile.Plex.Url = selectedItem.Url;
                profile.Plex.IsManualMode = false; // User is using dropdown mode
                
                // Also update the textbox for consistency (but don't trigger its event)
                txtPlexUrl.TextChanged -= TxtPlexUrl_TextChanged;
                txtPlexUrl.Text = selectedItem.Url;
                txtPlexUrl.TextChanged += TxtPlexUrl_TextChanged;
            }
        }

        private void RestoreServerSelection()
        {
            if (profile == null || string.IsNullOrEmpty(profile.Plex.Url))
                return;

            if (isManualMode)
            {
                // Manual mode: just ensure textbox has the saved URL (already loaded in LoadProfileData)
                // txtPlexUrl.Text is already set from LoadProfileData
            }
            else
            {
                // Dropdown mode: find and select the matching server
                for (int i = 0; i < cmbServerSelection.Items.Count; i++)
                {
                    if (cmbServerSelection.Items[i] is ServerDisplayItem item && item.Url == profile.Plex.Url)
                    {
                        // Temporarily remove event handler to avoid triggering save during restoration
                        cmbServerSelection.SelectedIndexChanged -= CmbServerSelection_SelectedIndexChanged;
                        cmbServerSelection.SelectedIndex = i;
                        cmbServerSelection.SelectedIndexChanged += CmbServerSelection_SelectedIndexChanged;
                        break;
                    }
                }
            }
        }


    }
}