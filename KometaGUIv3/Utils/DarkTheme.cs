using System;
using System.Drawing;
using System.Windows.Forms;

namespace KometaGUIv3.Utils
{
    public static class DarkTheme
    {
        // Dark theme color palette
        public static readonly Color BackgroundColor = Color.FromArgb(32, 32, 32);
        public static readonly Color PanelColor = Color.FromArgb(45, 45, 48);
        public static readonly Color ButtonColor = Color.FromArgb(62, 62, 66);
        public static readonly Color ButtonHoverColor = Color.FromArgb(82, 82, 86);
        public static readonly Color TextColor = Color.FromArgb(241, 241, 241);
        public static readonly Color AccentColor = Color.FromArgb(0, 122, 204);
        public static readonly Color AccentHoverColor = Color.FromArgb(28, 151, 234);
        public static readonly Color BorderColor = Color.FromArgb(63, 63, 70);
        public static readonly Color InputBackColor = Color.FromArgb(51, 51, 55);
        public static readonly Color SelectedColor = Color.FromArgb(51, 153, 255);

        public static void ApplyDarkTheme(Control control)
        {
            if (control is Form form)
            {
                form.BackColor = BackgroundColor;
                form.ForeColor = TextColor;
            }
            else if (control is Panel panel)
            {
                panel.BackColor = PanelColor;
                panel.ForeColor = TextColor;
            }
            else if (control is Button button)
            {
                ApplyButtonStyle(button);
            }
            else if (control is TextBox textBox)
            {
                ApplyTextBoxStyle(textBox);
            }
            else if (control is ComboBox comboBox)
            {
                ApplyComboBoxStyle(comboBox);
            }
            else if (control is CheckBox checkBox)
            {
                ApplyCheckBoxStyle(checkBox);
            }
            else if (control is RadioButton radioButton)
            {
                ApplyRadioButtonStyle(radioButton);
            }
            else if (control is Label label)
            {
                label.BackColor = Color.Transparent;
                label.ForeColor = TextColor;
            }
            else if (control is ListView listView)
            {
                ApplyListViewStyle(listView);
            }
            else if (control is TreeView treeView)
            {
                ApplyTreeViewStyle(treeView);
            }
            else if (control is TabControl tabControl)
            {
                ApplyTabControlStyle(tabControl);
            }
            else
            {
                control.BackColor = BackgroundColor;
                control.ForeColor = TextColor;
            }

            // Recursively apply theme to child controls
            foreach (Control childControl in control.Controls)
            {
                ApplyDarkTheme(childControl);
            }
        }

        private static void ApplyButtonStyle(Button button)
        {
            button.BackColor = ButtonColor;
            button.ForeColor = TextColor;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = BorderColor;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.MouseOverBackColor = ButtonHoverColor;

            // Special styling for primary buttons
            if (button.Name.Contains("Primary") || button.Name.Contains("Next") || button.Name.Contains("Go"))
            {
                button.BackColor = AccentColor;
                button.FlatAppearance.MouseOverBackColor = AccentHoverColor;
            }
        }

        private static void ApplyTextBoxStyle(TextBox textBox)
        {
            textBox.BackColor = InputBackColor;
            textBox.ForeColor = TextColor;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void ApplyComboBoxStyle(ComboBox comboBox)
        {
            comboBox.BackColor = InputBackColor;
            comboBox.ForeColor = TextColor;
            comboBox.FlatStyle = FlatStyle.Flat;
        }

        private static void ApplyCheckBoxStyle(CheckBox checkBox)
        {
            checkBox.BackColor = Color.Transparent;
            checkBox.ForeColor = TextColor;
            checkBox.FlatStyle = FlatStyle.Standard;
            checkBox.FlatAppearance.BorderColor = BorderColor;
            checkBox.FlatAppearance.CheckedBackColor = AccentColor;
        }

        private static void ApplyRadioButtonStyle(RadioButton radioButton)
        {
            radioButton.BackColor = Color.Transparent;
            radioButton.ForeColor = TextColor;
            radioButton.FlatStyle = FlatStyle.Flat;
            radioButton.FlatAppearance.BorderColor = BorderColor;
            radioButton.FlatAppearance.CheckedBackColor = AccentColor;
        }

        private static void ApplyListViewStyle(ListView listView)
        {
            listView.BackColor = InputBackColor;
            listView.ForeColor = TextColor;
            listView.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void ApplyTreeViewStyle(TreeView treeView)
        {
            treeView.BackColor = InputBackColor;
            treeView.ForeColor = TextColor;
            treeView.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void ApplyTabControlStyle(TabControl tabControl)
        {
            tabControl.BackColor = PanelColor;
            tabControl.ForeColor = TextColor;
            
            foreach (TabPage tabPage in tabControl.TabPages)
            {
                tabPage.BackColor = BackgroundColor;
                tabPage.ForeColor = TextColor;
            }
        }

        public static Font GetDefaultFont()
        {
            return new Font("Segoe UI", 9F, FontStyle.Regular);
        }

        public static Font GetHeaderFont()
        {
            return new Font("Segoe UI", 12F, FontStyle.Bold);
        }

        public static Font GetTitleFont()
        {
            return new Font("Segoe UI", 16F, FontStyle.Bold);
        }
    }
}