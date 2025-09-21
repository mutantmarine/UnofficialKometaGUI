using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KometaGUIv3.Shared.Models;
using KometaGUIv3.Utils;

namespace KometaGUIv3.Forms
{
    public partial class CollectionAdvancedForm : Form
    {
        private KometaProfile profile;
        private string collectionKey;
        private string tabName;
        private DefaultCollection collection;

        // Main controls
        private CheckBox chkEnableAdvanced;
        
        // Media Type controls (not shown for "ONLY" tabs)
        private GroupBox grpMediaType;
        private RadioButton rbMovies, rbTVShows, rbBoth;
        
        // Template Variables sections with individual enable checkboxes
        private GroupBox grpDataRange;
        private CheckBox chkEnableDataRange;
        private Label lblStarting, lblEnding;
        private ComboBox cmbStarting, cmbEnding;
        
        private GroupBox grpStyling;
        private CheckBox chkEnableStyling;
        private Label lblSepStyle;
        private ComboBox cmbSepStyle;
        
        private GroupBox grpVisibility;
        private CheckBox chkEnableVisibility;
        private CheckBox chkVisibleLibraryTop, chkVisibleHomeTop, chkVisibleSharedTop;
        
        private GroupBox grpContentFiltering;
        private CheckBox chkEnableContentFiltering;
        private CheckBox chkOriginalsOnly;
        private Label lblDepth, lblLimit;
        private NumericUpDown numDepth, numLimit;
        
        // Dialog buttons
        private Button btnOK, btnCancel;

        public CollectionAdvancedForm(KometaProfile profile, string collectionKey, string tabName, DefaultCollection collection)
        {
            this.profile = profile;
            this.collectionKey = collectionKey;
            this.tabName = tabName;
            this.collection = collection;
            
            InitializeForm();
            SetupControls();
            LoadConfiguration();
        }

        private void InitializeForm()
        {
            InitializeComponent();
            
            // Set appropriate title based on collection type
            var collectionTypeLabel = collectionKey.StartsWith("plex_") ? "Plex Collection" : "Default Collection";
            this.Text = $"Advanced Configuration - {collection.Name} ({collectionTypeLabel})";
            
            this.Size = new Size(500, 600);
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
            chkEnableAdvanced = new CheckBox
            {
                Text = $"Enable Advanced Variables for {collection.Name}",
                Size = new Size(450, 25),
                Location = new Point(20, yPos),
                Font = DarkTheme.GetHeaderFont(),
                ForeColor = DarkTheme.TextColor
            };
            chkEnableAdvanced.CheckedChanged += ChkEnableAdvanced_CheckedChanged;
            this.Controls.Add(chkEnableAdvanced);
            yPos += 40;

            // Media Type Selection (only for non-"ONLY" tabs)
            if (!IsOnlyTab())
            {
                SetupMediaTypeControls(ref yPos);
            }

            // Template Variables Sections
            SetupDataRangeControls(ref yPos);
            SetupStylingControls(ref yPos);
            SetupVisibilityControls(ref yPos);
            SetupContentFilteringControls(ref yPos);

            // Dialog buttons
            SetupDialogButtons(yPos);
            
            // Apply dark theme to get proper styling
            DarkTheme.ApplyDarkTheme(this);
        }

        private bool IsOnlyTab()
        {
            return tabName == "Movies Only" || tabName == "TV Shows Only";
        }

        private void SetupMediaTypeControls(ref int yPos)
        {
            grpMediaType = new GroupBox
            {
                Text = "Media Type",
                Size = new Size(450, 80),
                Location = new Point(20, yPos),
                ForeColor = DarkTheme.TextColor
            };

            rbMovies = new RadioButton
            {
                Text = "Movies",
                Size = new Size(80, 20),
                Location = new Point(20, 25),
                ForeColor = DarkTheme.TextColor
            };

            rbTVShows = new RadioButton
            {
                Text = "TV Shows",
                Size = new Size(80, 20),
                Location = new Point(120, 25),
                ForeColor = DarkTheme.TextColor
            };

            rbBoth = new RadioButton
            {
                Text = "Both",
                Size = new Size(80, 20),
                Location = new Point(220, 25),
                ForeColor = DarkTheme.TextColor,
                Checked = true // Default selection
            };

            grpMediaType.Controls.AddRange(new Control[] { rbMovies, rbTVShows, rbBoth });
            this.Controls.Add(grpMediaType);
            yPos += 90;
        }

