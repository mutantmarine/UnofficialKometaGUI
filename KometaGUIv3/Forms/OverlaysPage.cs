using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using KometaGUIv3.Models;
using KometaGUIv3.Utils;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using ImageSharpColor = SixLabors.ImageSharp.Color;
using SixLabors.ImageSharp.Formats.Png;

namespace KometaGUIv3.Forms
{
    public partial class OverlaysPage : UserControl
    {
        private KometaProfile profile;
        private ComboBox cmbMediaType, cmbBuilderLevel;
        private Panel imagePreviewPanel;
        private PictureBox picPreview1, picPreview2;
        private GroupBox grpOverlays;
        private Dictionary<string, CheckBox> overlayCheckboxes;
        private Dictionary<string, Button> advancedButtons;
        private string currentMediaType = "Movies";
        private string currentBuilderLevel = "show";
        private const string OVERLAY_IMAGES_PATH = @"Resources\OverlayPreviews\";

        public OverlaysPage(KometaProfile profile)
        {
            this.profile = profile;
            overlayCheckboxes = new Dictionary<string, CheckBox>();
            advancedButtons = new Dictionary<string, Button>();
            InitializeComponent();
            SetupControls();
            LoadProfileData();
        }

        private void SetupControls()
        {
            this.BackColor = DarkTheme.BackgroundColor;
            this.Dock = DockStyle.Fill;
            this.AutoScroll = true;

            // Page title
            var titleLabel = new Label
            {
                Text = "Overlay Configuration",
                Font = DarkTheme.GetTitleFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(500, 40),
                Location = new Point(30, 20)
            };

            var descriptionLabel = new Label
            {
                Text = "Configure visual overlays for your media. Select overlay types and see real-time previews of how they'll look on your posters.",
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(1000, 30),
                Location = new Point(30, 65)
            };

            // Media Type Selection Section
            var lblMediaType = new Label
            {
                Text = "Media Type:",
                Font = DarkTheme.GetHeaderFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(120, 25),
                Location = new Point(30, 120)
            };

            cmbMediaType = new ComboBox
            {
                Size = new Size(180, 30),
                Location = new Point(160, 117),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = DarkTheme.GetDefaultFont()
            };
            cmbMediaType.Items.AddRange(new[] { "Movies", "TV Shows" });
            cmbMediaType.SelectedIndex = 0;
            cmbMediaType.SelectedIndexChanged += CmbMediaType_SelectedIndexChanged;

            // Builder Level Selection (for TV Shows)
            var lblBuilderLevel = new Label
            {
                Text = "Builder Level:",
                Font = DarkTheme.GetHeaderFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(120, 25),
                Location = new Point(380, 120)
            };

            cmbBuilderLevel = new ComboBox
            {
                Size = new Size(180, 30),
                Location = new Point(510, 117),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = DarkTheme.GetDefaultFont()
            };
            cmbBuilderLevel.Items.AddRange(new[] { "show", "season", "episode" });
            cmbBuilderLevel.SelectedIndex = 0;
            cmbBuilderLevel.SelectedIndexChanged += CmbBuilderLevel_SelectedIndexChanged;

            // Image Preview Panel (replaces position map)
            SetupImagePreview();

            // Available Overlays Section
            grpOverlays = new GroupBox
            {
                Text = "Available Overlays",
                Size = new Size(640, 650),
                Location = new Point(30, 180),
                ForeColor = DarkTheme.TextColor,
                Font = DarkTheme.GetHeaderFont()
            };

            var overlayPanel = new Panel
            {
                Size = new Size(620, 610),
                Location = new Point(10, 30),
                AutoScroll = false,
                BackColor = DarkTheme.BackgroundColor
            };

            SetupOverlayCheckboxes(overlayPanel);
            grpOverlays.Controls.Add(overlayPanel);

            this.Controls.AddRange(new Control[] {
                titleLabel, descriptionLabel, lblMediaType, cmbMediaType,
                lblBuilderLevel, cmbBuilderLevel, grpOverlays
            });

            DarkTheme.ApplyDarkTheme(this);
            UpdateAvailableOverlays();
        }

