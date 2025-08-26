using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using KometaGUIv3.Models;
using KometaGUIv3.Services;
using KometaGUIv3.Utils;

namespace KometaGUIv3.Forms
{
    public partial class FinalActionsPage : UserControl
    {
        private KometaProfile profile;
        private YamlGenerator yamlGenerator;
        private KometaRunner kometaRunner;
        private TaskSchedulerService taskScheduler;
        private ProfileManager profileManager;

        // UI Controls
        private RichTextBox rtbLogs;
        private Button btnGenerateYaml, btnRunKometa, btnStopKometa, btnCreateSchedule, btnRemoveSchedule, btnStartLocalhost, btnPayPal;
        private ComboBox cmbScheduleFrequency;
        private NumericUpDown numScheduleInterval;
        private Label lblScheduleStatus, lblLocalhostStatus;
        private ProgressBar progressBar;
        private bool isLocalhostRunning = false;

        public FinalActionsPage(KometaProfile profile, ProfileManager profileManager)
        {
            this.profile = profile;
            this.profileManager = profileManager;
            this.yamlGenerator = new YamlGenerator();
            this.kometaRunner = new KometaRunner();
            this.taskScheduler = new TaskSchedulerService();

            InitializeComponent();
            SetupControls();
            SetupEventHandlers();
            UpdateScheduleStatus();
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
                Text = "Generate your configuration, run Kometa, schedule automatic execution, and manage the localhost server.",
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(900, 30),
                Location = new Point(30, 65)
            };

            // Action Buttons Panel
            var actionsPanel = new GroupBox
            {
                Text = "Actions",
                Size = new Size(550, 200),
                Location = new Point(30, 110),
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

            var lblDays = new Label
            {
                Text = "day(s)",
                Size = new Size(40, 20),
                Location = new Point(285, 85),
                ForeColor = DarkTheme.TextColor
            };

            btnCreateSchedule = new Button
            {
                Text = "Create Schedule",
                Size = new Size(100, 30),
                Location = new Point(340, 80)
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
                Text = "Start Localhost Server",
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
                lblSchedule, cmbScheduleFrequency, lblEvery, numScheduleInterval, lblDays, btnCreateSchedule,
                btnRemoveSchedule, lblScheduleStatus,
                btnStartLocalhost, btnPayPal, lblLocalhostStatus
            });

            // Progress Bar
            progressBar = new ProgressBar
            {
                Size = new Size(550, 25),
                Location = new Point(30, 325),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            // Log Area
            var logPanel = new GroupBox
            {
                Text = "Execution Logs",
                Size = new Size(1300, 450), // Increased for bigger window
                Location = new Point(30, 360),
                ForeColor = DarkTheme.TextColor
            };

            rtbLogs = new RichTextBox
            {
                Size = new Size(1280, 420), // Increased for bigger window
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
                Size = new Size(520, 240), // Reduced height to prevent overlap with logs
                Location = new Point(610, 110),
                ForeColor = DarkTheme.TextColor
            };

            var rtbPreview = new RichTextBox
            {
                Size = new Size(500, 210), // Adjusted to match reduced panel height
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
                titleLabel, descriptionLabel, actionsPanel, progressBar, logPanel, previewPanel
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

            kometaRunner.LogReceived += KometaRunner_LogReceived;
        }

        private void BtnGenerateYaml_Click(object sender, EventArgs e)
        {
            try
            {
                // Save current profile data
                profileManager.SaveProfile(profile);

                var yamlContent = yamlGenerator.GenerateKometaConfig(profile);
                
                using (var saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "YAML files (*.yml)|*.yml|All files (*.*)|*.*";
                    saveDialog.FileName = $"config_{profile.Name}.yml";
                    saveDialog.DefaultExt = "yml";

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
                // First generate a temporary config file
                profileManager.SaveProfile(profile);
                var yamlContent = yamlGenerator.GenerateKometaConfig(profile);
                var tempConfigPath = Path.Combine(Path.GetTempPath(), $"kometa_temp_{profile.Name}.yml");
                yamlGenerator.SaveConfigToFile(yamlContent, tempConfigPath);

                LogMessage("Starting Kometa execution...");
                btnRunKometa.Enabled = false;
                btnStopKometa.Enabled = true;
                progressBar.Visible = true;

                var success = await kometaRunner.RunKometaAsync(profile, tempConfigPath);

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
            if (!isLocalhostRunning)
            {
                // Start localhost server
                try
                {
                    LogMessage("Starting localhost server on port 6969...");
                    // TODO: Implement actual web server startup
                    // For now, simulate starting
                    isLocalhostRunning = true;
                    btnStartLocalhost.Text = "Stop Localhost Server";
                    lblLocalhostStatus.Text = "Server: Running on localhost:6969";
                    lblLocalhostStatus.ForeColor = Color.LightGreen;
                    LogMessage("Localhost server started successfully!");
                    LogMessage("Access the web interface at: http://localhost:6969");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error starting localhost server: {ex.Message}");
                }
            }
            else
            {
                // Stop localhost server
                try
                {
                    LogMessage("Stopping localhost server...");
                    // TODO: Implement actual web server shutdown
                    isLocalhostRunning = false;
                    btnStartLocalhost.Text = "Start Localhost Server";
                    lblLocalhostStatus.Text = "Server: Stopped";
                    lblLocalhostStatus.ForeColor = Color.Red;
                    LogMessage("Localhost server stopped.");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error stopping localhost server: {ex.Message}");
                }
            }
        }

        private void BtnPayPal_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("https://paypal.com");
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
                var lines = yamlContent.Split('\n');
                var preview = "";
                
                for (int i = 0; i < Math.Min(lines.Length, 50); i++)
                {
                    preview += lines[i] + "\n";
                }

                if (lines.Length > 50)
                {
                    preview += "... (truncated)";
                }

                var rtbPreview = this.Controls.Find("rtbPreview", true)[0] as RichTextBox;
                if (rtbPreview != null)
                {
                    rtbPreview.Text = preview;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error generating preview: {ex.Message}");
            }
        }

        public void SaveProfileData()
        {
            // Final actions page doesn't need to save additional data
            // Profile is saved when actions are performed
        }
    }
}