using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KometaGUIv3.Shared.Models;
using KometaGUIv3.Utils;

namespace KometaGUIv3.Forms
{
    public partial class SettingsPage : UserControl
    {
        private KometaProfile profile;
        private Dictionary<string, Control> settingsControls;

        public SettingsPage(KometaProfile profile)
        {
            this.profile = profile;
            this.settingsControls = new Dictionary<string, Control>();
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
                Text = "Default Settings Configuration",
                Font = DarkTheme.GetTitleFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(500, 40),
                Location = new Point(30, 20)
            };

            var descriptionLabel = new Label
            {
                Text = "Configure default Kometa settings. These settings control caching, sync behavior, asset management, and display options.",
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(900, 30),
                Location = new Point(30, 65)
            };

            // Scrollable panel for all settings
            var scrollPanel = new Panel
            {
                Size = new Size(1340, 730), // Increased for bigger window
                Location = new Point(30, 110),
                AutoScroll = true,
                BackColor = DarkTheme.BackgroundColor
            };

            int yPosition = 10;

            // Core Settings Section
            yPosition = CreateSettingsSection(scrollPanel, "Core Settings", 
                "Essential settings that control Kometa's basic behavior",
                GetCoreSettings(), yPosition);

            yPosition += 30;

            // Cache Settings Section
            yPosition = CreateSettingsSection(scrollPanel, "Cache & Performance", 
                "Settings that affect caching and performance optimization",
                GetCacheSettings(), yPosition);

            yPosition += 30;

            // Asset Settings Section
            yPosition = CreateSettingsSection(scrollPanel, "Asset Management", 
                "Configuration for posters, artwork, and asset handling",
                GetAssetSettings(), yPosition);

            yPosition += 30;

            // Display Settings Section
            yPosition = CreateSettingsSection(scrollPanel, "Display & Reporting", 
                "Control what information is shown in logs and reports",
                GetDisplaySettings(), yPosition);

            yPosition += 30;

            // Advanced Settings Section
            yPosition = CreateSettingsSection(scrollPanel, "Advanced Options", 
                "Advanced configuration options for power users",
                GetAdvancedSettings(), yPosition);

            // Reset to Defaults button
            var resetButton = new Button
            {
                Text = "Reset All to Defaults",
                Size = new Size(150, 35),
                Location = new Point(30, 730),
                BackColor = Color.FromArgb(192, 57, 43), // Red color
                ForeColor = Color.White
            };
            resetButton.Click += ResetButton_Click;

            this.Controls.AddRange(new Control[] { titleLabel, descriptionLabel, scrollPanel, resetButton });
            DarkTheme.ApplyDarkTheme(this);
        }

        private int CreateSettingsSection(Panel parent, string sectionTitle, string sectionDescription, 
            List<SettingDefinition> settings, int startY)
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

            foreach (var setting in settings)
            {
                y = CreateSettingRow(parent, setting, y);
                y += 40; // Spacing between settings
            }