        private void SetupDataRangeControls(ref int yPos)
        {
            grpDataRange = new GroupBox
            {
                Text = "Data Range",
                Size = new Size(450, 100),
                Location = new Point(20, yPos),
                ForeColor = DarkTheme.TextColor
            };

            chkEnableDataRange = new CheckBox
            {
                Text = "Enable Data Range Configuration",
                Size = new Size(400, 20),
                Location = new Point(10, 20),
                ForeColor = DarkTheme.TextColor
            };
            chkEnableDataRange.CheckedChanged += (s, e) => UpdateDataRangeControls();

            lblStarting = new Label
            {
                Text = "Starting:",
                Size = new Size(60, 20),
                Location = new Point(20, 50),
                ForeColor = DarkTheme.TextColor
            };

            cmbStarting = new ComboBox
            {
                Size = new Size(120, 25),
                Location = new Point(85, 47),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStarting.Items.AddRange(new string[] { "latest-10", "latest-5", "latest-3", "latest", "current" });
            cmbStarting.SelectedIndex = 0;

            lblEnding = new Label
            {
                Text = "Ending:",
                Size = new Size(60, 20),
                Location = new Point(220, 50),
                ForeColor = DarkTheme.TextColor
            };

            cmbEnding = new ComboBox
            {
                Size = new Size(120, 25),
                Location = new Point(280, 47),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbEnding.Items.AddRange(new string[] { "latest", "current", "latest-1", "latest-2", "latest-3" });
            cmbEnding.SelectedIndex = 0;

            grpDataRange.Controls.AddRange(new Control[] { chkEnableDataRange, lblStarting, cmbStarting, lblEnding, cmbEnding });
            this.Controls.Add(grpDataRange);
            yPos += 110;
        }

        private void SetupStylingControls(ref int yPos)
        {
            grpStyling = new GroupBox
            {
                Text = "Styling",
                Size = new Size(450, 80),
                Location = new Point(20, yPos),
                ForeColor = DarkTheme.TextColor
            };

            chkEnableStyling = new CheckBox
            {
                Text = "Enable Styling Configuration",
                Size = new Size(400, 20),
                Location = new Point(10, 20),
                ForeColor = DarkTheme.TextColor
            };
            chkEnableStyling.CheckedChanged += (s, e) => UpdateStylingControls();

            lblSepStyle = new Label
            {
                Text = "Separator Style:",
                Size = new Size(100, 20),
                Location = new Point(20, 50),
                ForeColor = DarkTheme.TextColor
            };

            cmbSepStyle = new ComboBox
            {
                Size = new Size(120, 25),
                Location = new Point(125, 47),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbSepStyle.Items.AddRange(new string[] { "purple", "plum", "blue", "green", "red", "orange", "gray" });
            cmbSepStyle.SelectedIndex = 0;

            grpStyling.Controls.AddRange(new Control[] { chkEnableStyling, lblSepStyle, cmbSepStyle });
            this.Controls.Add(grpStyling);
            yPos += 90;
        }

        private void SetupVisibilityControls(ref int yPos)
        {
            grpVisibility = new GroupBox
            {
                Text = "Visibility",
                Size = new Size(450, 100),
                Location = new Point(20, yPos),
                ForeColor = DarkTheme.TextColor
            };

            chkEnableVisibility = new CheckBox
            {
                Text = "Enable Visibility Configuration",
                Size = new Size(400, 20),
                Location = new Point(10, 20),
                ForeColor = DarkTheme.TextColor
            };
            chkEnableVisibility.CheckedChanged += (s, e) => UpdateVisibilityControls();

            chkVisibleLibraryTop = new CheckBox
            {
                Text = "Visible Library Top",
                Size = new Size(140, 20),
                Location = new Point(20, 50),
                ForeColor = DarkTheme.TextColor
            };

            chkVisibleHomeTop = new CheckBox
            {
                Text = "Visible Home Top",
                Size = new Size(140, 20),
                Location = new Point(170, 50),
                ForeColor = DarkTheme.TextColor
            };

            chkVisibleSharedTop = new CheckBox
            {
                Text = "Visible Shared Top",
                Size = new Size(140, 20),
                Location = new Point(320, 50),
                ForeColor = DarkTheme.TextColor
            };

            grpVisibility.Controls.AddRange(new Control[] { chkEnableVisibility, chkVisibleLibraryTop, chkVisibleHomeTop, chkVisibleSharedTop });
            this.Controls.Add(grpVisibility);
            yPos += 110;
        }

        private void SetupContentFilteringControls(ref int yPos)
        {
            grpContentFiltering = new GroupBox
            {
                Text = "Content Filtering",
                Size = new Size(450, 100),
                Location = new Point(20, yPos),
                ForeColor = DarkTheme.TextColor
            };

            chkEnableContentFiltering = new CheckBox
            {
                Text = "Enable Content Filtering",
                Size = new Size(400, 20),
                Location = new Point(10, 20),
                ForeColor = DarkTheme.TextColor
            };
            chkEnableContentFiltering.CheckedChanged += (s, e) => UpdateContentFilteringControls();

            chkOriginalsOnly = new CheckBox
            {
                Text = "Originals Only",
                Size = new Size(100, 20),
                Location = new Point(20, 50),
                ForeColor = DarkTheme.TextColor
            };

            lblDepth = new Label
            {
                Text = "Depth:",
                Size = new Size(40, 20),
                Location = new Point(140, 50),
                ForeColor = DarkTheme.TextColor
            };

            numDepth = new NumericUpDown
            {
                Size = new Size(60, 25),
                Location = new Point(185, 47),
                Minimum = 1,
                Maximum = 10,
                Value = 1
            };

            lblLimit = new Label
            {
                Text = "Limit:",
                Size = new Size(40, 20),
                Location = new Point(260, 50),
                ForeColor = DarkTheme.TextColor
            };

            numLimit = new NumericUpDown
            {
                Size = new Size(80, 25),
                Location = new Point(305, 47),
                Minimum = 1,
                Maximum = 1000,
                Value = 50
            };

            grpContentFiltering.Controls.AddRange(new Control[] { chkEnableContentFiltering, chkOriginalsOnly, lblDepth, numDepth, lblLimit, numLimit });
            this.Controls.Add(grpContentFiltering);
            yPos += 110;
        }

        private void SetupDialogButtons(int yPos)
        {
            btnOK = new Button
            {
                Text = "OK",
                Size = new Size(80, 30),
                Location = new Point(290, yPos + 20),
                DialogResult = DialogResult.OK,
                Name = "btnPrimary"
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(80, 30),
                Location = new Point(380, yPos + 20),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] { btnOK, btnCancel });
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            // Adjust form height based on final position
            this.Size = new Size(this.Size.Width, yPos + 80);
        }

        private void ChkEnableAdvanced_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = chkEnableAdvanced.Checked;

            // Enable/disable all individual section checkboxes
            chkEnableDataRange.Enabled = enabled;
            chkEnableStyling.Enabled = enabled;
            chkEnableVisibility.Enabled = enabled;
            chkEnableContentFiltering.Enabled = enabled;

            // Enable/disable media type controls (if present)
            if (grpMediaType != null)
            {
                rbMovies.Enabled = enabled;
                rbTVShows.Enabled = enabled;
                rbBoth.Enabled = enabled;
            }

            // Update individual sections
            UpdateDataRangeControls();
            UpdateStylingControls();
            UpdateVisibilityControls();
            UpdateContentFilteringControls();
        }

        private void UpdateDataRangeControls()
        {
            bool enabled = chkEnableAdvanced.Checked && chkEnableDataRange.Checked;
            lblStarting.Enabled = enabled;
            cmbStarting.Enabled = enabled;
            lblEnding.Enabled = enabled;
            cmbEnding.Enabled = enabled;
        }

        private void UpdateStylingControls()
        {
            bool enabled = chkEnableAdvanced.Checked && chkEnableStyling.Checked;
            lblSepStyle.Enabled = enabled;
            cmbSepStyle.Enabled = enabled;
        }

        private void UpdateVisibilityControls()
        {
            bool enabled = chkEnableAdvanced.Checked && chkEnableVisibility.Checked;
            chkVisibleLibraryTop.Enabled = enabled;
            chkVisibleHomeTop.Enabled = enabled;
            chkVisibleSharedTop.Enabled = enabled;
        }

        private void UpdateContentFilteringControls()
        {
            bool enabled = chkEnableAdvanced.Checked && chkEnableContentFiltering.Checked;
            chkOriginalsOnly.Enabled = enabled;
            lblDepth.Enabled = enabled;
            numDepth.Enabled = enabled;
            lblLimit.Enabled = enabled;
            numLimit.Enabled = enabled;
        }

        private void LoadConfiguration()
        {
            // Determine which storage dictionary to use based on collection type
            var isPlexCollection = collectionKey.StartsWith("plex_");
            var storageDict = isPlexCollection ? profile.MyCollectionAdvancedSettings : profile.CollectionAdvancedSettings;
            
            if (storageDict.ContainsKey(collectionKey))
            {
                var config = storageDict[collectionKey];
                
                // Load configuration values
                chkEnableAdvanced.Checked = config.IsEnabled;
                
                // Load media type selection
                if (rbMovies != null && rbTVShows != null && rbBoth != null)
                {
                    switch (config.MediaType)
                    {
                        case "Movies":
                            rbMovies.Checked = true;
                            break;
                        case "TV Shows":
                            rbTVShows.Checked = true;
                            break;
                        default:
                            rbBoth.Checked = true;
                            break;
                    }
                }
                
                // Load template variable settings
                chkEnableDataRange.Checked = config.DataRangeEnabled;
                if (!string.IsNullOrEmpty(config.StartingValue))
                    cmbStarting.SelectedItem = config.StartingValue;
                if (!string.IsNullOrEmpty(config.EndingValue))
                    cmbEnding.SelectedItem = config.EndingValue;
                
                chkEnableStyling.Checked = config.StylingEnabled;
                if (!string.IsNullOrEmpty(config.SepStyle))
                    cmbSepStyle.SelectedItem = config.SepStyle;
                
                chkEnableVisibility.Checked = config.VisibilityEnabled;
                chkVisibleLibraryTop.Checked = config.VisibleLibraryTop;
                chkVisibleHomeTop.Checked = config.VisibleHomeTop;
                chkVisibleSharedTop.Checked = config.VisibleSharedTop;
                
                chkEnableContentFiltering.Checked = config.ContentFilteringEnabled;
                chkOriginalsOnly.Checked = config.OriginalsOnly;
                numDepth.Value = config.Depth;
                numLimit.Value = config.Limit;
            }
            else
            {
                // Set defaults for new configurations
                chkEnableAdvanced.Checked = false;
                
                // Default media type to "Both" if available
                if (rbBoth != null)
                    rbBoth.Checked = true;
            }
            
            // Update UI state based on loaded configuration
            ChkEnableAdvanced_CheckedChanged(null, EventArgs.Empty);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Save configuration to appropriate profile dictionary
            var config = GetConfiguration();
            var isPlexCollection = collectionKey.StartsWith("plex_");
            var storageDict = isPlexCollection ? profile.MyCollectionAdvancedSettings : profile.CollectionAdvancedSettings;
            
            storageDict[collectionKey] = config;
        }

        public CollectionAdvancedConfiguration GetConfiguration()
        {
            var config = new CollectionAdvancedConfiguration
            {
                IsEnabled = chkEnableAdvanced.Checked,
                MediaType = GetSelectedMediaType(),
                DataRangeEnabled = chkEnableDataRange.Checked,
                StartingValue = cmbStarting.SelectedItem?.ToString(),
                EndingValue = cmbEnding.SelectedItem?.ToString(),
                StylingEnabled = chkEnableStyling.Checked,
                SepStyle = cmbSepStyle.SelectedItem?.ToString(),
                VisibilityEnabled = chkEnableVisibility.Checked,
                VisibleLibraryTop = chkVisibleLibraryTop.Checked,
                VisibleHomeTop = chkVisibleHomeTop.Checked,
                VisibleSharedTop = chkVisibleSharedTop.Checked,
                ContentFilteringEnabled = chkEnableContentFiltering.Checked,
                OriginalsOnly = chkOriginalsOnly.Checked,
                Depth = (int)numDepth.Value,
                Limit = (int)numLimit.Value
            };

            return config;
        }

        private string GetSelectedMediaType()
        {
            if (rbMovies?.Checked == true) return "Movies";
            if (rbTVShows?.Checked == true) return "TV Shows";
            if (rbBoth?.Checked == true) return "Both";
            return "Both"; // Default
        }
    }

}