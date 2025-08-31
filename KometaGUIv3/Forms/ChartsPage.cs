using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KometaGUIv3.Models;
using KometaGUIv3.Utils;

namespace KometaGUIv3.Forms
{
    public partial class ChartsPage : UserControl
    {
        private KometaProfile profile;
        private TabControl tabControl;
        private Dictionary<string, CheckBox> collectionCheckboxes; // Add dictionary like OverlaysPage

        public ChartsPage(KometaProfile profile)
        {
            this.profile = profile;
            collectionCheckboxes = new Dictionary<string, CheckBox>(); // Initialize dictionary
            InitializeComponent();
            SetupControls();
        }

        private void SetupControls()
        {
            this.BackColor = DarkTheme.BackgroundColor;
            this.Dock = DockStyle.Fill;

            // Page title
            var titleLabel = new Label
            {
                Text = "Collection Selection", // Changed from "Chart Collections"
                Font = DarkTheme.GetTitleFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(400, 40),
                Location = new Point(30, 20)
            };

            var descriptionLabel = new Label
            {
                Text = "Select the collections you want to include in your libraries. These collections will automatically update with trending, popular, and top-rated content.",
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(900, 30),
                Location = new Point(30, 65)
            };

            // Tab control for different collection types
            tabControl = new TabControl
            {
                Size = new Size(1300, 700), // Increased size for bigger window
                Location = new Point(30, 110),
                BackColor = DarkTheme.PanelColor,
                ForeColor = DarkTheme.TextColor,
                HotTrack = false, // Disable hot tracking which might interfere
                Multiline = false // Ensure single line tabs
            };

            // Charts Tab
            CreateChartsTab();
            
            // Awards Tab
            CreateAwardsTab();

            // Movies Tab
            CreateMoviesTab();

            // TV Shows Tab
            CreateShowsTab();

            // Both Movies & TV Tab
            CreateBothTab();

            // Control buttons
            var selectAllBtn = new Button
            {
                Text = "Select All on Current Tab",
                Size = new Size(180, 35),
                Location = new Point(30, 830), // Moved down for bigger window
                Name = "btnPrimary"
            };

            var unselectAllBtn = new Button
            {
                Text = "Unselect All on Current Tab",
                Size = new Size(180, 35),
                Location = new Point(220, 830) // Moved down for bigger window
            };


            selectAllBtn.Click += SelectAllBtn_Click;
            unselectAllBtn.Click += UnselectAllBtn_Click;

            this.Controls.AddRange(new Control[] {
                titleLabel, descriptionLabel, tabControl, selectAllBtn, unselectAllBtn
            });

            // Apply dark theme BEFORE loading profile selections
            DarkTheme.ApplyDarkTheme(this);
            
            // Load profile selections AFTER controls are fully set up
            LoadProfileSelections();
        }

        private void CreateChartsTab()
        {
            var tabPage = new TabPage("Charts")
            {
                BackColor = DarkTheme.BackgroundColor
            };

            var panel = new Panel
            {
                Size = new Size(1280, 650), // Fixed size instead of Dock.Fill
                Location = new Point(0, 0),
                AutoScroll = true,
                BackColor = DarkTheme.BackgroundColor
            };

            int y = 20;
            
            foreach (var collection in KometaDefaults.AllChartCollections)
            {
                var checkBox = new CheckBox
                {
                    Text = collection.Name,
                    Tag = collection,
                    Size = new Size(280, 25),
                    Location = new Point(20, y),
                    ForeColor = DarkTheme.TextColor,
                    BackColor = Color.Transparent,
                    Name = $"chk_{collection.Id}",
                    Enabled = true,
                    Visible = true,
                    TabStop = true,
                    UseCompatibleTextRendering = false
                };
                
                checkBox.CheckedChanged += Collection_CheckedChanged;

                var descLabel = new Label
                {
                    Text = collection.Description,
                    Size = new Size(500, 40),
                    Location = new Point(310, y),
                    ForeColor = DarkTheme.TextColor,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 8F)
                };
                
                // Add controls directly to panel
                panel.Controls.Add(checkBox);
                panel.Controls.Add(descLabel);
                collectionCheckboxes[collection.Id] = checkBox;
                y += 45;
            }

            tabPage.Controls.Add(panel);
            tabControl.TabPages.Add(tabPage);
        }

