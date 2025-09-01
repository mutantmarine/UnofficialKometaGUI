using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KometaGUIv3.Shared.Models;
using KometaGUIv3.Utils;

namespace KometaGUIv3.Forms
{
    public partial class GenericOverlayAdvancedForm : Form
    {
        private KometaProfile profile;
        private OverlayConfiguration overlayConfig;
        private string overlayKey;
        private string mediaType;
        private OverlayInfo overlayInfo;

        // Main controls
        private CheckBox chkEnableOverlay;
        
        // Builder Level controls (TV Shows only)
        private CheckBox chkShowLevel, chkSeasonLevel, chkEpisodeLevel;
        
        // Dialog buttons
        private Button btnOK, btnCancel;

        public GenericOverlayAdvancedForm(KometaProfile profile, OverlayConfiguration overlayConfig, string overlayKey, string mediaType, OverlayInfo overlayInfo)
        {
            this.profile = profile;
            this.overlayConfig = overlayConfig;
            this.overlayKey = overlayKey;
            this.mediaType = mediaType;
            this.overlayInfo = overlayInfo;
            
            InitializeComponent();
            SetupControls();
            LoadConfiguration();
        }

        private void InitializeComponent()
        {
            this.Text = $"Advanced {overlayInfo.Name} Configuration";
            // Set initial size - will be adjusted dynamically in SetupControls()
            this.Size = new Size(500, 300);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // Apply dark theme
            this.BackColor = DarkTheme.BackgroundColor;
        }

        private void SetupControls()
        {
            int yPos = 20;

            // Main enable/disable checkbox
            chkEnableOverlay = new CheckBox
            {
                Text = $"Enable {overlayInfo.Name} Advanced Variables",
                Size = new Size(400, 25),
                Location = new Point(20, yPos),
                Font = DarkTheme.GetHeaderFont(),
                ForeColor = DarkTheme.TextColor
            };
            chkEnableOverlay.CheckedChanged += ChkEnableOverlay_CheckedChanged;
            this.Controls.Add(chkEnableOverlay);
            yPos += 40;

            // Description
            var descriptionLabel = new Label
            {
                Text = overlayInfo.Description,
                Size = new Size(440, 40),
                Location = new Point(20, yPos),
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = Color.LightGray,
                AutoSize = false
            };
            this.Controls.Add(descriptionLabel);
            yPos += 50;

            // Builder Level Section (TV Shows only)
            if (mediaType == "TV Shows")
            {
                var lblBuilderLevel = new Label
                {
                    Text = "Builder Level:",
                    Size = new Size(150, 25),
                    Location = new Point(20, yPos),
                    Font = DarkTheme.GetDefaultFont(),
                    ForeColor = DarkTheme.TextColor
                };

                this.Controls.Add(lblBuilderLevel);
                yPos += 30;

                // Create checkboxes based on supported levels
                int xPos = 40;
                
                if (overlayInfo.SupportedLevels.Contains("show"))
                {
                    chkShowLevel = new CheckBox
                    {
                        Text = "Show",
                        Size = new Size(70, 25),
                        Location = new Point(xPos, yPos),
                        Font = DarkTheme.GetDefaultFont(),
                        ForeColor = DarkTheme.TextColor,
                        Checked = true, // Default to show level
                        Enabled = false // Initially disabled
                    };
                    this.Controls.Add(chkShowLevel);
                    xPos += 80;
                }

                if (overlayInfo.SupportedLevels.Contains("season"))
                {
                    chkSeasonLevel = new CheckBox
                    {
                        Text = "Season",
                        Size = new Size(80, 25),
                        Location = new Point(xPos, yPos),
                        Font = DarkTheme.GetDefaultFont(),
                        ForeColor = DarkTheme.TextColor,
                        Enabled = false // Initially disabled
                    };
                    this.Controls.Add(chkSeasonLevel);
                    xPos += 90;
                }

                if (overlayInfo.SupportedLevels.Contains("episode"))
                {
                    chkEpisodeLevel = new CheckBox
                    {
                        Text = "Episode",
                        Size = new Size(80, 25),
                        Location = new Point(xPos, yPos),
                        Font = DarkTheme.GetDefaultFont(),
                        ForeColor = DarkTheme.TextColor,
                        Enabled = false // Initially disabled
                    };
                    this.Controls.Add(chkEpisodeLevel);
                }

                yPos += 40;
            }

            // Dialog buttons
            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 35),
                Location = new Point(280, yPos),
                Font = DarkTheme.GetDefaultFont(),
                DialogResult = DialogResult.Cancel
            };

