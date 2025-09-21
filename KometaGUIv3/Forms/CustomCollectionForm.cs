using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using KometaGUIv3.Shared.Models;
using KometaGUIv3.Utils;

namespace KometaGUIv3.Forms
{
    public partial class CustomCollectionForm : Form
    {
        private CustomCollection customCollection;
        private bool isEditMode;

        // Main controls
        private Label lblName, lblUrl, lblPoster, lblDescription;
        private TextBox txtName, txtUrl, txtPoster, txtDescription;
        private Button btnBrowsePoster, btnOK, btnCancel;

        // Additional variables section (expandable for future use)
        private GroupBox grpAdditionalVariables;
        private Label lblPlaceholder;

        public CustomCollectionForm(CustomCollection collection = null)
        {
            this.customCollection = collection ?? new CustomCollection();
            this.isEditMode = collection != null;
            
            InitializeForm();
            SetupControls();
            LoadData();
        }

        private void InitializeForm()
        {
            InitializeComponent();
            this.Text = isEditMode ? "Edit Custom Collection" : "Add Custom Collection";
            this.Size = new Size(500, 450);
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

            // Collection Name
            lblName = new Label
            {
                Text = "Collection Name:",
                Size = new Size(120, 20),
                Location = new Point(20, yPos),
                ForeColor = DarkTheme.TextColor,
                Font = DarkTheme.GetDefaultFont()
            };

            txtName = new TextBox
            {
                Size = new Size(320, 25),
                Location = new Point(150, yPos - 3),
                Font = DarkTheme.GetDefaultFont()
            };

            this.Controls.AddRange(new Control[] { lblName, txtName });
            yPos += 40;

            // Collection URL
            lblUrl = new Label
            {
                Text = "Collection URL:",
                Size = new Size(120, 20),
                Location = new Point(20, yPos),
                ForeColor = DarkTheme.TextColor,
                Font = DarkTheme.GetDefaultFont()
            };

            txtUrl = new TextBox
            {
                Size = new Size(320, 25),
                Location = new Point(150, yPos - 3),
                Font = DarkTheme.GetDefaultFont(),
                PlaceholderText = "https://example.com/collection.yml"
            };

            this.Controls.AddRange(new Control[] { lblUrl, txtUrl });
            yPos += 40;

            // Collection Poster
            lblPoster = new Label
            {
                Text = "Collection Poster:",
                Size = new Size(120, 20),
                Location = new Point(20, yPos),
                ForeColor = DarkTheme.TextColor,
                Font = DarkTheme.GetDefaultFont()
            };

            txtPoster = new TextBox
            {
                Size = new Size(240, 25),
                Location = new Point(150, yPos - 3),
                Font = DarkTheme.GetDefaultFont(),
                PlaceholderText = "URL or local file path"
            };

            btnBrowsePoster = new Button
            {
                Text = "Browse",
                Size = new Size(70, 25),
                Location = new Point(400, yPos - 3)
            };
            btnBrowsePoster.Click += BtnBrowsePoster_Click;

            this.Controls.AddRange(new Control[] { lblPoster, txtPoster, btnBrowsePoster });
            yPos += 40;

            // Description
            lblDescription = new Label
            {
                Text = "Description:",
                Size = new Size(120, 20),
                Location = new Point(20, yPos),
                ForeColor = DarkTheme.TextColor,
                Font = DarkTheme.GetDefaultFont()
            };

            txtDescription = new TextBox
            {
                Size = new Size(320, 60),
                Location = new Point(150, yPos - 3),
                Font = DarkTheme.GetDefaultFont(),
                Multiline = true,
                PlaceholderText = "Optional description for this collection"
            };

            this.Controls.AddRange(new Control[] { lblDescription, txtDescription });
            yPos += 80;

            // Additional Variables Section (expandable for future use)
            grpAdditionalVariables = new GroupBox
            {
                Text = "Additional Variables (Future Use)",
                Size = new Size(450, 80),
                Location = new Point(20, yPos),
                ForeColor = DarkTheme.TextColor,
                Font = DarkTheme.GetDefaultFont()
            };

            lblPlaceholder = new Label
            {
                Text = "Additional template variables and custom settings will be available in future updates.",
                Size = new Size(420, 40),
                Location = new Point(10, 25),
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9F)
            };

            grpAdditionalVariables.Controls.Add(lblPlaceholder);
            this.Controls.Add(grpAdditionalVariables);
            yPos += 90;

            // Dialog buttons
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

            // Apply dark theme to get proper styling
            DarkTheme.ApplyDarkTheme(this);
        }

        private void LoadData()
        {
            if (isEditMode && customCollection != null)
            {
                txtName.Text = customCollection.Name ?? string.Empty;
                txtUrl.Text = customCollection.Url ?? string.Empty;
                txtPoster.Text = customCollection.Poster ?? string.Empty;
                txtDescription.Text = customCollection.Description ?? string.Empty;
            }
        }

        private void BtnBrowsePoster_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|All Files|*.*";
                openFileDialog.Title = "Select Collection Poster Image";
                
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtPoster.Text = openFileDialog.FileName;
                }
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Collection Name is required.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(txtUrl.Text))
            {
                MessageBox.Show("Collection URL is required.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUrl.Focus();
                return;
            }

            // Validate URL format
            if (!IsValidUrl(txtUrl.Text))
            {
                MessageBox.Show("Please enter a valid URL.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUrl.Focus();
                return;
            }

            // Validate poster path/URL if provided
            if (!string.IsNullOrWhiteSpace(txtPoster.Text) && !IsValidPosterPath(txtPoster.Text))
            {
                MessageBox.Show("Please enter a valid poster URL or file path.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPoster.Focus();
                return;
            }

            // Update custom collection object
            customCollection.Name = txtName.Text.Trim();
            customCollection.Url = txtUrl.Text.Trim();
            customCollection.Poster = txtPoster.Text.Trim();
            customCollection.Description = txtDescription.Text.Trim();
            customCollection.LastModified = DateTime.Now;

            if (!isEditMode)
            {
                customCollection.Id = Guid.NewGuid().ToString();
                customCollection.CreatedDate = DateTime.Now;
            }
        }

        private bool IsValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out Uri result) && 
                   (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
        }

        private bool IsValidPosterPath(string path)
        {
            // Check if it's a valid URL
            if (IsValidUrl(path))
                return true;

            // Check if it's a valid file path
            try
            {
                return File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        public CustomCollection GetCustomCollection()
        {
            return customCollection;
        }
    }

}