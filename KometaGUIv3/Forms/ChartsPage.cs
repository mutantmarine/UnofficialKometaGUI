using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using KometaGUIv3.Shared.Models;
using KometaGUIv3.Services;
using KometaGUIv3.Utils;

namespace KometaGUIv3.Forms
{
    public partial class ChartsPage : UserControl
    {
        private KometaProfile profile;
        private TabControl tabControl;
        private Dictionary<string, CheckBox> collectionCheckboxes; // Add dictionary like OverlaysPage
        private Dictionary<string, Button> advancedButtons; // Dictionary to track Advanced buttons
        private PlexService plexService; // Service for retrieving Plex collections

        public ChartsPage(KometaProfile profile)
        {
            this.profile = profile;
            collectionCheckboxes = new Dictionary<string, CheckBox>(); // Initialize dictionary
            advancedButtons = new Dictionary<string, Button>(); // Initialize Advanced buttons dictionary
            plexService = new PlexService(); // Initialize Plex service
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

            // Custom Collections Tab
            CreateCustomTab();

            // My Collections Tab (existing Plex collections)
            CreateMyCollectionsTab();

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
                    Size = new Size(420, 40),
                    Location = new Point(310, y),
                    ForeColor = DarkTheme.TextColor,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 8F)
                };

                // Advanced button
                var advancedButton = new Button
                {
                    Text = "Advanced",
                    Size = new Size(80, 25),
                    Location = new Point(740, y),
                    Name = $"btnAdvanced_{collection.Id}",
                    Enabled = false // Initially disabled until collection is checked
                };
                advancedButton.Click += AdvancedButton_Click;
                
                // Add controls directly to panel
                panel.Controls.Add(checkBox);
                panel.Controls.Add(descLabel);
                panel.Controls.Add(advancedButton);
                
