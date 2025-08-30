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
        private TabControl tabControl;
        private Dictionary<string, CheckBox> overlayCheckboxes;
        private Dictionary<string, Button> advancedButtons;
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

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Configure overlays for your media libraries",
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(600, 20),
                Location = new Point(30, 55)
            };

            // Tab control for Movies and TV Shows (with preview images inside each tab)
            SetupTabControl();

            this.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });
        }


        private void SetupTabControl()
        {
            tabControl = new TabControl
            {
                Size = new Size(1200, 700),  // Much wider to accommodate preview images
                Location = new Point(30, 90),  // Moved up since no global preview panel
                BackColor = DarkTheme.BackgroundColor,
                ForeColor = DarkTheme.TextColor,
                Font = DarkTheme.GetDefaultFont()
            };

            // Movies tab
            var moviesTab = new TabPage("Movies")
            {
                BackColor = DarkTheme.BackgroundColor,
                AutoScroll = true
            };

            // TV Shows tab
            var tvShowsTab = new TabPage("TV Shows")
            {
                BackColor = DarkTheme.BackgroundColor,
                AutoScroll = true
            };

            tabControl.TabPages.Add(moviesTab);
            tabControl.TabPages.Add(tvShowsTab);

            // Setup overlays and preview images for each tab
            SetupOverlaysForTab(moviesTab, "Movies");
            SetupOverlaysForTab(tvShowsTab, "TV Shows");

            this.Controls.Add(tabControl);
        }

        private void SetupOverlaysForTab(TabPage tabPage, string mediaType)
        {
            // Add preview images to the right side of the tab
            SetupPreviewImagesInTab(tabPage, mediaType);

            var availableOverlays = OverlayDefaults.MediaTypeOverlays[mediaType];
            int yPos = 20;

            foreach (var overlayId in availableOverlays)
            {
                if (OverlayDefaults.AllOverlays.ContainsKey(overlayId))
                {
                    var overlayInfo = OverlayDefaults.AllOverlays[overlayId];
                    var overlayKey = $"{overlayId}_{mediaType.Replace(" ", "_")}";

                    // Main checkbox
                    var checkbox = new CheckBox
                    {
                        Text = overlayInfo.Name,
                        Size = new Size(200, 25),
                        Location = new Point(20, yPos),
                        ForeColor = DarkTheme.TextColor,
                        Font = DarkTheme.GetDefaultFont(),
                        Name = $"chk_{overlayKey}"
                    };
                    checkbox.CheckedChanged += OverlayCheckBox_CheckedChanged;

                    // Description
                    var descLabel = new Label
                    {
                        Text = overlayInfo.Description,
                        Size = new Size(300, 40),
                        Location = new Point(20, yPos + 25),
                        ForeColor = Color.LightGray,
                        Font = new Font("Segoe UI", 9F)
                    };

                    // Positions
                    var posLabel = new Label
                    {
                        Text = $"Positions: {string.Join(", ", overlayInfo.Positions)}",
                        Size = new Size(200, 20),
                        Location = new Point(350, yPos + 5),
                        ForeColor = DarkTheme.AccentColor,
                        Font = new Font("Segoe UI", 8F, FontStyle.Bold)
                    };

                    // Advanced button (only for overlays that need it)
                    Button advancedButton = null;
                    if (ShouldHaveAdvancedButton(overlayInfo, mediaType))
                    {
                        Console.WriteLine($"[DEBUG] Creating advanced button for: {overlayKey}");
                        advancedButton = new Button
                        {
                            Text = "Advanced",
                            Size = new Size(80, 25),
                            Location = new Point(570, yPos + 2),
                            Font = DarkTheme.GetDefaultFont(),
                            Name = $"btnAdvanced_{overlayKey}",
                            Enabled = false
                        };
                        advancedButton.Click += AdvancedButton_Click;
                        advancedButtons[overlayKey] = advancedButton;
                        Console.WriteLine($"[DEBUG] Advanced button created and added to dictionary for: {overlayKey}");
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] No advanced button for: {overlayKey}");
                    }

                    // Add controls to tab
                    var controlsToAdd = advancedButton != null
                        ? new Control[] { checkbox, descLabel, posLabel, advancedButton }
                        : new Control[] { checkbox, descLabel, posLabel };
                    
                    tabPage.Controls.AddRange(controlsToAdd);
                    overlayCheckboxes[overlayKey] = checkbox;

                    yPos += 75;
                }
            }
        }

        private void SetupPreviewImagesInTab(TabPage tabPage, string mediaType)
        {
            // Preview images panel positioned on the right side of the tab
            var previewPanel = new Panel
            {
                Size = new Size(600, 320),
                Location = new Point(580, 20), // Right side of tab
                BackColor = DarkTheme.BackgroundColor,
                BorderStyle = BorderStyle.FixedSingle
            };

            var previewTitle = new Label
            {
                Text = $"Overlay Preview - {mediaType}",
                Font = DarkTheme.GetHeaderFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(250, 25),
                Location = new Point(10, 10)
            };

            // Three preview images
            var picPreview1 = new PictureBox
            {
                Size = new Size(150, 225),
                Location = new Point(30, 50),
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                Name = $"picPreview1_{mediaType.Replace(" ", "_")}"
            };

            var picPreview2 = new PictureBox
            {
                Size = new Size(150, 225),
                Location = new Point(200, 50),
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                Name = $"picPreview2_{mediaType.Replace(" ", "_")}"
            };

            var picPreview3 = new PictureBox
            {
                Size = new Size(150, 225),
                Location = new Point(370, 50),
                BackColor = Color.Black,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                Name = $"picPreview3_{mediaType.Replace(" ", "_")}"
            };

            previewPanel.Controls.AddRange(new Control[] { previewTitle, picPreview1, picPreview2, picPreview3 });
            tabPage.Controls.Add(previewPanel);

            // Load preview images for this media type
            LoadPreviewImagesInTab(picPreview1, picPreview2, picPreview3, mediaType);
        }

        private void LoadPreviewImagesInTab(PictureBox pic1, PictureBox pic2, PictureBox pic3, string mediaType)
        {
            try
            {
                string suffix = mediaType == "Movies" ? "movie" : "show";
                
                LoadImageIntoPictureBox(pic1, $"overlay_preview_{suffix}_1.png");
                LoadImageIntoPictureBox(pic2, $"overlay_preview_{suffix}_2.png");
                LoadImageIntoPictureBox(pic3, $"overlay_preview_{suffix}_3.png");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading preview images for {mediaType}: {ex.Message}");
            }
        }

        private bool ShouldHaveAdvancedButton(OverlayInfo overlayInfo, string mediaType)
        {
            // Debug logging
            Console.WriteLine($"[DEBUG] ShouldHaveAdvancedButton: {overlayInfo.Id} for {mediaType}");
            Console.WriteLine($"[DEBUG]   SupportedLevels: [{string.Join(", ", overlayInfo.SupportedLevels)}]");
            Console.WriteLine($"[DEBUG]   SupportedLevels.Length: {overlayInfo.SupportedLevels.Length}");
            
            // Ratings always gets advanced button
            if (overlayInfo.Id == "ratings")
            {
                Console.WriteLine($"[DEBUG]   -> TRUE (ratings always gets button)");
                return true;
            }

            // For TV Shows, multi-level overlays get advanced buttons
            if (mediaType == "TV Shows" && overlayInfo.SupportedLevels.Length > 1)
            {
                Console.WriteLine($"[DEBUG]   -> TRUE (TV Shows multi-level: {overlayInfo.SupportedLevels.Length} levels)");
                return true;
            }

            Console.WriteLine($"[DEBUG]   -> FALSE");
            return false;
        }


        private void OverlayCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is CheckBox checkbox)
            {
                var overlayKey = checkbox.Name.Replace("chk_", "");
                Console.WriteLine($"[DEBUG] Checkbox changed for: {overlayKey}, Checked: {checkbox.Checked}");
                
                // Enable/disable advanced button if it exists
                if (advancedButtons.ContainsKey(overlayKey))
                {
                    advancedButtons[overlayKey].Enabled = checkbox.Checked;
                    Console.WriteLine($"[DEBUG] Advanced button enabled={checkbox.Checked} for: {overlayKey}");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] No advanced button found in dictionary for: {overlayKey}");
                }

                // Update profile data
                if (!profile.OverlaySettings.ContainsKey(overlayKey))
                {
                    // Parse overlayKey properly - same logic as AdvancedButton_Click
                    string overlayId;
                    
                    if (overlayKey.EndsWith("_Movies"))
                    {
                        overlayId = overlayKey.Substring(0, overlayKey.Length - "_Movies".Length);
                    }
                    else if (overlayKey.EndsWith("_TV_Shows"))
                    {
                        overlayId = overlayKey.Substring(0, overlayKey.Length - "_TV_Shows".Length);
                    }
                    else
                    {
                        // Fallback to old logic if format doesn't match expected patterns
                        var parts = overlayKey.Split('_');
                        overlayId = string.Join("_", parts.Take(parts.Length - 1));
                    }
                    
                    profile.OverlaySettings[overlayKey] = new OverlayConfiguration
                    {
                        OverlayType = overlayId,
                        IsEnabled = checkbox.Checked,
                        UseAdvancedVariables = false,
                        TemplateVariables = new Dictionary<string, object>()
                    };
                }
                else
                {
                    profile.OverlaySettings[overlayKey].IsEnabled = checkbox.Checked;
                }
            }
        }

        private void AdvancedButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine($"[DEBUG] Advanced button clicked!");
            if (sender is Button button)
            {
                var overlayKey = button.Name.Replace("btnAdvanced_", "");
                
                // Parse overlayKey properly - it's in format "overlayId_MediaType" where MediaType could be "Movies" or "TV_Shows"
                string overlayId;
                string mediaType;
                
                if (overlayKey.EndsWith("_Movies"))
                {
                    overlayId = overlayKey.Substring(0, overlayKey.Length - "_Movies".Length);
                    mediaType = "Movies";
                }
                else if (overlayKey.EndsWith("_TV_Shows"))
                {
                    overlayId = overlayKey.Substring(0, overlayKey.Length - "_TV_Shows".Length);
                    mediaType = "TV Shows";
                }
                else
                {
                    // Fallback to old logic if format doesn't match expected patterns
                    var parts = overlayKey.Split('_');
                    overlayId = string.Join("_", parts.Take(parts.Length - 1));
                    mediaType = parts.Last().Replace("_", " ");
                }

                Console.WriteLine($"[DEBUG] Button clicked - overlayKey: {overlayKey}, overlayId: {overlayId}, mediaType: {mediaType}");

                if (!profile.OverlaySettings.ContainsKey(overlayKey))
                {
                    Console.WriteLine($"[DEBUG] Creating new OverlayConfiguration for: {overlayKey}");
                    profile.OverlaySettings[overlayKey] = new OverlayConfiguration
                    {
                        OverlayType = overlayId,
                        IsEnabled = true,
                        UseAdvancedVariables = false,
                        TemplateVariables = new Dictionary<string, object>()
                    };
                }

                var overlayConfig = profile.OverlaySettings[overlayKey];

                if (overlayId == "ratings")
                {
                    Console.WriteLine($"[DEBUG] Opening RatingsAdvancedForm for: {overlayKey}");
                    try
                    {
                        using (var ratingsForm = new RatingsAdvancedForm(profile, overlayConfig, overlayKey, mediaType))
                        {
                            ratingsForm.ShowDialog(this);
                        }
                        Console.WriteLine($"[DEBUG] RatingsAdvancedForm closed successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[DEBUG] ERROR opening RatingsAdvancedForm: {ex.Message}");
                    }
                }
                else
                {
                    if (OverlayDefaults.AllOverlays.ContainsKey(overlayId))
                    {
                        Console.WriteLine($"[DEBUG] Opening GenericOverlayAdvancedForm for: {overlayKey}");
                        try
                        {
                            var overlayInfo = OverlayDefaults.AllOverlays[overlayId];
                            using (var genericForm = new GenericOverlayAdvancedForm(profile, overlayConfig, overlayKey, mediaType, overlayInfo))
                            {
                                genericForm.ShowDialog(this);
                            }
                            Console.WriteLine($"[DEBUG] GenericOverlayAdvancedForm closed successfully");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[DEBUG] ERROR opening GenericOverlayAdvancedForm: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[DEBUG] ERROR: overlayId '{overlayId}' not found in OverlayDefaults.AllOverlays");
                    }
                }
            }
        }


        private void LoadImageIntoPictureBox(PictureBox pictureBox, string fileName)
        {
            try
            {
                string imagePath = Path.Combine(OVERLAY_IMAGES_PATH, fileName);
                if (File.Exists(imagePath))
                {
                    using (var image = ImageSharpImage.Load(imagePath))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            image.Save(memoryStream, new PngEncoder());
                            memoryStream.Position = 0;
                            pictureBox.Image = System.Drawing.Image.FromStream(memoryStream);
                        }
                    }
                }
                else
                {
                    // Create a placeholder image if file doesn't exist
                    var placeholder = new Bitmap(180, 270);
                    using (var g = Graphics.FromImage(placeholder))
                    {
                        g.Clear(Color.DarkGray);
                        g.DrawString("No Preview", new Font("Arial", 12), Brushes.White, 10, 120);
                    }
                    pictureBox.Image = placeholder;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image {fileName}: {ex.Message}");
            }
        }

        private void LoadProfileData()
        {
            // Load existing overlay selections from profile
            foreach (var setting in profile.OverlaySettings)
            {
                if (overlayCheckboxes.ContainsKey(setting.Key))
                {
                    overlayCheckboxes[setting.Key].Checked = setting.Value.IsEnabled;
                }
            }
        }

        public void SaveProfileData()
        {
            // Profile data is automatically saved through checkbox events
            // This method is kept for interface consistency
        }
    }
}