        private void SetupImagePreview()
        {
            var previewTitle = new Label
            {
                Text = "Overlay Preview",
                Font = DarkTheme.GetHeaderFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(200, 25),
                Location = new Point(780, 100),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // For Movies: Two images stacked vertically - immediately under Overlay Preview header
            picPreview1 = new PictureBox
            {
                Size = new Size(400, 250),
                Location = new Point(780, 130),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = DarkTheme.BackgroundColor,
                BorderStyle = BorderStyle.None
            };

            picPreview2 = new PictureBox
            {
                Size = new Size(400, 250),
                Location = new Point(780, 390),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = DarkTheme.BackgroundColor,
                BorderStyle = BorderStyle.None
            };

            // Add controls directly to the main form instead of a container panel
            this.Controls.AddRange(new Control[] { previewTitle, picPreview1, picPreview2 });
            
            // Load initial images
            LoadPreviewImages();
        }

        private void SetupOverlayCheckboxes(Panel parent)
        {
            int y = 15;
            var availableOverlays = GetAvailableOverlaysForCurrentSelection();

            foreach (var overlay in availableOverlays)
            {
                var checkBox = new CheckBox
                {
                    Text = overlay.Value.Name,
                    Tag = overlay.Value,
                    Size = new Size(280, 25),
                    Location = new Point(15, y),
                    ForeColor = DarkTheme.TextColor,
                    Font = DarkTheme.GetDefaultFont(),
                    Name = $"chk_{overlay.Key}"
                };
                checkBox.CheckedChanged += OverlayCheckBox_CheckedChanged;

                var descLabel = new Label
                {
                    Text = overlay.Value.Description,
                    Size = new Size(520, 40),
                    Location = new Point(15, y + 25),
                    ForeColor = Color.LightGray,
                    Font = new Font("Segoe UI", 9F)
                };

                var posLabel = new Label
                {
                    Text = $"Positions: {string.Join(", ", overlay.Value.Positions)}",
                    Size = new Size(200, 20),
                    Location = new Point(320, y + 5),
                    ForeColor = DarkTheme.AccentColor,
                    Font = new Font("Segoe UI", 8F, FontStyle.Bold)
                };

                var advancedButton = new Button
                {
                    Text = "Advanced",
                    Size = new Size(80, 25),
                    Location = new Point(525, y + 2),
                    Font = DarkTheme.GetDefaultFont(),
                    Name = $"btnAdvanced_{overlay.Key}",
                    Tag = overlay.Value,
                    Enabled = false // Initially disabled until overlay is checked
                };
                advancedButton.Click += AdvancedButton_Click;

                parent.Controls.AddRange(new Control[] { checkBox, descLabel, posLabel, advancedButton });
                overlayCheckboxes[overlay.Key] = checkBox;
                advancedButtons[overlay.Key] = advancedButton;
                y += 75;
            }
        }

        private void LoadPreviewImages()
        {
            try
            {
                if (currentMediaType == "Movies")
                {
                    // Show both movie images stacked - use much larger sizes
                    picPreview1.Size = new Size(600, 365);
                    picPreview1.Location = new Point(690, 130);
                    picPreview2.Visible = true;
                    picPreview2.Size = new Size(600, 365);
                    picPreview2.Location = new Point(690, 505);
                    
                    LoadWebPImage(picPreview1, "movie_overlay_preview_1.webp");
                    LoadWebPImage(picPreview2, "movie_overlay_preview_2.webp");
                }
                else // TV Shows
                {
                    // Show single image based on builder level, make it much larger
                    picPreview1.Size = new Size(600, 740);
                    picPreview1.Location = new Point(690, 130); // Use full height for single image
                    picPreview2.Visible = false;
                    
                    string imageName = $"tvshows_{currentBuilderLevel}_overlay_preview.webp";
                    LoadWebPImage(picPreview1, imageName);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading preview images: {ex.Message}");
                // Set placeholder images or text
                SetPlaceholderImage(picPreview1, "Preview not available");
                if (picPreview2.Visible)
                    SetPlaceholderImage(picPreview2, "Preview not available");
            }
        }

        private void LoadWebPImage(PictureBox pictureBox, string fileName)
        {
            try
            {
                // Try multiple possible paths for the images
                var possiblePaths = new[]
                {
                    Path.Combine(Application.StartupPath, OVERLAY_IMAGES_PATH, fileName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, OVERLAY_IMAGES_PATH, fileName),
                    Path.Combine(Directory.GetCurrentDirectory(), OVERLAY_IMAGES_PATH, fileName),
                    Path.Combine(Application.StartupPath, fileName),
                    fileName // In case it's already a full path
                };

                string foundPath = null;
                foreach (var path in possiblePaths)
                {
                    System.Diagnostics.Debug.WriteLine($"Checking path: {path}");
                    if (File.Exists(path))
                    {
                        foundPath = path;
                        System.Diagnostics.Debug.WriteLine($"Found image at: {foundPath}");
                        break;
                    }
                }
                
                if (foundPath != null)
                {
                    // Load WebP image using ImageSharp and convert to PNG for System.Drawing compatibility
                    using (var image = ImageSharpImage.Load(foundPath))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            image.Save(memoryStream, new PngEncoder());
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            pictureBox.Image = System.Drawing.Image.FromStream(memoryStream);
                        }
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Image not found in any location: {fileName}");
                    SetPlaceholderImage(pictureBox, $"Image not found:\n{fileName}\n\nSearched in:\n{Application.StartupPath}\\{OVERLAY_IMAGES_PATH}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading WebP image {fileName}: {ex.Message}");
                SetPlaceholderImage(pictureBox, $"Error loading:\n{fileName}\n\n{ex.Message}");
            }
        }

        private void SetPlaceholderImage(PictureBox pictureBox, string text)
        {
            // Create a placeholder image with text
            var bmp = new Bitmap(pictureBox.Width, pictureBox.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(new SolidBrush(DarkTheme.BackgroundColor), 0, 0, bmp.Width, bmp.Height);
                g.DrawString(text, DarkTheme.GetDefaultFont(), new SolidBrush(DarkTheme.TextColor), 
                    new RectangleF(10, 10, bmp.Width - 20, bmp.Height - 20), 
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
            pictureBox.Image = bmp;
        }


        private Dictionary<string, OverlayInfo> GetAvailableOverlaysForCurrentSelection()
        {
            var availableIds = OverlayDefaults.MediaTypeOverlays[currentMediaType];
            var result = new Dictionary<string, OverlayInfo>();

            foreach (var id in availableIds)
            {
                if (OverlayDefaults.AllOverlays.ContainsKey(id))
                {
                    var overlay = OverlayDefaults.AllOverlays[id];
                    if (overlay.SupportedLevels.Contains(currentBuilderLevel))
                    {
                        result[id] = overlay;
                    }
                }
            }

            return result;
        }

        private void CmbMediaType_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentMediaType = cmbMediaType.SelectedItem.ToString();
            UpdateAvailableOverlays();
            LoadPreviewImages(); // Update preview images when media type changes
        }

        private void CmbBuilderLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentBuilderLevel = cmbBuilderLevel.SelectedItem.ToString();
            UpdateAvailableOverlays();
            
            if (currentMediaType == "TV Shows")
            {
                LoadPreviewImages(); // Update preview image when builder level changes for TV Shows
            }
        }

        private void UpdateAvailableOverlays()
        {
            // Clear existing checkboxes and advanced buttons
            foreach (var checkbox in overlayCheckboxes.Values)
            {
                checkbox.Parent?.Controls.Remove(checkbox);
            }
            foreach (var button in advancedButtons.Values)
            {
                button.Parent?.Controls.Remove(button);
            }
            overlayCheckboxes.Clear();
            advancedButtons.Clear();

            // Find the overlay panel and refresh it
            if (grpOverlays != null)
            {
                var overlayPanel = grpOverlays.Controls.OfType<Panel>().FirstOrDefault();
                if (overlayPanel != null)
                {
                    overlayPanel.Controls.Clear();
                    SetupOverlayCheckboxes(overlayPanel);
                }
            }

            // Show/hide builder level for TV Shows
            var lblBuilderLevel = this.Controls.OfType<Label>().FirstOrDefault(l => l.Text == "Builder Level:");
            if (lblBuilderLevel != null)
            {
                lblBuilderLevel.Visible = currentMediaType == "TV Shows";
                cmbBuilderLevel.Visible = currentMediaType == "TV Shows";
            }
        }


        private void OverlayCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is OverlayInfo overlayInfo)
            {
                var overlayKey = $"{overlayInfo.Id}_{currentMediaType}_{currentBuilderLevel}";
                
                if (checkBox.Checked)
                {
                    var overlayConfig = new OverlayConfiguration
                    {
                        OverlayType = overlayInfo.Id,
                        BuilderLevel = currentBuilderLevel,
                        IsEnabled = true
                    };
                    
                    profile.OverlaySettings[overlayKey] = overlayConfig;
                }
                else
                {
                    profile.OverlaySettings.Remove(overlayKey);
                }

                // Enable/disable the corresponding Advanced button
                if (advancedButtons.ContainsKey(overlayInfo.Id))
                {
                    advancedButtons[overlayInfo.Id].Enabled = checkBox.Checked;
                }
            }
        }

