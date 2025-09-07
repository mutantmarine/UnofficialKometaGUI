using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using KometaGUIv3.Services;
using KometaGUIv3.Utils;
using KometaGUIv3.Shared.Models;
using KometaGUIv3.Shared.Services;

namespace KometaGUIv3.Forms
{
    public partial class FinalActionsPage : UserControl
    {
        private KometaProfile profile;
        private YamlGenerator yamlGenerator;
        private KometaRunner kometaRunner;
        private TaskSchedulerService taskScheduler;
        private ProfileManager profileManager;
        private KometaInstaller kometaInstaller;
        private SystemRequirements systemRequirements;

        // UI Controls
        private RichTextBox rtbLogs;
        private Button btnGenerateYaml, btnRunKometa, btnStopKometa, btnCreateSchedule, btnRemoveSchedule, btnStartLocalhost, btnPayPal;
        private Button btnInstallKometa, btnCheckInstallation, btnUpdateKometa;
        private ComboBox cmbScheduleFrequency;
        private NumericUpDown numScheduleInterval;
        private Label lblScheduleStatus, lblLocalhostStatus, lblInstallationStatus, lblTimeUnit;
        private ProgressBar progressBar, installationProgressBar;

        public FinalActionsPage(KometaProfile profile, ProfileManager profileManager)
        {
            this.profile = profile;
            this.profileManager = profileManager;
            this.yamlGenerator = new YamlGenerator();
            this.kometaRunner = new KometaRunner();
            this.taskScheduler = new TaskSchedulerService();
            this.kometaInstaller = new KometaInstaller();
            this.systemRequirements = new SystemRequirements();

            InitializeComponent();
            SetupControls();
            SetupEventHandlers();
            UpdateScheduleStatus();
            
            // Defer the async call to avoid constructor issues
            Task.Run(async () =>
            {
                try
                {
                    await CheckInstallationStatusAsync();
                }
                catch (Exception ex)
                {
                    // Log the error but don't crash the constructor
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => LogMessage($"Error during initial installation check: {ex.Message}")));
                    }
                    else
                    {
                        LogMessage($"Error during initial installation check: {ex.Message}");
                    }
                }
            });
        }

        private void SetupControls()
        {
            this.BackColor = DarkTheme.BackgroundColor;
            this.Dock = DockStyle.Fill;
            this.AutoScroll = true; // Enable scrolling for the entire page

            // Page title
            var titleLabel = new Label
            {
                Text = "Final Actions",
                Font = DarkTheme.GetTitleFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(300, 40),
                Location = new Point(30, 20)
            };

            var descriptionLabel = new Label
            {
                Text = "Generate your configuration, run Kometa, and schedule automatic execution.",
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(900, 30),
                Location = new Point(30, 65)
            };

            // Installation Status Panel
            var installationPanel = new GroupBox
            {
                Text = "Kometa Installation",
                Size = new Size(1100, 140),
                Location = new Point(30, 110),
                ForeColor = DarkTheme.TextColor
            };

            lblInstallationStatus = new Label
            {
                Text = "Checking installation status...",
                Size = new Size(500, 60),
                Location = new Point(20, 30),
                ForeColor = Color.Orange,
                Font = DarkTheme.GetDefaultFont()
            };

            btnCheckInstallation = new Button
            {
                Text = "Check Status",
                Size = new Size(100, 30),
                Location = new Point(530, 30)
            };

            btnInstallKometa = new Button
            {
                Text = "Download Prerequisites",
                Size = new Size(160, 30),
                Location = new Point(640, 30),
                Name = "btnPrimary"
            };

            btnUpdateKometa = new Button
            {
                Text = "Update Kometa",
                Size = new Size(120, 30),
                Location = new Point(810, 30),
                Enabled = false
            };

            installationProgressBar = new ProgressBar
            {
                Size = new Size(860, 20),
                Location = new Point(20, 70),
                Visible = false,
                Style = ProgressBarStyle.Continuous
            };

            var installProgressLabel = new Label
            {
                Text = "",
                Size = new Size(860, 20),
                Location = new Point(20, 95),
                ForeColor = DarkTheme.TextColor,
                Name = "installProgressLabel"
            };

            installationPanel.Controls.AddRange(new Control[] {
                lblInstallationStatus, btnCheckInstallation, btnInstallKometa, btnUpdateKometa,
                installationProgressBar, installProgressLabel
            });

            // Action Buttons Panel
            var actionsPanel = new GroupBox
            {
                Text = "Actions",
                Size = new Size(550, 200),
                Location = new Point(30, 260),
                ForeColor = DarkTheme.TextColor
            };

            // Row 1: Config and Kometa
            btnGenerateYaml = new Button
            {
                Text = "Generate YAML Config",
                Size = new Size(150, 40),
                Location = new Point(20, 30),
                Name = "btnPrimary"
            };

            btnRunKometa = new Button
            {
                Text = "Run Kometa",
                Size = new Size(120, 40),
                Location = new Point(190, 30),
                Name = "btnPrimary"
            };

            btnStopKometa = new Button
            {
                Text = "Stop Kometa",
                Size = new Size(120, 40),
                Location = new Point(330, 30),
                Enabled = false
            };

            // Row 2: Scheduling
            var lblSchedule = new Label
            {
                Text = "Schedule:",
                Size = new Size(60, 20),
                Location = new Point(20, 85),
                ForeColor = DarkTheme.TextColor
            };

            cmbScheduleFrequency = new ComboBox
            {
                Size = new Size(80, 25),
                Location = new Point(85, 82),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbScheduleFrequency.Items.AddRange(new[] { "Daily", "Weekly", "Monthly" });
            cmbScheduleFrequency.SelectedIndex = 0;
            cmbScheduleFrequency.SelectedIndexChanged += CmbScheduleFrequency_SelectedIndexChanged;

            var lblEvery = new Label
            {
                Text = "Every:",
                Size = new Size(45, 20),
                Location = new Point(175, 85),
                ForeColor = DarkTheme.TextColor
            };

            numScheduleInterval = new NumericUpDown
            {
                Size = new Size(50, 25),
                Location = new Point(225, 82),
                Minimum = 1,
                Maximum = 30,
                Value = 1
            };

            lblTimeUnit = new Label
            {
                Text = "day(s)",
                Size = new Size(65, 20),
                Location = new Point(285, 85),
                ForeColor = DarkTheme.TextColor
            };

            btnCreateSchedule = new Button
            {
                Text = "Create Schedule",
                Size = new Size(100, 30),
                Location = new Point(385, 80)
            };

            btnRemoveSchedule = new Button
            {
                Text = "Remove Schedule",
                Size = new Size(110, 30),
                Location = new Point(20, 120)
            };

            lblScheduleStatus = new Label
            {
                Text = "Status: No scheduled task",
                Size = new Size(200, 20),
                Location = new Point(145, 125),
                ForeColor = Color.Orange
            };

            // Row 3: Other Actions
            btnStartLocalhost = new Button
            {
                Text = "Enable Local Server",
                Size = new Size(150, 40),
                Location = new Point(20, 155),
                Name = "btnPrimary"
            };

            btnPayPal = new Button
            {
                Text = "Support via PayPal",
                Size = new Size(130, 40),
                Location = new Point(190, 155)
            };

            lblLocalhostStatus = new Label
            {
                Text = "Server: Stopped",
                Size = new Size(150, 20),
                Location = new Point(340, 170),
                ForeColor = Color.Red
            };

            actionsPanel.Controls.AddRange(new Control[] {
                btnGenerateYaml, btnRunKometa, btnStopKometa,
                lblSchedule, cmbScheduleFrequency, lblEvery, numScheduleInterval, lblTimeUnit, btnCreateSchedule,
                btnRemoveSchedule, lblScheduleStatus,
                btnStartLocalhost, btnPayPal, lblLocalhostStatus
            });

            // Progress Bar
            progressBar = new ProgressBar
            {
                Size = new Size(550, 25),
                Location = new Point(30, 475),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            // Log Area
            var logPanel = new GroupBox
            {
                Text = "Execution Logs",
                Size = new Size(1300, 350), // Reduced to make room for installation panel
                Location = new Point(30, 510),
                ForeColor = DarkTheme.TextColor
            };

            rtbLogs = new RichTextBox
            {
                Size = new Size(1280, 320), // Adjusted for reduced log panel size
                Location = new Point(10, 25),
                BackColor = DarkTheme.InputBackColor,
                ForeColor = DarkTheme.TextColor,
                Font = new Font("Consolas", 9F),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };

            logPanel.Controls.Add(rtbLogs);

            // Config Preview (side panel)
            var previewPanel = new GroupBox
            {
                Text = "Configuration Preview",
                Size = new Size(520, 200), // Reduced height to fit with installation panel
                Location = new Point(610, 260),
                ForeColor = DarkTheme.TextColor
            };

            var rtbPreview = new RichTextBox
            {
                Size = new Size(500, 170), // Adjusted to match reduced panel height
                Location = new Point(10, 25),
                BackColor = DarkTheme.InputBackColor,
                ForeColor = DarkTheme.TextColor,
                Font = new Font("Consolas", 8F),
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Both,
                Name = "rtbPreview"
            };

            previewPanel.Controls.Add(rtbPreview);

            this.Controls.AddRange(new Control[] {
                titleLabel, descriptionLabel, installationPanel, actionsPanel, progressBar, logPanel, previewPanel
            });

            DarkTheme.ApplyDarkTheme(this);
            GenerateConfigPreview();
        }

        private void SetupEventHandlers()
        {
            btnGenerateYaml.Click += BtnGenerateYaml_Click;
            btnRunKometa.Click += BtnRunKometa_Click;
            btnStopKometa.Click += BtnStopKometa_Click;
            btnCreateSchedule.Click += BtnCreateSchedule_Click;
            btnRemoveSchedule.Click += BtnRemoveSchedule_Click;
            btnStartLocalhost.Click += BtnStartLocalhost_Click;
            btnPayPal.Click += BtnPayPal_Click;
            
            btnCheckInstallation.Click += BtnCheckInstallation_Click;
            btnInstallKometa.Click += BtnInstallKometa_Click;
            btnUpdateKometa.Click += BtnUpdateKometa_Click;

            kometaRunner.LogReceived += KometaRunner_LogReceived;
            kometaInstaller.LogReceived += KometaInstaller_LogReceived;
            kometaInstaller.ProgressChanged += KometaInstaller_ProgressChanged;
            systemRequirements.LogReceived += SystemRequirements_LogReceived;
        }

        private void BtnGenerateYaml_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate that Kometa directory is set
                if (string.IsNullOrWhiteSpace(profile.KometaDirectory))
                {
                    MessageBox.Show("Please set a Kometa directory in the Connections page before generating the config.", 
                        "Kometa Directory Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Save current profile data
                profileManager.SaveProfile(profile);

                var yamlContent = yamlGenerator.GenerateKometaConfig(profile);
                
                // Create the config file path (Kometa directory + config subfolder)
                var configDirectory = Path.Combine(profile.KometaDirectory, "config");
                var defaultConfigPath = Path.Combine(configDirectory, "config.yml");
                
                // Ensure the config directory exists
                Directory.CreateDirectory(configDirectory);
                
                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "YAML files (*.yml)|*.yml|All files (*.*)|*.*";
                    saveDialog.FileName = "config.yml";
                    saveDialog.DefaultExt = "yml";
                    saveDialog.InitialDirectory = configDirectory;
                    saveDialog.Title = "Save Kometa Configuration";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        yamlGenerator.SaveConfigToFile(yamlContent, saveDialog.FileName);
                        LogMessage($"Configuration saved to: {saveDialog.FileName}");
                        
                        MessageBox.Show($"Configuration successfully generated and saved to:\n{saveDialog.FileName}", 
                            "Configuration Generated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating configuration: {ex.Message}");
                MessageBox.Show($"Error generating configuration: {ex.Message}", 
                    "Generation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnRunKometa_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate that Kometa directory is set
                if (string.IsNullOrWhiteSpace(profile.KometaDirectory))
                {
                    MessageBox.Show("Please set a Kometa directory in the Connections page before running Kometa.", 
                        "Kometa Directory Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Generate and save the config file to the designated Kometa directory
                profileManager.SaveProfile(profile);
                var yamlContent = yamlGenerator.GenerateKometaConfig(profile);
                
                // Create the config file path (Kometa directory + config subfolder)
                var configDirectory = Path.Combine(profile.KometaDirectory, "config");
                var configPath = Path.Combine(configDirectory, "config.yml");
                
                // Ensure the config directory exists
                Directory.CreateDirectory(configDirectory);
                
                // Save the config file
                yamlGenerator.SaveConfigToFile(yamlContent, configPath);
                LogMessage($"Configuration generated and saved to: {configPath}");

                LogMessage("Starting Kometa execution...");
                btnRunKometa.Enabled = false;
                btnStopKometa.Enabled = true;
                progressBar.Visible = true;

                var success = await kometaRunner.RunKometaAsync(profile, configPath);

                if (success)
                {
                    LogMessage("Kometa execution completed successfully!");
                }
                else
                {
                    LogMessage("Kometa execution completed with errors.");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error running Kometa: {ex.Message}");
            }
            finally
            {
                btnRunKometa.Enabled = true;
                btnStopKometa.Enabled = false;
                progressBar.Visible = false;
            }
        }

        private void BtnStopKometa_Click(object sender, EventArgs e)
        {
            kometaRunner.StopKometa();
            btnRunKometa.Enabled = true;
            btnStopKometa.Enabled = false;
            progressBar.Visible = false;
        }

        private void BtnCreateSchedule_Click(object sender, EventArgs e)
        {
            try
            {
                // Generate and save config file
                profileManager.SaveProfile(profile);
                var yamlContent = yamlGenerator.GenerateKometaConfig(profile);
                var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
                    "Kometa", $"config_{profile.Name}.yml");

                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                yamlGenerator.SaveConfigToFile(yamlContent, configPath);

                var frequency = (ScheduleFrequency)cmbScheduleFrequency.SelectedIndex;
                var interval = (int)numScheduleInterval.Value;

                var success = taskScheduler.CreateScheduledTask(profile, configPath, frequency, interval);

                if (success)
                {
                    LogMessage($"Scheduled task created successfully for profile '{profile.Name}'");
                    UpdateScheduleStatus();
                    MessageBox.Show("Scheduled task created successfully!", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogMessage("Failed to create scheduled task");
                    MessageBox.Show("Failed to create scheduled task. Please check permissions.", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error creating scheduled task: {ex.Message}");
                MessageBox.Show($"Error creating scheduled task: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRemoveSchedule_Click(object sender, EventArgs e)
        {
            try
            {
                var success = taskScheduler.DeleteScheduledTask(profile.Name);

                if (success)
                {
                    LogMessage($"Scheduled task removed for profile '{profile.Name}'");
                    UpdateScheduleStatus();
                    MessageBox.Show("Scheduled task removed successfully!", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogMessage("Failed to remove scheduled task or task doesn't exist");
                    MessageBox.Show("Failed to remove scheduled task or task doesn't exist.", 
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error removing scheduled task: {ex.Message}");
                MessageBox.Show($"Error removing scheduled task: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStartLocalhost_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This feature has not been implemented yet.", 
                "Feature Not Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnPayPal_Click(object sender, EventArgs e)
        {
            try
            {
                var donationUrl = "https://www.paypal.com/donate/?business=WBHFP3TMYUHS8&amount=5&no_recurring=1&item_name=Thank+you+for+trying+my+program.+It+took+many+hours+and+late+nights+to+get+it+up+and+running.+Your+donations+are+appreciated%21%C2%A4cy_code=USD";
                
                var psi = new ProcessStartInfo
                {
                    FileName = donationUrl,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
                LogMessage("Opening PayPal donation page...");
            }
            catch (Exception ex)
            {
                LogMessage($"Error opening PayPal: {ex.Message}");
            }
        }

        private void KometaRunner_LogReceived(object sender, string logMessage)
        {
            if (rtbLogs.InvokeRequired)
            {
                rtbLogs.Invoke(new Action(() => LogMessage(logMessage)));
            }
            else
            {
                LogMessage(logMessage);
            }
        }

        private void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            rtbLogs.AppendText($"[{timestamp}] {message}\n");
            rtbLogs.ScrollToCaret();
        }

        private void UpdateScheduleStatus()
        {
            if (taskScheduler.TaskExists(profile.Name))
            {
                lblScheduleStatus.Text = "Status: Task scheduled";
                lblScheduleStatus.ForeColor = Color.LightGreen;
                btnRemoveSchedule.Enabled = true;
            }
            else
            {
                lblScheduleStatus.Text = "Status: No scheduled task";
                lblScheduleStatus.ForeColor = Color.Orange;
                btnRemoveSchedule.Enabled = false;
            }
        }

        private void GenerateConfigPreview()
        {
            try
            {
                var yamlContent = yamlGenerator.GenerateKometaConfig(profile);
                
                var rtbPreview = this.Controls.Find("rtbPreview", true)[0] as RichTextBox;
                if (rtbPreview != null)
                {
                    rtbPreview.Text = yamlContent;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating preview: {ex.Message}");
            }
        }

        private async void BtnCheckInstallation_Click(object sender, EventArgs e)
        {
            await CheckInstallationStatusAsync();
        }

        private async void BtnInstallKometa_Click(object sender, EventArgs e)
        {
            try
            {
                LogMessage("Install Kometa button clicked");
                
                if (profile == null)
                {
                    MessageBox.Show("Profile not initialized. Please restart the application.", 
                        "Profile Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(profile.KometaDirectory))
                {
                    MessageBox.Show("Please set a Kometa directory in the Connections page before installing.", 
                        "Kometa Directory Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Check if this is an existing installation and provide appropriate prompt
                var installStatus = await kometaInstaller.CheckInstallationStatusAsync(profile.KometaDirectory);
                DialogResult result;
                bool forceReinstall = false;

                if (installStatus.IsKometaInstalled)
                {
                    // Existing installation detected - offer update/reinstall options
                    var existingVersionText = !string.IsNullOrEmpty(installStatus.KometaVersion) 
                        ? $" (Version: {installStatus.KometaVersion})" 
                        : "";
                    
                    result = MessageBox.Show(
                        $"An existing Kometa installation was found in:\n{profile.KometaDirectory}{existingVersionText}\n\n" +
                        "Would you like to:\n" +
                        "• YES - Update/refresh the existing installation\n" +
                        "• NO - Cancel installation\n\n" +
                        "To completely reinstall from scratch, hold SHIFT and click Install again.",
                        "Existing Kometa Installation Found",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                }
                else if (Directory.Exists(profile.KometaDirectory) && 
                        (Directory.GetFiles(profile.KometaDirectory).Length > 0 || Directory.GetDirectories(profile.KometaDirectory).Length > 0))
                {
                    // Directory has content but no Kometa installation
                    result = MessageBox.Show(
                        $"The selected directory is not empty:\n{profile.KometaDirectory}\n\n" +
                        "Kometa will be installed alongside existing files.\n\n" +
                        "The installation process includes:\n" +
                        "• Installing Python (if not found)\n" +
                        "• Installing Git (if not found)\n" +
                        "• Cloning the Kometa repository\n" +
                        "• Creating a virtual environment\n" +
                        "• Installing Python dependencies\n\n" +
                        "Continue with installation?",
                        "Install Kometa in Existing Directory",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                }
                else
                {
                    // Standard installation prompt for empty/new directories
                    result = MessageBox.Show(
                        $"This will install Kometa and all its dependencies to:\n{profile.KometaDirectory}\n\n" +
                        "The installation process includes:\n" +
                        "• Installing Python (if not found)\n" +
                        "• Installing Git (if not found)\n" +
                        "• Cloning the Kometa repository\n" +
                        "• Creating a virtual environment\n" +
                        "• Installing Python dependencies\n\n" +
                        "This process may take several minutes and requires internet access.\n\n" +
                        "Continue with installation?",
                        "Confirm Kometa Installation",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                }

                // Check if SHIFT key is held for force reinstall
                if (result == DialogResult.Yes && Control.ModifierKeys == Keys.Shift && installStatus.IsKometaInstalled)
                {
                    var forceResult = MessageBox.Show(
                        "SHIFT key detected. This will completely remove the existing installation and reinstall from scratch.\n\n" +
                        "⚠️ WARNING: All existing Kometa data, configurations, and customizations will be lost!\n\n" +
                        "Are you sure you want to proceed with a complete reinstall?",
                        "Force Reinstall Confirmation",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);
                    
                    if (forceResult == DialogResult.Yes)
                    {
                        forceReinstall = true;
                        LogMessage("User confirmed force reinstall (SHIFT+Click detected)");
                    }
                    else
                    {
                        result = DialogResult.No; // Cancel if user doesn't confirm force reinstall
                    }
                }

                if (result == DialogResult.Yes)
                {
                    LogMessage("User confirmed Kometa installation");
                    LogMessage("Starting Kometa installation...");
                    
                    // Disable controls safely
                    SafeUpdateUI(() =>
                    {
                        if (btnInstallKometa != null && !btnInstallKometa.IsDisposed) btnInstallKometa.Enabled = false;
                        if (btnUpdateKometa != null && !btnUpdateKometa.IsDisposed) btnUpdateKometa.Enabled = false;
                        if (btnRunKometa != null && !btnRunKometa.IsDisposed) btnRunKometa.Enabled = false;
                        if (installationProgressBar != null && !installationProgressBar.IsDisposed)
                        {
                            installationProgressBar.Visible = true;
                            installationProgressBar.Value = 0;
                        }
                    });

                    if (kometaInstaller == null)
                    {
                        LogMessage("Error: KometaInstaller is not initialized");
                        MessageBox.Show("Installation service not available. Please restart the application.", 
                            "Service Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var success = await kometaInstaller.InstallKometaAsync(profile.KometaDirectory, forceReinstall);

                    if (success)
                    {
                        LogMessage("Kometa installation completed successfully!");
                        MessageBox.Show("Kometa has been installed successfully!", "Installation Complete", 
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await CheckInstallationStatusAsync();
                    }
                    else
                    {
                        LogMessage("Kometa installation failed. Please check the logs for details.");
                        MessageBox.Show("Kometa installation failed. Please check the logs for details.", 
                            "Installation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    SafeUpdateUI(() =>
                    {
                        if (installationProgressBar != null && !installationProgressBar.IsDisposed)
                            installationProgressBar.Visible = false;
                        if (btnInstallKometa != null && !btnInstallKometa.IsDisposed)
                            btnInstallKometa.Enabled = true;
                    });
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Installation error: {ex.Message}");
                MessageBox.Show($"Installation error: {ex.Message}", "Installation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                SafeUpdateUI(() =>
                {
                    if (installationProgressBar != null && !installationProgressBar.IsDisposed)
                        installationProgressBar.Visible = false;
                    if (btnInstallKometa != null && !btnInstallKometa.IsDisposed)
                        btnInstallKometa.Enabled = true;
                    if (btnUpdateKometa != null && !btnUpdateKometa.IsDisposed)
                        btnUpdateKometa.Enabled = false;
                    if (btnRunKometa != null && !btnRunKometa.IsDisposed)
                        btnRunKometa.Enabled = false;
                });
            }
        }

        private async void BtnUpdateKometa_Click(object sender, EventArgs e)
        {
            try
            {
                LogMessage("Starting Kometa update...");
                btnUpdateKometa.Enabled = false;
                btnRunKometa.Enabled = false;

                var success = await kometaInstaller.UpdateKometaAsync(profile.KometaDirectory);

                if (success)
                {
                    LogMessage("Kometa updated successfully!");
                    MessageBox.Show("Kometa has been updated successfully!", "Update Complete", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await CheckInstallationStatusAsync();
                }
                else
                {
                    LogMessage("Kometa update failed. Please check the logs for details.");
                    MessageBox.Show("Kometa update failed. Please check the logs for details.", 
                        "Update Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Update error: {ex.Message}");
                MessageBox.Show($"Update error: {ex.Message}", "Update Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnUpdateKometa.Enabled = true;
                await CheckInstallationStatusAsync();
            }
        }

        private async Task CheckInstallationStatusAsync()
        {
            try
            {
                if (profile == null || string.IsNullOrWhiteSpace(profile.KometaDirectory))
                {
                    SafeUpdateUI(() =>
                    {
                        if (lblInstallationStatus != null && !lblInstallationStatus.IsDisposed)
                        {
                            lblInstallationStatus.Text = "No Kometa directory set.\nPlease set a directory in the Connections page.";
                            lblInstallationStatus.ForeColor = Color.Red;
                        }
                        if (btnInstallKometa != null && !btnInstallKometa.IsDisposed) btnInstallKometa.Enabled = true;
                        if (btnUpdateKometa != null && !btnUpdateKometa.IsDisposed) btnUpdateKometa.Enabled = false;
                        if (btnRunKometa != null && !btnRunKometa.IsDisposed) btnRunKometa.Enabled = false;
                    });
                    return;
                }

                SafeUpdateUI(() =>
                {
                    if (lblInstallationStatus != null && !lblInstallationStatus.IsDisposed)
                    {
                        lblInstallationStatus.Text = "Checking installation status...";
                        lblInstallationStatus.ForeColor = Color.Orange;
                    }
                });
                
                // Check system requirements first
                var systemCheck = await systemRequirements.CheckSystemRequirementsAsync();
                
                // Check Kometa installation
                var installStatus = await kometaInstaller.CheckInstallationStatusAsync(profile.KometaDirectory);

                var statusText = "";
                var allGood = true;

                // Python status
                if (systemCheck.IsPythonInstalled)
                {
                    statusText += $"✓ Python {systemCheck.PythonVersion}\n";
                }
                else
                {
                    statusText += "✗ Python not installed\n";
                    allGood = false;
                }

                // Git status
                if (systemCheck.IsGitInstalled)
                {
                    statusText += $"✓ Git {systemCheck.GitVersion}\n";
                }
                else
                {
                    statusText += "✗ Git not installed\n";
                    allGood = false;
                }

                // Kometa status
                if (installStatus.IsKometaInstalled)
                {
                    statusText += $"✓ Kometa {installStatus.KometaVersion}\n";
                }
                else
                {
                    statusText += "✗ Kometa not installed\n";
                    allGood = false;
                }

                // Virtual Environment status
                if (installStatus.IsVirtualEnvironmentReady)
                {
                    statusText += "✓ Virtual environment ready\n";
                }
                else if (installStatus.IsKometaInstalled)
                {
                    statusText += "✗ Virtual environment not ready\n";
                    allGood = false;
                }

                // Dependencies status
                if (installStatus.AreDependenciesInstalled)
                {
                    statusText += "✓ Dependencies installed";
                }
                else if (installStatus.IsVirtualEnvironmentReady)
                {
                    statusText += "✗ Dependencies not installed";
                    allGood = false;
                }

                SafeUpdateUI(() =>
                {
                    if (lblInstallationStatus != null && !lblInstallationStatus.IsDisposed)
                    {
                        lblInstallationStatus.Text = statusText;

                        if (allGood)
                        {
                            lblInstallationStatus.ForeColor = Color.LightGreen;
                        }
                        else
                        {
                            lblInstallationStatus.ForeColor = Color.Orange;
                        }

                        if (!string.IsNullOrEmpty(installStatus.ErrorMessage))
                        {
                            lblInstallationStatus.Text += $"\n\nError: {installStatus.ErrorMessage}";
                            lblInstallationStatus.ForeColor = Color.Red;
                        }
                    }

                    if (btnInstallKometa != null && !btnInstallKometa.IsDisposed)
                        btnInstallKometa.Enabled = true;
                    
                    if (btnUpdateKometa != null && !btnUpdateKometa.IsDisposed)
                        btnUpdateKometa.Enabled = allGood || installStatus.IsKometaInstalled;
                    
                    if (btnRunKometa != null && !btnRunKometa.IsDisposed)
                        btnRunKometa.Enabled = allGood;
                });
            }
            catch (Exception ex)
            {
                SafeUpdateUI(() =>
                {
                    if (lblInstallationStatus != null && !lblInstallationStatus.IsDisposed)
                    {
                        lblInstallationStatus.Text = $"Error checking installation: {ex.Message}";
                        lblInstallationStatus.ForeColor = Color.Red;
                    }
                });
                LogMessage($"Error checking installation: {ex.Message}");
            }
        }

        private void KometaInstaller_LogReceived(object sender, string logMessage)
        {
            if (rtbLogs.InvokeRequired)
            {
                rtbLogs.Invoke(new Action(() => LogMessage(logMessage)));
            }
            else
            {
                LogMessage(logMessage);
            }
        }

        private void KometaInstaller_ProgressChanged(object sender, int progress)
        {
            if (installationProgressBar.InvokeRequired)
            {
                installationProgressBar.Invoke(new Action(() => 
                {
                    installationProgressBar.Value = progress;
                    var progressLabel = this.Controls.Find("installProgressLabel", true)[0] as Label;
                    if (progressLabel != null)
                    {
                        progressLabel.Text = $"Installation Progress: {progress}%";
                    }
                }));
            }
            else
            {
                installationProgressBar.Value = progress;
                var progressLabel = this.Controls.Find("installProgressLabel", true)[0] as Label;
                if (progressLabel != null)
                {
                    progressLabel.Text = $"Installation Progress: {progress}%";
                }
            }
        }

        private void SystemRequirements_LogReceived(object sender, string logMessage)
        {
            if (rtbLogs.InvokeRequired)
            {
                rtbLogs.Invoke(new Action(() => LogMessage(logMessage)));
            }
            else
            {
                LogMessage(logMessage);
            }
        }

        private void SafeUpdateUI(Action updateAction)
        {
            try
            {
                if (this.IsDisposed || this.Disposing)
                    return;

                if (this.InvokeRequired)
                {
                    this.BeginInvoke(updateAction);
                }
                else
                {
                    updateAction();
                }
            }
            catch (ObjectDisposedException)
            {
                // Control was disposed, ignore
            }
            catch (InvalidOperationException)
            {
                // Handle was not created or control is disposing
            }
        }

        private void CmbScheduleFrequency_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cmbScheduleFrequency.SelectedIndex)
            {
                case 0: // Daily
                    lblTimeUnit.Text = "day(s)";
                    break;
                case 1: // Weekly
                    lblTimeUnit.Text = "week(s)";
                    break;
                case 2: // Monthly
                    lblTimeUnit.Text = "month(s)";
                    break;
                default:
                    lblTimeUnit.Text = "day(s)";
                    break;
            }
        }


        public void SaveProfileData()
        {
            // Final actions page doesn't need to save additional data
            // Profile is saved when actions are performed
        }
    }
}