        private void CreateAwardsTab()
        {
            var tabPage = new TabPage("Awards")
            {
                BackColor = DarkTheme.BackgroundColor
            };

            var panel = new Panel
            {
                Size = new Size(1280, 650), // Fixed size instead of Dock.Fill
                Location = new Point(0, 0),
                AutoScroll = true,
                BackColor = DarkTheme.BackgroundColor
            };

            int y = 20;
            
            foreach (var collection in KometaDefaults.AllAwardCollections)
            {
                var checkBox = new CheckBox
                {
                    Text = collection.Name,
                    Tag = collection,
                    Size = new Size(280, 25), // Made larger to ensure clickable area
                    Location = new Point(20, y),
                    ForeColor = DarkTheme.TextColor,
                    Name = $"chk_{collection.Id}",
                    Enabled = true,
                    Visible = true,
                    TabStop = true
                };
                checkBox.CheckedChanged += Collection_CheckedChanged;

                var descLabel = new Label
                {
                    Text = collection.Description,
                    Size = new Size(500, 40),
                    Location = new Point(310, y), // Moved right and aligned with checkbox
                    ForeColor = DarkTheme.TextColor,
                    Font = new Font("Segoe UI", 8F)
                };

                // Awards collections now use simple checkbox only (advanced year range controls removed)

                
                panel.Controls.AddRange(new Control[] { checkBox, descLabel }); // Use AddRange like OverlaysPage
                collectionCheckboxes[collection.Id] = checkBox; // Store in dictionary
                y += 45;
            }

            tabPage.Controls.Add(panel);
            tabControl.TabPages.Add(tabPage);
        }

        private void CreateMoviesTab()
        {
            CreateCollectionTab("Movies Only", KometaDefaults.AllMovieCollections);
        }

        private void CreateShowsTab()
        {
            CreateCollectionTab("TV Shows Only", KometaDefaults.AllShowCollections);
        }

        private void CreateBothTab()
        {
            CreateCollectionTab("Movies & TV Shows", KometaDefaults.AllBothCollections);
        }

        private void CreateCollectionTab(string tabName, System.Collections.Generic.List<DefaultCollection> collections)
        {
            var tabPage = new TabPage(tabName)
            {
                BackColor = DarkTheme.BackgroundColor
            };

            var panel = new Panel
            {
                Size = new Size(1280, 650), // Fixed size instead of Dock.Fill
                Location = new Point(0, 0),
                AutoScroll = true,
                BackColor = DarkTheme.BackgroundColor
            };

            int y = 20;
            
            foreach (var collection in collections)
            {
                var checkBox = new CheckBox
                {
                    Text = collection.Name,
                    Tag = collection,
                    Size = new Size(280, 25),
                    Location = new Point(20, y),
                    ForeColor = DarkTheme.TextColor,
                    BackColor = Color.Transparent,
                    Name = $"chk_{collection.Id}",
                    Enabled = true,
                    Visible = true,
                    TabStop = true,
                    UseCompatibleTextRendering = false
                };
                
                checkBox.CheckedChanged += Collection_CheckedChanged;

                var descLabel = new Label
                {
                    Text = collection.Description,
                    Size = new Size(500, 40),
                    Location = new Point(310, y),
                    ForeColor = DarkTheme.TextColor,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 8F)
                };
                
                // Add controls directly to panel
                panel.Controls.Add(checkBox);
                panel.Controls.Add(descLabel);
                collectionCheckboxes[collection.Id] = checkBox;
                y += 45;
            }

            tabPage.Controls.Add(panel);
            tabControl.TabPages.Add(tabPage);
        }

        private void Collection_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is DefaultCollection collection)
            {
                collection.IsSelected = checkBox.Checked;
                
                // Update profile
                if (!profile.SelectedCharts.ContainsKey(collection.Id))
                {
                    profile.SelectedCharts.Add(collection.Id, checkBox.Checked);
                }
                else
                {
                    profile.SelectedCharts[collection.Id] = checkBox.Checked;
                }
            }
        }

        private void SelectAllBtn_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                SetAllCheckboxes(tabControl.SelectedTab, true);
            }
        }

        private void UnselectAllBtn_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedTab != null)
            {
                SetAllCheckboxes(tabControl.SelectedTab, false);
            }
        }

        private void SetAllCheckboxes(TabPage tabPage, bool isChecked)
        {
            // Get the panel from the current tab and find all checkboxes
            var panel = tabPage.Controls.OfType<Panel>().FirstOrDefault();
            
            if (panel != null)
            {
                var checkboxes = panel.Controls.OfType<CheckBox>().ToList();
                
                foreach (var checkBox in checkboxes)
                {
                    // Temporarily disable event to avoid multiple firings
                    checkBox.CheckedChanged -= Collection_CheckedChanged;
                    checkBox.Checked = isChecked;
                    checkBox.CheckedChanged += Collection_CheckedChanged;
                    
                    // Manually trigger the event to update the profile
                    Collection_CheckedChanged(checkBox, EventArgs.Empty);
                }
            }
        }

        private void LoadProfileSelections()
        {
            // Load existing selections from profile using the dictionary
            foreach (var checkboxPair in collectionCheckboxes)
            {
                var collectionId = checkboxPair.Key;
                var checkBox = checkboxPair.Value;
                
                if (profile.SelectedCharts.ContainsKey(collectionId))
                {
                    checkBox.Checked = profile.SelectedCharts[collectionId];
                    if (checkBox.Tag is DefaultCollection collection)
                    {
                        collection.IsSelected = checkBox.Checked;
                    }
                }
            }
        }


        public void SaveProfileData()
        {
            // Data is already saved in real-time through the CheckedChanged events
        }
    }
}