        private void AdvancedButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button && button.Tag is OverlayInfo overlayInfo)
            {
                // Handle different overlay types
                switch (overlayInfo.Id.ToLower())
                {
                    case "ratings":
                        ShowRatingsAdvancedDialog(overlayInfo);
                        break;
                    default:
                        // For now, show a placeholder message for other overlay types
                        MessageBox.Show($"Advanced configuration for '{overlayInfo.Name}' coming soon!", 
                            "Advanced Configuration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                }
            }
        }

        private void ShowRatingsAdvancedDialog(OverlayInfo overlayInfo)
        {
            var overlayKey = $"{overlayInfo.Id}_{currentMediaType}_{currentBuilderLevel}";
            
            // Get or create overlay configuration
            if (!profile.OverlaySettings.ContainsKey(overlayKey))
            {
                profile.OverlaySettings[overlayKey] = new OverlayConfiguration
                {
                    OverlayType = overlayInfo.Id,
                    BuilderLevel = currentBuilderLevel,
                    IsEnabled = false
                };
            }

            var overlayConfig = profile.OverlaySettings[overlayKey];
            
            using (var ratingsForm = new RatingsAdvancedForm(profile, overlayConfig, overlayKey))
            {
                if (ratingsForm.ShowDialog(this) == DialogResult.OK)
                {
                    // Update the main overlay checkbox based on the advanced settings
                    if (overlayCheckboxes.ContainsKey(overlayInfo.Id))
                    {
                        overlayCheckboxes[overlayInfo.Id].Checked = overlayConfig.RatingConfig.EnableOverlay;
                    }
                }
            }
        }


        private void LoadProfileData()
        {
            // Load existing overlay selections from profile
            foreach (var kvp in profile.OverlaySettings)
            {
                var parts = kvp.Key.Split('_');
                if (parts.Length >= 3)
                {
                    var overlayId = parts[0];
                    var mediaType = parts[1];  
                    var builderLevel = string.Join("_", parts.Skip(2));

                    if (overlayCheckboxes.ContainsKey(overlayId) && 
                        mediaType == currentMediaType && 
                        builderLevel == currentBuilderLevel)
                    {
                        overlayCheckboxes[overlayId].Checked = kvp.Value.IsEnabled;
                    }
                }
            }
        }

        public void SaveProfileData()
        {
            // Data is saved in real-time through the CheckedChanged events
        }
    }
}