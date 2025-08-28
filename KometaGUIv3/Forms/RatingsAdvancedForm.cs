using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using KometaGUIv3.Models;
using KometaGUIv3.Utils;

namespace KometaGUIv3.Forms
{
    public partial class RatingsAdvancedForm : Form
    {
        private KometaProfile profile;
        private OverlayConfiguration overlayConfig;
        private string overlayKey;

        // Main controls
        private CheckBox chkEnableOverlay;
        
        // User Rating controls
        private CheckBox chkUserRating;
        private ComboBox cmbUserSource;
        private Button btnUserFont;
        private Label lblUserFontPath;
        private TrackBar trkUserFontSize;
        private Label lblUserFontSize;
        
        // Critic Rating controls
        private CheckBox chkCriticRating;
        private ComboBox cmbCriticSource;
        private Button btnCriticFont;
        private Label lblCriticFontPath;
        private TrackBar trkCriticFontSize;
        private Label lblCriticFontSize;
        
        // Audience Rating controls
        private CheckBox chkAudienceRating;
        private ComboBox cmbAudienceSource;
        private Button btnAudienceFont;
        private Label lblAudienceFontPath;
        private TrackBar trkAudienceFontSize;
        private Label lblAudienceFontSize;
        
        // Position controls
        private ComboBox cmbPosition;
        
        // Dialog buttons
        private Button btnOK, btnCancel;

        public RatingsAdvancedForm(KometaProfile profile, OverlayConfiguration overlayConfig, string overlayKey)
        {
            this.profile = profile;
            this.overlayConfig = overlayConfig;
            this.overlayKey = overlayKey;
            
            InitializeComponent();
            SetupControls();
            LoadConfiguration();
        }

        private void InitializeComponent()
        {
            this.Text = "Advanced Ratings Configuration";
            this.Size = new Size(600, 550);
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
                Text = "Enable Ratings Overlay",
                Size = new Size(300, 25),
                Location = new Point(20, yPos),
                Font = DarkTheme.GetHeaderFont(),
                ForeColor = DarkTheme.TextColor
            };
            chkEnableOverlay.CheckedChanged += ChkEnableOverlay_CheckedChanged;
            this.Controls.Add(chkEnableOverlay);
            yPos += 40;

            // User Rating Section
            yPos = CreateRatingSection("User Rating (RT)", yPos, 
                out chkUserRating, out cmbUserSource, out btnUserFont, out lblUserFontPath, out trkUserFontSize, out lblUserFontSize,
                new[] { "rt_tomato", "imdb", "tmdb" }, "rt_tomato");

            // Critic Rating Section  
            yPos = CreateRatingSection("Critic Rating (IMDb)", yPos,
                out chkCriticRating, out cmbCriticSource, out btnCriticFont, out lblCriticFontPath, out trkCriticFontSize, out lblCriticFontSize,
                new[] { "rt_tomato", "imdb", "tmdb" }, "imdb");

            // Audience Rating Section
            yPos = CreateRatingSection("Audience Rating (TMDb)", yPos,
                out chkAudienceRating, out cmbAudienceSource, out btnAudienceFont, out lblAudienceFontPath, out trkAudienceFontSize, out lblAudienceFontSize,
                new[] { "rt_tomato", "imdb", "tmdb" }, "tmdb");

            // Horizontal Position
            var lblPosition = new Label
            {
                Text = "Horizontal Position:",
                Size = new Size(150, 25),
                Location = new Point(20, yPos),
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor
            };

            cmbPosition = new ComboBox
            {
                Size = new Size(100, 25),
                Location = new Point(180, yPos),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = DarkTheme.GetDefaultFont(),
                Enabled = false // Initially disabled until main checkbox is enabled
            };
            cmbPosition.Items.AddRange(new[] { "Right", "Left" });
            cmbPosition.SelectedItem = "Right";

            this.Controls.AddRange(new Control[] { lblPosition, cmbPosition });
            yPos += 40;

            // Dialog buttons
            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(100, 35),
                Location = new Point(380, yPos),
                Font = DarkTheme.GetDefaultFont(),
                DialogResult = DialogResult.Cancel
            };

            btnOK = new Button
            {
                Text = "OK",
                Size = new Size(100, 35),
                Location = new Point(490, yPos),
                Font = DarkTheme.GetDefaultFont(),
                DialogResult = DialogResult.OK
            };
            btnOK.Click += BtnOK_Click;