                // Charts tab uses original collection ID (applies to all libraries)
                string dictKey = collection.Id;
                collectionCheckboxes[dictKey] = checkBox;
                advancedButtons[dictKey] = advancedButton;
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
                    Size = new Size(420, 40),
                    Location = new Point(310, y), // Moved right and aligned with checkbox
                    ForeColor = DarkTheme.TextColor,
                    Font = new Font("Segoe UI", 8F)
                };

                // Advanced button
                var advancedButton = new Button
                {
                    Text = "Advanced",
                    Size = new Size(80, 25),
                    Location = new Point(740, y),
                    Name = $"btnAdvanced_{collection.Id}",
                    Enabled = false // Initially disabled until collection is checked
                };
                advancedButton.Click += AdvancedButton_Click;
                
                panel.Controls.AddRange(new Control[] { checkBox, descLabel, advancedButton }); // Use AddRange like OverlaysPage
                
                // Awards tab uses original collection ID (applies to all libraries)
                string dictKey = collection.Id;
                collectionCheckboxes[dictKey] = checkBox; // Store in dictionary
                advancedButtons[dictKey] = advancedButton;
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

        private void CreateCustomTab()
        {
            var tabPage = new TabPage("Custom")
            {
                BackColor = DarkTheme.BackgroundColor
            };

            var panel = new Panel
            {
                Size = new Size(1280, 650),
                Location = new Point(0, 0),
                AutoScroll = true,
                BackColor = DarkTheme.BackgroundColor
            };

            // Title and description
            var titleLabel = new Label
            {
                Text = "Custom Collections",
                Font = DarkTheme.GetHeaderFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(300, 30),
                Location = new Point(20, 20)
            };

            var descLabel = new Label
            {
                Text = "Manage your custom collection configurations",
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = Color.LightGray,
                Size = new Size(400, 20),
                Location = new Point(20, 55)
            };

            // Action buttons
            var btnAdd = new Button
            {
                Text = "Add Custom Collection",
                Size = new Size(150, 35),
                Location = new Point(20, 90),
                Name = "btnPrimary"
            };
            btnAdd.Click += BtnAddCustomCollection_Click;

            var btnEdit = new Button
            {
                Text = "Edit",
                Size = new Size(80, 35),
                Location = new Point(180, 90),
                Enabled = false
            };
            btnEdit.Click += BtnEditCustomCollection_Click;

            var btnRemove = new Button
            {
                Text = "Remove",
                Size = new Size(80, 35),
                Location = new Point(270, 90),
                Enabled = false
            };
            btnRemove.Click += BtnRemoveCustomCollection_Click;

            // ListView to display custom collections
            var listView = new ListView
            {
                Size = new Size(600, 400),
                Location = new Point(20, 140),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                BackColor = DarkTheme.PanelColor,
                ForeColor = DarkTheme.TextColor,
                Name = "lvCustomCollections"
            };

            listView.Columns.Add("Name", 200);
            listView.Columns.Add("URL", 250);
            listView.Columns.Add("Poster", 150);

            listView.SelectedIndexChanged += (s, e) =>
            {
                btnEdit.Enabled = listView.SelectedItems.Count > 0;
                btnRemove.Enabled = listView.SelectedItems.Count > 0;
            };

            panel.Controls.AddRange(new Control[] { titleLabel, descLabel, btnAdd, btnEdit, btnRemove, listView });
            tabPage.Controls.Add(panel);
            tabControl.TabPages.Add(tabPage);
        }

        private void CreateMyCollectionsTab()
        {
            var tabPage = new TabPage("My Collections")
            {
                BackColor = DarkTheme.BackgroundColor
            };

            var panel = new Panel
            {
                Size = new Size(1280, 650),
                Location = new Point(0, 0),
                AutoScroll = true,
                BackColor = DarkTheme.BackgroundColor
            };

            // Title and description
            var titleLabel = new Label
            {
                Text = "My Collections",
                Font = DarkTheme.GetHeaderFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(300, 30),
                Location = new Point(20, 20)
            };

            var descLabel = new Label
            {
                Text = "Your existing collections from Plex libraries",
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = Color.LightGray,
                Size = new Size(400, 20),
                Location = new Point(20, 55)
            };

            // Filter and refresh section
            var lblFilter = new Label
            {
                Text = "Library:",
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = DarkTheme.TextColor,
                Size = new Size(60, 20),
                Location = new Point(20, 90)
            };

            var cmbLibraryFilter = new ComboBox
            {
                Size = new Size(150, 25),
                Location = new Point(85, 87),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Name = "cmbLibraryFilter"
            };
            cmbLibraryFilter.Items.Add("All Libraries");
            cmbLibraryFilter.SelectedIndex = 0;

            var btnRefresh = new Button
            {
                Text = "Refresh Collections",
                Size = new Size(130, 35),
                Location = new Point(250, 85),
                Name = "btnPrimary"
            };
            btnRefresh.Click += BtnRefreshCollections_Click;

            // Loading label (initially hidden)
            var lblLoading = new Label
            {
                Text = "Loading collections...",
                Font = DarkTheme.GetDefaultFont(),
                ForeColor = Color.Orange,
                Size = new Size(200, 20),
                Location = new Point(20, 125),
                Visible = false,
                Name = "lblLoading"
            };

            // DataGridView to display collections
            var dgvCollections = new DataGridView
            {
                Size = new Size(1200, 480),
                Location = new Point(20, 155),
                BackgroundColor = DarkTheme.PanelColor,
                ForeColor = DarkTheme.TextColor,
                GridColor = Color.Gray,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                Name = "dgvCollections"
            };

            // Configure columns
            dgvCollections.Columns.Add("Name", "Collection Name");
            dgvCollections.Columns.Add("Library", "Library");
            dgvCollections.Columns.Add("Items", "Items");
            dgvCollections.Columns.Add("Updated", "Last Updated");

            // Set column widths
            dgvCollections.Columns["Name"].Width = 350;
            dgvCollections.Columns["Library"].Width = 200;
            dgvCollections.Columns["Items"].Width = 100;
            dgvCollections.Columns["Updated"].Width = 150;

            // Add Advanced button column
            var advancedColumn = new DataGridViewButtonColumn
            {
                Name = "Advanced",
                HeaderText = "Advanced",
                Text = "Advanced",
                UseColumnTextForButtonValue = true,
                Width = 100
            };
            dgvCollections.Columns.Add(advancedColumn);

            dgvCollections.CellClick += DgvCollections_CellClick;

            // Apply dark theme to DataGridView
            dgvCollections.BackgroundColor = DarkTheme.PanelColor;
            dgvCollections.DefaultCellStyle.BackColor = DarkTheme.PanelColor;
            dgvCollections.DefaultCellStyle.ForeColor = DarkTheme.TextColor;
            dgvCollections.ColumnHeadersDefaultCellStyle.BackColor = DarkTheme.BackgroundColor;
            dgvCollections.ColumnHeadersDefaultCellStyle.ForeColor = DarkTheme.TextColor;
            dgvCollections.EnableHeadersVisualStyles = false;

            panel.Controls.AddRange(new Control[] { titleLabel, descLabel, lblFilter, cmbLibraryFilter, btnRefresh, lblLoading, dgvCollections });
            tabPage.Controls.Add(panel);
            tabControl.TabPages.Add(tabPage);

            // Load collections if Plex is configured
            if (profile.Plex.IsAuthenticated && !string.IsNullOrEmpty(profile.Plex.Url))
            {
                LoadMyCollections();
            }
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
                    Size = new Size(420, 40),
                    Location = new Point(310, y),
                    ForeColor = DarkTheme.TextColor,
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 8F)
                };

                // Advanced button
                var advancedButton = new Button
                {
                    Text = "Advanced",
                    Size = new Size(80, 25),
                    Location = new Point(740, y),
                    Name = $"btnAdvanced_{GetCollectionStorageKey(collection.Id, tabName)}",
                    Enabled = false // Initially disabled until collection is checked
                };
                advancedButton.Click += AdvancedButton_Click;
                
                // Add controls directly to panel
                panel.Controls.Add(checkBox);
                panel.Controls.Add(descLabel);
                panel.Controls.Add(advancedButton);
                
                // Use tab-aware dictionary key to match storage key format
                string dictKey = GetCollectionStorageKey(collection.Id, tabName);
                collectionCheckboxes[dictKey] = checkBox;
                advancedButtons[dictKey] = advancedButton;
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
                
                // Determine which tab this checkbox belongs to
                string tabName = GetTabNameForCheckbox(checkBox);
                
                // Determine the storage key based on the tab context
                string storageKey = GetCollectionStorageKey(collection.Id, tabName);
                
                // Update profile
                if (!profile.SelectedCharts.ContainsKey(storageKey))
                {
                    profile.SelectedCharts.Add(storageKey, checkBox.Checked);
                }
                else
                {
                    profile.SelectedCharts[storageKey] = checkBox.Checked;
                }

                // Enable/disable the corresponding Advanced button
                if (advancedButtons.ContainsKey(storageKey))
                {
                    advancedButtons[storageKey].Enabled = checkBox.Checked;
                }
            }
        }

        private string GetTabNameForCheckbox(CheckBox checkBox)
        {
            // Find which tab this checkbox belongs to by traversing up the control hierarchy
            Control current = checkBox.Parent;
            while (current != null)
            {
                if (current is TabPage tabPage)
                {
                    return tabPage.Text;
                }
                current = current.Parent;
            }
            return string.Empty;
        }

        private string GetCollectionStorageKey(string collectionId, string tabName)
        {
            // Use tab context to determine correct prefix
            switch (tabName)
            {
                case "Movies Only":
                    return $"movie_{collectionId}";
                case "TV Shows Only":
                    return $"show_{collectionId}";
                default:
                    // Charts, Awards, and Both collections use original ID (apply to all libraries)
                    return collectionId;
            }
        }

        private void AdvancedButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button)
            {
                // Extract collection key from button name
                var collectionKey = button.Name.Replace("btnAdvanced_", "");
                
                // Find the corresponding collection from the button tag or by searching
                DefaultCollection collection = null;
                string tabName = "";
                
                // Find the collection and tab name by searching through the dictionaries
                foreach (var kvp in collectionCheckboxes)
                {
                    if (kvp.Key == collectionKey && kvp.Value.Tag is DefaultCollection coll)
                    {
                        collection = coll;
                        // Get tab name from the collection key
                        if (collectionKey.StartsWith("movie_"))
                            tabName = "Movies Only";
                        else if (collectionKey.StartsWith("show_"))
                            tabName = "TV Shows Only";
                        else
                            tabName = GetTabNameForCheckbox(kvp.Value);
                        break;
                    }
                }

                if (collection != null)
                {
                    using (var advancedForm = new CollectionAdvancedForm(profile, collectionKey, tabName, collection))
                    {
                        if (advancedForm.ShowDialog() == DialogResult.OK)
                        {
                            // Save the configuration when user clicks OK
                            var config = advancedForm.GetConfiguration();
                            // TODO: Store config in profile (will be implemented when profile is updated)
                        }
                    }
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
            // Dictionary keys now match storage keys exactly, so we can use them directly
            foreach (var checkboxPair in collectionCheckboxes)
            {
                var storageKey = checkboxPair.Key; // Dictionary key is now the same as storage key
                var checkBox = checkboxPair.Value;
                
                if (profile.SelectedCharts.ContainsKey(storageKey))
                {
                    checkBox.Checked = profile.SelectedCharts[storageKey];
                    if (checkBox.Tag is DefaultCollection collection)
                    {
                        collection.IsSelected = checkBox.Checked;
                    }
                    
                    // Enable/disable corresponding Advanced button
                    if (advancedButtons.ContainsKey(storageKey))
                    {
                        advancedButtons[storageKey].Enabled = checkBox.Checked;
                    }
                }
            }
        }


        public void SaveProfileData()
        {
            // Data is already saved in real-time through the CheckedChanged events
        }

        private void BtnAddCustomCollection_Click(object sender, EventArgs e)
        {
            using (var customForm = new CustomCollectionForm())
            {
                if (customForm.ShowDialog() == DialogResult.OK)
                {
                    var newCollection = customForm.GetCustomCollection();
                    
                    // Add to ListView
                    var listView = this.Controls.Find("lvCustomCollections", true).FirstOrDefault() as ListView;
                    if (listView != null)
                    {
                        var listItem = new ListViewItem(newCollection.Name);
                        listItem.SubItems.Add(newCollection.Url);
                        listItem.SubItems.Add(newCollection.Poster);
                        listItem.Tag = newCollection;
                        listView.Items.Add(listItem);
                    }
                    
                    // TODO: Save to profile when profile is updated
                }
            }
        }

        private void BtnEditCustomCollection_Click(object sender, EventArgs e)
        {
            var listView = this.Controls.Find("lvCustomCollections", true).FirstOrDefault() as ListView;
            if (listView?.SelectedItems.Count > 0)
            {
                var selectedItem = listView.SelectedItems[0];
                var customCollection = selectedItem.Tag as CustomCollection;
                
                using (var customForm = new CustomCollectionForm(customCollection))
                {
                    if (customForm.ShowDialog() == DialogResult.OK)
                    {
                        var updatedCollection = customForm.GetCustomCollection();
                        
                        // Update ListView
                        selectedItem.Text = updatedCollection.Name;
                        selectedItem.SubItems[1].Text = updatedCollection.Url;
                        selectedItem.SubItems[2].Text = updatedCollection.Poster;
                        selectedItem.Tag = updatedCollection;
                        
                        // TODO: Update profile when profile is updated
                    }
                }
            }
        }

        private void BtnRemoveCustomCollection_Click(object sender, EventArgs e)
        {
            // TODO: Remove selected custom collection
            var listView = this.Controls.Find("lvCustomCollections", true).FirstOrDefault() as ListView;
            if (listView?.SelectedItems.Count > 0)
            {
                var result = MessageBox.Show("Are you sure you want to remove this custom collection?", 
                    "Remove Custom Collection", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    listView.SelectedItems[0].Remove();
                }
            }
        }

        private async void LoadMyCollections()
        {
            try
            {
                var loadingLabel = this.Controls.Find("lblLoading", true).FirstOrDefault();
                var dgvCollections = this.Controls.Find("dgvCollections", true).FirstOrDefault() as DataGridView;
                
                if (loadingLabel != null && dgvCollections != null)
                {
                    loadingLabel.Visible = true;
                    dgvCollections.Rows.Clear();

                    var collections = await plexService.GetAllCollections(profile.Plex.Url, profile.Plex.Token, profile.Plex.AvailableLibraries);
                    
                    // Store collections in profile
                    profile.MyPlexCollections = collections;

                    // Populate DataGridView
                    foreach (var collection in collections)
                    {
                        var rowIndex = dgvCollections.Rows.Add();
                        var row = dgvCollections.Rows[rowIndex];
                        
                        row.Cells["Name"].Value = collection.Name;
                        row.Cells["Library"].Value = collection.LibraryName;
                        row.Cells["Items"].Value = collection.ItemCount.ToString();
                        row.Cells["Updated"].Value = collection.UpdatedAt.ToString("MM/dd/yyyy");
                        row.Tag = collection; // Store the collection object for reference
                    }

                    // Update library filter dropdown
                    var cmbLibraryFilter = this.Controls.Find("cmbLibraryFilter", true).FirstOrDefault() as ComboBox;
                    if (cmbLibraryFilter != null)
                    {
                        var currentSelection = cmbLibraryFilter.SelectedItem?.ToString();
                        cmbLibraryFilter.Items.Clear();
                        cmbLibraryFilter.Items.Add("All Libraries");
                        
                        var libraries = collections.Select(c => c.LibraryName).Distinct().OrderBy(l => l);
                        foreach (var library in libraries)
                        {
                            cmbLibraryFilter.Items.Add(library);
                        }
                        
                        // Restore selection or default to "All Libraries"
                        var itemToSelect = cmbLibraryFilter.Items.Cast<string>().FirstOrDefault(i => i == currentSelection) ?? "All Libraries";
                        cmbLibraryFilter.SelectedItem = itemToSelect;
                    }

                    loadingLabel.Visible = false;
                }
            }
            catch (Exception ex)
            {
                var loadingLabel = this.Controls.Find("lblLoading", true).FirstOrDefault();
                if (loadingLabel != null)
                {
                    loadingLabel.Text = $"Error loading collections: {ex.Message}";
                    loadingLabel.ForeColor = Color.Red;
                }
            }
        }

        private async void BtnRefreshCollections_Click(object sender, EventArgs e)
        {
            if (!profile.Plex.IsAuthenticated || string.IsNullOrEmpty(profile.Plex.Url))
            {
                MessageBox.Show("Please configure your Plex connection first in the Connections tab.", 
                    "Plex Not Configured", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            LoadMyCollections();
        }

        private void DgvCollections_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && sender is DataGridView dgv)
            {
                var columnName = dgv.Columns[e.ColumnIndex].Name;
                
                if (columnName == "Advanced")
                {
                    var row = dgv.Rows[e.RowIndex];
                    var collection = row.Tag as PlexCollection;
                    
                    if (collection != null)
                    {
                        OpenAdvancedFormForPlexCollection(collection);
                    }
                }
            }
        }

        private void OpenAdvancedFormForPlexCollection(PlexCollection collection)
        {
            // Create a DefaultCollection wrapper for the Plex collection
            var defaultCollection = new DefaultCollection(collection.Id, collection.Name, $"Plex collection from {collection.LibraryName} library");
            
            // Determine media type from library type
            string mediaType = DetermineMediaTypeFromLibrary(collection.LibraryName);
            
            using (var advancedForm = new CollectionAdvancedForm(profile, $"plex_{collection.Id}", mediaType, defaultCollection))
            {
                if (advancedForm.ShowDialog() == DialogResult.OK)
                {
                    var config = advancedForm.GetConfiguration();
                    // Store config in the Plex collections advanced settings
                    profile.MyCollectionAdvancedSettings[$"plex_{collection.Id}"] = config;
                }
            }
        }

        private string DetermineMediaTypeFromLibrary(string libraryName)
        {
            // Try to determine media type from the library name or type
            // This is a simple heuristic - could be enhanced later
            var library = profile.Plex.AvailableLibraries.FirstOrDefault(l => l.Name == libraryName);
            if (library != null)
            {
                switch (library.Type?.ToLower())
                {
                    case "movie":
                        return "Movies Only";
                    case "show":
                        return "TV Shows Only";
                    default:
                        return "Movies & TV Shows"; // Default for unknown types
                }
            }
            return "Movies & TV Shows"; // Safe default
        }
    }
}