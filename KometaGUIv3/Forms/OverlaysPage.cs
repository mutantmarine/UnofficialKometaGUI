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
        
        // Preview image controls (outside of tabs)
        private PictureBox picMoviePreview1, picMoviePreview2;
        private PictureBox picTVShowPreview, picTVSeasonPreview, picTVEpisodePreview;
        private Button btnSelectAll, btnUnselectAll;

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

            // Tab control for Movies and TV Shows (slimmed down)
            SetupTabControl();
            
            // Preview images outside of tabs
            SetupPreviewImages();
            
            // Select/Unselect All buttons
            SetupControlButtons();

            this.Controls.AddRange(new Control[] { titleLabel, subtitleLabel });
            
            // Apply dark theme to get proper button styling
            DarkTheme.ApplyDarkTheme(this);
        }


        private void SetupTabControl()
        {
            tabControl = new TabControl
            {
                Size = new Size(700, 700),  // Slimmed down width to make room for external previews
                Location = new Point(30, 90),  
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

            // Setup overlays for each tab (no more preview images inside)
            SetupOverlaysForTab(moviesTab, "Movies");
            SetupOverlaysForTab(tvShowsTab, "TV Shows");
            
            // Add event handler for tab switching
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            this.Controls.Add(tabControl);
        }

        private void SetupOverlaysForTab(TabPage tabPage, string mediaType)
        {
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
                        Size = new Size(180, 25),  // Slightly smaller for narrower layout
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
                        Size = new Size(280, 40),  // Adjust for narrower layout
                        Location = new Point(20, yPos + 25),
                        ForeColor = Color.LightGray,
                        Font = new Font("Segoe UI", 9F)
                    };

                    // Positions
                    var posLabel = new Label
                    {
                        Text = $"Positions: {string.Join(", ", overlayInfo.Positions)}",
                        Size = new Size(160, 20),  // Smaller for narrower layout
                        Location = new Point(320, yPos + 5),  // Moved left
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
                            Location = new Point(500, yPos + 2),  // Moved left for narrower layout
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

        private void SetupPreviewImages()
        {
            // Movie preview images (2 total) - 10% larger and positioned left
            picMoviePreview1 = new PictureBox
            {
                Size = new Size(774, 258),  // Another 5% smaller (815->774, 272->258)
                Location = new Point(680, 104),  // Moved left from 750 to 680
                BorderStyle = BorderStyle.None,
                SizeMode = PictureBoxSizeMode.Zoom,
                Name = "picMoviePreview1",
                Visible = true  // Start with movies visible
            };

            picMoviePreview2 = new PictureBox
            {
                Size = new Size(800, 267),  // Resized to fit within window bounds
                Location = new Point(680, 410),  // Moved left from 750 to 680
                BorderStyle = BorderStyle.None,
                SizeMode = PictureBoxSizeMode.Zoom,
                Name = "picMoviePreview2",
                Visible = true
            };

            // TV Show preview images (3 total) - 20% larger and positioned left/down
            picTVShowPreview = new PictureBox
            {
                Size = new Size(936, 234),  // 20% larger (780->936, 195->234)
                Location = new Point(544, 79),  // Moved further left to fit within window (680→544)
                BorderStyle = BorderStyle.None,
                SizeMode = PictureBoxSizeMode.Zoom,
                Name = "picTVShowPreview",
                Visible = false
            };

            picTVSeasonPreview = new PictureBox
            {
                Size = new Size(936, 234),  // 20% larger
                Location = new Point(544, 333),  // Moved further left to fit within window (680→544)
                BorderStyle = BorderStyle.None,
                SizeMode = PictureBoxSizeMode.Zoom,
                Name = "picTVSeasonPreview",
                Visible = false
            };

            picTVEpisodePreview = new PictureBox
            {
                Size = new Size(936, 234),  // 20% larger
                Location = new Point(544, 587),  // Moved further left to fit within window (680→544)
                BorderStyle = BorderStyle.None,
                SizeMode = PictureBoxSizeMode.Zoom,
                Name = "picTVEpisodePreview",
                Visible = false
            };

            // Add all preview controls to main form
            this.Controls.AddRange(new Control[] { 
                picMoviePreview1, picMoviePreview2,
                picTVShowPreview, picTVSeasonPreview, picTVEpisodePreview 
            });

            // Load the preview images
            LoadPreviewImages();
        }
        
        private void SetupControlButtons()
        {
            // Select All button
            btnSelectAll = new Button
            {
                Text = "Select All on Current Tab",
                Size = new Size(180, 35),
                Location = new Point(30, 800),
                Name = "btnPrimary"
            };
            
            // Unselect All button
            btnUnselectAll = new Button
            {
                Text = "Unselect All on Current Tab", 
                Size = new Size(180, 35),
                Location = new Point(220, 800)
            };
            
            // Add event handlers
            btnSelectAll.Click += BtnSelectAll_Click;
            btnUnselectAll.Click += BtnUnselectAll_Click;
            
            // Add buttons to form
            this.Controls.AddRange(new Control[] { btnSelectAll, btnUnselectAll });
        }

        private void LoadPreviewImages()
        {
            try
            {
                // Load movie preview images
                LoadImageIntoPictureBox(picMoviePreview1, "movie_overlay_preview_1.webp");
                LoadImageIntoPictureBox(picMoviePreview2, "movie_overlay_preview_2.webp");
                
                // Load TV show preview images
                LoadImageIntoPictureBox(picTVShowPreview, "tvshows_show_overlay_preview.webp");
                LoadImageIntoPictureBox(picTVSeasonPreview, "tvshows_season_overlay_preview.webp");
                LoadImageIntoPictureBox(picTVEpisodePreview, "tvshows_episode_overlay_preview.webp");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading preview images: {ex.Message}");
            }
        }
        
        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                string selectedTabText = tabControl.SelectedTab.Text;
                
                if (selectedTabText == "Movies")
                {
                    // Show movie previews, hide TV show previews
                    picMoviePreview1.Visible = true;
                    picMoviePreview2.Visible = true;
                    
                    picTVShowPreview.Visible = false;
                    picTVSeasonPreview.Visible = false;
                    picTVEpisodePreview.Visible = false;
                }
                else if (selectedTabText == "TV Shows")
                {
                    // Hide movie previews, show TV show previews
                    picMoviePreview1.Visible = false;
                    picMoviePreview2.Visible = false;
                    
                    picTVShowPreview.Visible = true;
                    picTVSeasonPreview.Visible = true;
                    picTVEpisodePreview.Visible = true;
                }
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
                    CreatePlaceholderImage(pictureBox, "No Preview");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image {fileName}: {ex.Message}");
                CreatePlaceholderImage(pictureBox, "Error Loading");
            }
        }

        private void CreatePlaceholderImage(PictureBox pictureBox, string text)
        {
            var placeholder = new Bitmap(600, 200);  // Match new larger size
            using (var g = Graphics.FromImage(placeholder))
            {
                g.Clear(Color.DarkGray);
                g.DrawString(text, new Font("Arial", 14), Brushes.White, 250, 90);  // Centered for larger image
            }
            pictureBox.Image = placeholder;
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

        private void BtnSelectAll_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                SetAllCheckboxes(tabControl.SelectedTab, true);
            }
        }

        private void BtnUnselectAll_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                SetAllCheckboxes(tabControl.SelectedTab, false);
            }
        }

        private void SetAllCheckboxes(TabPage tabPage, bool isChecked)
        {
            // Find all checkboxes in the current tab
            var checkboxes = new List<CheckBox>();
            FindCheckboxes(tabPage, checkboxes);
            
            foreach (var checkBox in checkboxes)
            {
                if (checkBox.Name.StartsWith("chk_"))
                {
                    // Temporarily disable event to avoid multiple firings
                    checkBox.CheckedChanged -= OverlayCheckBox_CheckedChanged;
                    checkBox.Checked = isChecked;
                    checkBox.CheckedChanged += OverlayCheckBox_CheckedChanged;
                    
                    // Manually trigger the event to update the profile
                    OverlayCheckBox_CheckedChanged(checkBox, EventArgs.Empty);
                }
            }
        }
        
        private void FindCheckboxes(Control parent, List<CheckBox> checkboxes)
        {
            foreach (Control control in parent.Controls)
            {
                if (control is CheckBox checkbox)
                {
                    checkboxes.Add(checkbox);
                }
                else if (control.HasChildren)
                {
                    FindCheckboxes(control, checkboxes);
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