            return y;
        }

        private int CreateSettingRow(Panel parent, SettingDefinition setting, int y)
        {
            // Setting name label
            var nameLabel = new Label
            {
                Text = setting.DisplayName,
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(200, 20),
                Location = new Point(15, y)
            };

            // Setting description
            var descLabel = new Label
            {
                Text = setting.Description,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.Gray,
                Size = new Size(300, 30),
                Location = new Point(15, y + 20)
            };

            Control inputControl = null;

            // Create appropriate input control based on setting type
            switch (setting.Type)
            {
                case SettingType.Boolean:
                    var checkBox = new CheckBox
                    {
                        Checked = (bool)setting.DefaultValue,
                        Size = new Size(15, 15),
                        Location = new Point(230, y + 2),
                        Name = setting.Key
                    };
                    checkBox.CheckedChanged += SettingChanged;
                    inputControl = checkBox;
                    break;

                case SettingType.Integer:
                    var numericUpDown = new NumericUpDown
                    {
                        Value = (decimal)(int)setting.DefaultValue,
                        Minimum = setting.MinValue ?? 0,
                        Maximum = setting.MaxValue ?? 9999,
                        Size = new Size(80, 25),
                        Location = new Point(230, y - 2),
                        Name = setting.Key
                    };
                    numericUpDown.ValueChanged += SettingChanged;
                    inputControl = numericUpDown;
                    break;

                case SettingType.String:
                    var textBox = new TextBox
                    {
                        Text = setting.DefaultValue?.ToString() ?? "",
                        Size = new Size(150, 25),
                        Location = new Point(230, y - 2),
                        Name = setting.Key
                    };
                    textBox.TextChanged += SettingChanged;
                    inputControl = textBox;
                    break;

                case SettingType.Dropdown:
                    var comboBox = new ComboBox
                    {
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Size = new Size(120, 25),
                        Location = new Point(230, y - 2),
                        Name = setting.Key
                    };
                    comboBox.Items.AddRange(setting.Options.ToArray());
                    comboBox.SelectedItem = setting.DefaultValue;
                    comboBox.SelectedIndexChanged += SettingChanged;
                    inputControl = comboBox;
                    break;

                case SettingType.StringList:
                    var listTextBox = new TextBox
                    {
                        Text = string.Join(", ", (List<string>)setting.DefaultValue),
                        Size = new Size(200, 25),
                        Location = new Point(230, y - 2),
                        Name = setting.Key,
                        PlaceholderText = "Comma-separated values"
                    };
                    listTextBox.TextChanged += SettingChanged;
                    inputControl = listTextBox;
                    break;
            }

            if (inputControl != null)
            {
                settingsControls[setting.Key] = inputControl;
                parent.Controls.Add(inputControl);
            }

            // Add help icon for complex settings
            if (!string.IsNullOrEmpty(setting.HelpText))
            {
                var helpLabel = new Label
                {
                    Text = "?",
                    Size = new Size(15, 15),
                    Location = new Point(400, y + 2),
                    BackColor = DarkTheme.AccentColor,
                    ForeColor = Color.White,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold)
                };

                var tooltip = new ToolTip();
                tooltip.SetToolTip(helpLabel, setting.HelpText);
                parent.Controls.Add(helpLabel);
            }

            parent.Controls.AddRange(new Control[] { nameLabel, descLabel });

            return y + 35;
        }

        private void SettingChanged(object sender, EventArgs e)
        {
            if (sender is Control control)
            {
                object value = null;

                switch (control)
                {
                    case CheckBox cb:
                        value = cb.Checked;
                        break;
                    case NumericUpDown nud:
                        value = (int)nud.Value;
                        break;
                    case TextBox tb:
                        if (control.Name.EndsWith("_list"))
                        {
                            value = tb.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim()).ToList();
                        }
                        else
                        {
                            value = tb.Text;
                        }
                        break;
                    case ComboBox cmb:
                        value = cmb.SelectedItem?.ToString();
                        break;
                }

                // Update profile settings using reflection
                var property = typeof(KometaSettings).GetProperty(control.Name);
                if (property != null && value != null)
                {
                    try
                    {
                        var convertedValue = Convert.ChangeType(value, property.PropertyType);
                        property.SetValue(profile.Settings, convertedValue);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating setting {control.Name}: {ex.Message}");
                    }
                }
            }
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to reset all settings to their default values?", 
                "Reset Settings", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                profile.Settings = new KometaSettings(); // Reset to defaults
                LoadProfileData(); // Reload UI
                MessageBox.Show("All settings have been reset to their default values.", 
                    "Settings Reset", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private List<SettingDefinition> GetCoreSettings()
        {
            return new List<SettingDefinition>
            {
                new SettingDefinition("SyncMode", "Sync Mode", "How collections are synchronized", 
                    SettingType.Dropdown, "append", new List<string> { "append", "sync" },
                    "append = add items only, sync = mirror the collection exactly"),
                
                new SettingDefinition("MinimumItems", "Minimum Items", "Minimum items required for a collection", 
                    SettingType.Integer, 1, 0, 100,
                    "Collections with fewer items than this will be skipped"),
                
                new SettingDefinition("DeleteBelowMinimum", "Delete Below Minimum", "Delete collections below minimum items", 
                    SettingType.Boolean, true,
                    "Remove collections that don't meet the minimum item requirement"),
                
                new SettingDefinition("RunAgainDelay", "Run Again Delay", "Hours between runs when run_again is used", 
                    SettingType.Integer, 2, 1, 24,
                    "Delay before running again when using run_again option")
            };
        }

        private List<SettingDefinition> GetCacheSettings()
        {
            return new List<SettingDefinition>
            {
                new SettingDefinition("Cache", "Enable Cache", "Enable caching for faster subsequent runs", 
                    SettingType.Boolean, true,
                    "Caching significantly improves performance on repeat runs"),
                
                new SettingDefinition("CacheExpiration", "Cache Expiration", "Cache expiration time in days", 
                    SettingType.Integer, 60, 1, 365,
                    "How long cached data remains valid before refresh"),
                
                new SettingDefinition("VerifySSL", "Verify SSL", "Verify SSL certificates for HTTPS requests", 
                    SettingType.Boolean, true,
                    "Disable only if you have SSL certificate issues")
            };
        }

        private List<SettingDefinition> GetAssetSettings()
        {
            return new List<SettingDefinition>
            {
                new SettingDefinition("AssetFolders", "Asset Folders", "Use asset folders for organization", 
                    SettingType.Boolean, true,
                    "Organize assets in subfolders by collection name"),
                
                new SettingDefinition("AssetDepth", "Asset Depth", "Asset folder search depth", 
                    SettingType.Integer, 0, 0, 5,
                    "How deep to search in asset folders (0 = only root)"),
                
                new SettingDefinition("CreateAssetFolders", "Create Asset Folders", "Auto-create missing asset folders", 
                    SettingType.Boolean, false,
                    "Automatically create asset folders when they don't exist"),
                
                new SettingDefinition("PrioritizeAssets", "Prioritize Assets", "Prioritize local assets over online", 
                    SettingType.Boolean, false,
                    "Use local assets instead of downloading from online sources"),
                
                new SettingDefinition("DimensionalAssetRename", "Dimensional Asset Rename", "Rename assets with dimensions", 
                    SettingType.Boolean, false,
                    "Add image dimensions to asset filenames"),
                
                new SettingDefinition("DownloadUrlAssets", "Download URL Assets", "Download assets from URLs", 
                    SettingType.Boolean, false,
                    "Download and save assets specified by URL"),
                
                new SettingDefinition("ShowAssetNotNeeded", "Show Asset Not Needed", "Show when assets aren't needed", 
                    SettingType.Boolean, true,
                    "Display messages when assets aren't required for items")
            };
        }

        private List<SettingDefinition> GetDisplaySettings()
        {
            return new List<SettingDefinition>
            {
                new SettingDefinition("ShowUnmanaged", "Show Unmanaged", "Show collections not managed by Kometa", 
                    SettingType.Boolean, true,
                    "Display collections that exist but aren't in your config"),
                
                new SettingDefinition("ShowUnconfigured", "Show Unconfigured", "Show unconfigured collection details", 
                    SettingType.Boolean, true,
                    "Display details about collections not in config"),
                
                new SettingDefinition("ShowMissing", "Show Missing", "Show missing items in collections", 
                    SettingType.Boolean, true,
                    "Display items that should be in collections but aren't found"),
                
                new SettingDefinition("ShowMissingAssets", "Show Missing Assets", "Show missing asset information", 
                    SettingType.Boolean, true,
                    "Display information about missing posters and artwork"),
                
                new SettingDefinition("ShowFiltered", "Show Filtered", "Show filtered items", 
                    SettingType.Boolean, false,
                    "Display items that were filtered out of collections"),
                
                new SettingDefinition("ShowOptions", "Show Options", "Show collection options in logs", 
                    SettingType.Boolean, true,
                    "Display collection configuration options in log output"),
                
                new SettingDefinition("SaveReport", "Save Report", "Save detailed reports to files", 
                    SettingType.Boolean, false,
                    "Save collection reports as files for later review")
            };
        }

        private List<SettingDefinition> GetAdvancedSettings()
        {
            return new List<SettingDefinition>
            {
                new SettingDefinition("DeleteNotScheduled", "Delete Not Scheduled", "Delete collections not scheduled to run", 
                    SettingType.Boolean, false,
                    "Remove collections that aren't scheduled for current run"),
                
                new SettingDefinition("MissingOnlyReleased", "Missing Only Released", "Only show missing items that are released", 
                    SettingType.Boolean, false,
                    "Filter missing items to only include those that have been released"),
                
                new SettingDefinition("OnlyFilterMissing", "Only Filter Missing", "Only apply filters to missing items", 
                    SettingType.Boolean, false,
                    "Apply collection filters only to items not in your library"),
                
                new SettingDefinition("ItemRefreshDelay", "Item Refresh Delay", "Delay between item refreshes (seconds)", 
                    SettingType.Integer, 0, 0, 60,
                    "Delay to prevent overwhelming your server during refreshes"),
                
                new SettingDefinition("PlaylistReport", "Playlist Report", "Generate playlist reports", 
                    SettingType.Boolean, false,
                    "Create detailed reports for playlist operations"),
                
                new SettingDefinition("OverlayArtworkFiletype", "Overlay Artwork Filetype", "File format for overlay images", 
                    SettingType.Dropdown, "webp_lossy", new List<string> { "webp_lossy", "webp_lossless", "jpg", "png" },
                    "Format for generated overlay artwork files"),
                
                new SettingDefinition("OverlayArtworkQuality", "Overlay Artwork Quality", "Quality of overlay images (1-100)", 
                    SettingType.Integer, 90, 1, 100,
                    "Image quality for overlay artwork (higher = better quality, larger files)")
            };
        }

        private void LoadProfileData()
        {
            var settings = profile.Settings;
            
            foreach (var control in settingsControls)
            {
                var property = typeof(KometaSettings).GetProperty(control.Key);
                if (property != null)
                {
                    var value = property.GetValue(settings);
                    
                    switch (control.Value)
                    {
                        case CheckBox cb:
                            cb.Checked = (bool)value;
                            break;
                        case NumericUpDown nud:
                            nud.Value = (decimal)(int)value;
                            break;
                        case TextBox tb:
                            if (value is List<string> list)
                            {
                                tb.Text = string.Join(", ", list);
                            }
                            else
                            {
                                tb.Text = value?.ToString() ?? "";
                            }
                            break;
                        case ComboBox cmb:
                            cmb.SelectedItem = value?.ToString();
                            break;
                    }
                }
            }
        }

        public void SaveProfileData()
        {
            // Data is saved in real-time through the event handlers
        }
    }

    public class SettingDefinition
    {
        public string Key { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public SettingType Type { get; set; }
        public object DefaultValue { get; set; }
        public List<string> Options { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public string HelpText { get; set; }

        public SettingDefinition(string key, string displayName, string description, SettingType type, object defaultValue, string helpText = null)
        {
            Key = key;
            DisplayName = displayName;
            Description = description;
            Type = type;
            DefaultValue = defaultValue;
            HelpText = helpText;
            Options = new List<string>();
        }

        public SettingDefinition(string key, string displayName, string description, SettingType type, object defaultValue, 
            int? minValue, int? maxValue, string helpText = null) : this(key, displayName, description, type, defaultValue, helpText)
        {
            MinValue = minValue;
            MaxValue = maxValue;
        }

        public SettingDefinition(string key, string displayName, string description, SettingType type, object defaultValue, 
            List<string> options, string helpText = null) : this(key, displayName, description, type, defaultValue, helpText)
        {
            Options = options;
        }
    }

    public enum SettingType
    {
        Boolean,
        Integer,
        String,
        Dropdown,
        StringList
    }
}