            this.Controls.AddRange(new Control[] { btnCancel, btnOK });
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            // Apply dark theme to all controls
            DarkTheme.ApplyDarkTheme(this);
        }

        private int CreateRatingSection(string title, int yPos,
            out CheckBox checkBox, out ComboBox comboBox, out Button fontButton, out Label fontLabel, out TrackBar fontSizeTrack, out Label fontSizeLabel,
            string[] sourceOptions, string defaultSource)
        {
            // Section checkbox
            checkBox = new CheckBox
            {
                Text = title,
                Size = new Size(200, 25),
                Location = new Point(40, yPos),
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Enabled = false // Initially disabled
            };
            checkBox.CheckedChanged += RatingCheckBox_CheckedChanged;
            this.Controls.Add(checkBox);
            yPos += 30;

            // Source dropdown
            var lblSource = new Label
            {
                Text = "Source:",
                Size = new Size(60, 25),
                Location = new Point(60, yPos),
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor
            };

            comboBox = new ComboBox
            {
                Size = new Size(100, 25),
                Location = new Point(125, yPos),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = DarkTheme.GetDefaultFont(),
                Enabled = false
            };
            comboBox.Items.AddRange(sourceOptions);
            comboBox.SelectedItem = defaultSource;

            this.Controls.AddRange(new Control[] { lblSource, comboBox });
            yPos += 35;

            // Font selection
            var lblFont = new Label
            {
                Text = "Font:",
                Size = new Size(60, 25),
                Location = new Point(60, yPos),
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor
            };

            fontButton = new Button
            {
                Text = "Browse...",
                Size = new Size(80, 25),
                Location = new Point(125, yPos),
                Font = DarkTheme.GetDefaultFont(),
                Enabled = false
            };
            fontButton.Click += FontButton_Click;

            fontLabel = new Label
            {
                Text = "No font selected",
                Size = new Size(200, 25),
                Location = new Point(215, yPos),
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = Color.Gray
            };

            this.Controls.AddRange(new Control[] { lblFont, fontButton, fontLabel });
            yPos += 35;

            // Font size slider
            var lblFontSizeTitle = new Label
            {
                Text = "Font Size:",
                Size = new Size(70, 25),
                Location = new Point(60, yPos),
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor
            };

            fontSizeTrack = new TrackBar
            {
                Size = new Size(200, 45),
                Location = new Point(135, yPos - 5),
                Minimum = 30,
                Maximum = 100,
                Value = 70,
                TickFrequency = 10,
                Enabled = false
            };
            fontSizeTrack.ValueChanged += FontSizeTrack_ValueChanged;

            fontSizeLabel = new Label
            {
                Text = "70",
                Size = new Size(30, 25),
                Location = new Point(345, yPos),
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor
            };

            this.Controls.AddRange(new Control[] { lblFontSizeTitle, fontSizeTrack, fontSizeLabel });
            yPos += 50;

            return yPos;
        }

        private void ChkEnableOverlay_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = chkEnableOverlay.Checked;
            
            // Enable/disable all rating section checkboxes
            chkUserRating.Enabled = enabled;
            chkCriticRating.Enabled = enabled;
            chkAudienceRating.Enabled = enabled;
            
            // Enable/disable position dropdown
            cmbPosition.Enabled = enabled;
            
            // If disabling main checkbox, uncheck and disable all sub-controls
            if (!enabled)
            {
                chkUserRating.Checked = false;
                chkCriticRating.Checked = false;
                chkAudienceRating.Checked = false;
            }
        }

        private void RatingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (sender == chkUserRating)
            {
                bool enabled = chkUserRating.Checked;
                cmbUserSource.Enabled = enabled;
                btnUserFont.Enabled = enabled;
                trkUserFontSize.Enabled = enabled;
            }
            else if (sender == chkCriticRating)
            {
                bool enabled = chkCriticRating.Checked;
                cmbCriticSource.Enabled = enabled;
                btnCriticFont.Enabled = enabled;
                trkCriticFontSize.Enabled = enabled;
            }
            else if (sender == chkAudienceRating)
            {
                bool enabled = chkAudienceRating.Checked;
                cmbAudienceSource.Enabled = enabled;
                btnAudienceFont.Enabled = enabled;
                trkAudienceFontSize.Enabled = enabled;
            }
        }

        private void FontButton_Click(object sender, EventArgs e)
        {
            string fontsPath = Path.Combine(profile.KometaDirectory, "fonts");
            
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Font File";
                openFileDialog.Filter = "TrueType Fonts (*.ttf)|*.ttf|All Files (*.*)|*.*";
                openFileDialog.InitialDirectory = Directory.Exists(fontsPath) ? fontsPath : profile.KometaDirectory;
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedFont = openFileDialog.FileName;
                    string fontName = Path.GetFileName(selectedFont);
                    
                    // Update the appropriate label
                    if (sender == btnUserFont)
                        lblUserFontPath.Text = fontName;
                    else if (sender == btnCriticFont)
                        lblCriticFontPath.Text = fontName;
                    else if (sender == btnAudienceFont)
                        lblAudienceFontPath.Text = fontName;
                }
            }
        }

        private void FontSizeTrack_ValueChanged(object sender, EventArgs e)
        {
            if (sender is TrackBar trackBar)
            {
                // Update the corresponding label
                if (trackBar == trkUserFontSize)
                    lblUserFontSize.Text = trkUserFontSize.Value.ToString();
                else if (trackBar == trkCriticFontSize)
                    lblCriticFontSize.Text = trkCriticFontSize.Value.ToString();
                else if (trackBar == trkAudienceFontSize)
                    lblAudienceFontSize.Text = trkAudienceFontSize.Value.ToString();
            }
        }

        private void LoadConfiguration()
        {
            // Load existing configuration from overlay config
            var ratingConfig = overlayConfig.RatingConfig;
            
            chkEnableOverlay.Checked = ratingConfig.EnableOverlay;
            
            // User Rating
            chkUserRating.Checked = ratingConfig.UserRating.IsEnabled;
            cmbUserSource.SelectedItem = ratingConfig.UserRating.Source;
            trkUserFontSize.Value = Math.Max(30, Math.Min(100, ratingConfig.UserRating.FontSize));
            lblUserFontSize.Text = trkUserFontSize.Value.ToString();
            if (!string.IsNullOrEmpty(ratingConfig.UserRating.CustomFont))
                lblUserFontPath.Text = Path.GetFileName(ratingConfig.UserRating.CustomFont);

            // Critic Rating
            chkCriticRating.Checked = ratingConfig.CriticRating.IsEnabled;
            cmbCriticSource.SelectedItem = ratingConfig.CriticRating.Source;
            trkCriticFontSize.Value = Math.Max(30, Math.Min(100, ratingConfig.CriticRating.FontSize));
            lblCriticFontSize.Text = trkCriticFontSize.Value.ToString();
            if (!string.IsNullOrEmpty(ratingConfig.CriticRating.CustomFont))
                lblCriticFontPath.Text = Path.GetFileName(ratingConfig.CriticRating.CustomFont);

            // Audience Rating  
            chkAudienceRating.Checked = ratingConfig.AudienceRating.IsEnabled;
            cmbAudienceSource.SelectedItem = ratingConfig.AudienceRating.Source;
            trkAudienceFontSize.Value = Math.Max(30, Math.Min(100, ratingConfig.AudienceRating.FontSize));
            lblAudienceFontSize.Text = trkAudienceFontSize.Value.ToString();
            if (!string.IsNullOrEmpty(ratingConfig.AudienceRating.CustomFont))
                lblAudienceFontPath.Text = Path.GetFileName(ratingConfig.AudienceRating.CustomFont);

            // Position
            if (ratingConfig.HorizontalPosition.ToLower() == "left")
                cmbPosition.SelectedItem = "Left";
            else
                cmbPosition.SelectedItem = "Right";
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Save configuration back to overlay config
            var ratingConfig = overlayConfig.RatingConfig;
            
            ratingConfig.EnableOverlay = chkEnableOverlay.Checked;
            
            // User Rating
            ratingConfig.UserRating.IsEnabled = chkUserRating.Checked;
            ratingConfig.UserRating.Source = cmbUserSource.SelectedItem?.ToString() ?? "rt_tomato";
            ratingConfig.UserRating.FontSize = trkUserFontSize.Value;
            if (lblUserFontPath.Text != "No font selected")
                ratingConfig.UserRating.CustomFont = lblUserFontPath.Text;

            // Critic Rating
            ratingConfig.CriticRating.IsEnabled = chkCriticRating.Checked;
            ratingConfig.CriticRating.Source = cmbCriticSource.SelectedItem?.ToString() ?? "imdb";
            ratingConfig.CriticRating.FontSize = trkCriticFontSize.Value;
            if (lblCriticFontPath.Text != "No font selected")
                ratingConfig.CriticRating.CustomFont = lblCriticFontPath.Text;

            // Audience Rating
            ratingConfig.AudienceRating.IsEnabled = chkAudienceRating.Checked;
            ratingConfig.AudienceRating.Source = cmbAudienceSource.SelectedItem?.ToString() ?? "tmdb";
            ratingConfig.AudienceRating.FontSize = trkAudienceFontSize.Value;
            if (lblAudienceFontPath.Text != "No font selected")
                ratingConfig.AudienceRating.CustomFont = lblAudienceFontPath.Text;

            // Position
            ratingConfig.HorizontalPosition = cmbPosition.SelectedItem?.ToString()?.ToLower() ?? "right";
            
            // Update the main overlay enabled state based on whether overlay is enabled
            overlayConfig.IsEnabled = ratingConfig.EnableOverlay;
            
            // Save to profile
            profile.OverlaySettings[overlayKey] = overlayConfig;
        }
    }
}