            btnOK = new Button
            {
                Text = "OK",
                Size = new Size(100, 35),
                Location = new Point(390, yPos),
                Font = DarkTheme.GetDefaultFont(),
                DialogResult = DialogResult.OK
            };
            btnOK.Click += BtnOK_Click;

            this.Controls.AddRange(new Control[] { btnCancel, btnOK });
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            // Calculate required form height dynamically based on final button position
            int buttonHeight = 35;
            int bottomPadding = 20; // Space below buttons
            int titleBarAndBorderHeight = 50; // Approximate height for form chrome
            int requiredHeight = yPos + buttonHeight + bottomPadding + titleBarAndBorderHeight;
            
            // Set the form size with calculated height
            this.Size = new Size(500, requiredHeight);

            // Apply dark theme to all controls
            DarkTheme.ApplyDarkTheme(this);
        }

        private void ChkEnableOverlay_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = chkEnableOverlay.Checked;
            
            // Enable/disable builder level checkboxes (TV Shows only)
            if (mediaType == "TV Shows")
            {
                if (chkShowLevel != null) chkShowLevel.Enabled = enabled;
                if (chkSeasonLevel != null) chkSeasonLevel.Enabled = enabled;
                if (chkEpisodeLevel != null) chkEpisodeLevel.Enabled = enabled;
            }
        }

        private void LoadConfiguration()
        {
            // Load existing configuration - use UseAdvancedVariables instead of IsEnabled
            chkEnableOverlay.Checked = overlayConfig.UseAdvancedVariables;
                
            // Builder Level (TV Shows only)
            if (mediaType == "TV Shows")
            {
                // Parse builder level(s) from configuration
                var builderLevel = overlayConfig.BuilderLevel?.ToLower() ?? "show";
                var builderLevels = builderLevel.Split(',').Select(level => level.Trim()).ToArray();
                
                // Set checkboxes based on stored builder levels
                if (chkShowLevel != null) chkShowLevel.Checked = builderLevels.Contains("show");
                if (chkSeasonLevel != null) chkSeasonLevel.Checked = builderLevels.Contains("season");
                if (chkEpisodeLevel != null) chkEpisodeLevel.Checked = builderLevels.Contains("episode");
                
                // Ensure at least one level is selected (default to show if none found) 
                // But only if advanced variables are enabled
                if (overlayConfig.UseAdvancedVariables && 
                    chkShowLevel != null && !chkShowLevel.Checked && 
                    (chkSeasonLevel == null || !chkSeasonLevel.Checked) && 
                    (chkEpisodeLevel == null || !chkEpisodeLevel.Checked))
                {
                    if (chkShowLevel != null) chkShowLevel.Checked = true;
                }
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Clear existing template variables
            overlayConfig.TemplateVariables.Clear();
            
            // Set UseAdvancedVariables flag
            overlayConfig.UseAdvancedVariables = chkEnableOverlay.Checked;
            
            // If advanced variables are enabled, populate template variables
            if (chkEnableOverlay.Checked)
            {
                // For TV Shows - add builder level information
                if (mediaType == "TV Shows")
                {
                    var selectedLevels = new List<string>();
                    
                    if (chkShowLevel != null && chkShowLevel.Checked)
                        selectedLevels.Add("show");
                    if (chkSeasonLevel != null && chkSeasonLevel.Checked)
                        selectedLevels.Add("season");
                    if (chkEpisodeLevel != null && chkEpisodeLevel.Checked)
                        selectedLevels.Add("episode");
                    
                    // Default to show level if none selected
                    if (selectedLevels.Count == 0)
                        selectedLevels.Add("show");
                    
                    overlayConfig.TemplateVariables["builder_levels"] = selectedLevels;
                }
                else
                {
                    // For Movies - use same format as TV Shows for consistency
                    overlayConfig.TemplateVariables["builder_levels"] = new List<string> { "show" };
                }
            }
            
            // Save to profile
            profile.OverlaySettings[overlayKey] = overlayConfig;
        }
    }
}