using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Oblivion.GUI.MVVM.Model;
using Oblivion.GUI.Services;

namespace Oblivion.GUI.UI.Dialogs
{
    public partial class ThemeSettingsDialog : Window
    {
        private readonly ThemeService _themeService;
        private ThemePalette _editingPalette;
        private ThemePalette? _selectedPreset;

        private static readonly (string PropertyName, string Label)[] ColorFields =
        [
            ("BgColor", "Background"),
            ("SidebarColor", "Sidebar"),
            ("CardColor", "Card"),
            ("BorderColor", "Border"),
            ("TextColor", "Text"),
            ("TextSecondaryColor", "Text Secondary"),
            ("TextMutedColor", "Text Muted"),
            ("AccentColor", "Accent"),
            ("GreenColor", "Green"),
            ("RedColor", "Red"),
            ("OrangeColor", "Orange"),
            ("TealColor", "Teal")
        ];

        private readonly Dictionary<string, (Rectangle Preview, TextBox HexBox)> _editors = new();

        public ThemeSettingsDialog(ThemeService themeService)
        {
            _themeService = themeService;
            _editingPalette = themeService.CurrentPalette.Clone();

            InitializeComponent();
            BuildColorEditors();
            PopulatePresets();
        }

        private void PopulatePresets()
        {
            PresetComboBox.Items.Clear();
            foreach (var p in _themeService.Palettes)
                PresetComboBox.Items.Add(p);

            // Select the matching preset, or first item
            var match = _themeService.Palettes.FirstOrDefault(
                p => p.Name == _themeService.CurrentPalette.Name);
            if (match != null)
                PresetComboBox.SelectedItem = match;
            else
                PresetComboBox.SelectedIndex = 0;
        }

        private void BuildColorEditors()
        {
            ColorEditorsPanel.Children.Clear();
            _editors.Clear();

            foreach (var (propName, label) in ColorFields)
            {
                var color = GetColor(_editingPalette, propName);

                var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Label
                var lbl = new TextBlock
                {
                    Text = label,
                    Foreground = (Brush)FindResource("OblivionTextSecondary"),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12
                };
                Grid.SetColumn(lbl, 0);
                row.Children.Add(lbl);

                // Color preview
                var preview = new Rectangle
                {
                    Width = 24,
                    Height = 24,
                    RadiusX = 4,
                    RadiusY = 4,
                    Fill = new SolidColorBrush(color),
                    Stroke = (Brush)FindResource("OblivionBorder"),
                    StrokeThickness = 1,
                    Margin = new Thickness(0, 0, 8, 0)
                };
                Grid.SetColumn(preview, 1);
                row.Children.Add(preview);

                // Hex textbox
                var hexBox = new TextBox
                {
                    Text = ColorToHex(color),
                    FontSize = 12,
                    FontFamily = new FontFamily("Consolas"),
                    Background = (Brush)FindResource("OblivionCard"),
                    Foreground = (Brush)FindResource("OblivionText"),
                    BorderBrush = (Brush)FindResource("OblivionBorder"),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(6, 4, 6, 4),
                    VerticalAlignment = VerticalAlignment.Center,
                    MaxLength = 7,
                    Tag = propName
                };
                hexBox.LostFocus += HexBox_LostFocus;
                hexBox.KeyDown += HexBox_KeyDown;
                Grid.SetColumn(hexBox, 2);
                row.Children.Add(hexBox);

                _editors[propName] = (preview, hexBox);
                ColorEditorsPanel.Children.Add(row);
            }
        }

        private void HexBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                ApplyHexFromTextBox((TextBox)sender);
        }

        private void HexBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyHexFromTextBox((TextBox)sender);
        }

        private void ApplyHexFromTextBox(TextBox textBox)
        {
            var propName = (string)textBox.Tag;
            var hex = textBox.Text.Trim();

            if (!hex.StartsWith('#'))
                hex = "#" + hex;

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(hex);
                SetColor(_editingPalette, propName, color);
                textBox.Text = ColorToHex(color);

                if (_editors.TryGetValue(propName, out var editor))
                    editor.Preview.Fill = new SolidColorBrush(color);

                // Live preview
                _themeService.ApplyTheme(_editingPalette);
            }
            catch
            {
                // Revert to current value
                var current = GetColor(_editingPalette, propName);
                textBox.Text = ColorToHex(current);
            }
        }

        private void PresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PresetComboBox.SelectedItem is not ThemePalette preset)
                return;

            _selectedPreset = preset;
            _editingPalette = preset.Clone();

            // Update all editors
            foreach (var (propName, _) in ColorFields)
            {
                var color = GetColor(_editingPalette, propName);
                if (_editors.TryGetValue(propName, out var editor))
                {
                    editor.Preview.Fill = new SolidColorBrush(color);
                    editor.HexBox.Text = ColorToHex(color);
                }
            }

            // Live preview
            _themeService.ApplyTheme(_editingPalette);
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset to the selected preset (or GitHub Dark)
            var preset = _selectedPreset ?? ThemePalette.GitHubDark;
            _editingPalette = preset.Clone();

            foreach (var (propName, _) in ColorFields)
            {
                var color = GetColor(_editingPalette, propName);
                if (_editors.TryGetValue(propName, out var editor))
                {
                    editor.Preview.Fill = new SolidColorBrush(color);
                    editor.HexBox.Text = ColorToHex(color);
                }
            }

            _themeService.ApplyTheme(_editingPalette);
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            _themeService.ApplyTheme(_editingPalette);
            _themeService.SaveToFile();
            Close();
        }

        private static string ColorToHex(Color c)
            => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private static Color GetColor(ThemePalette p, string prop) => prop switch
        {
            "BgColor" => p.BgColor,
            "SidebarColor" => p.SidebarColor,
            "CardColor" => p.CardColor,
            "BorderColor" => p.BorderColor,
            "TextColor" => p.TextColor,
            "TextSecondaryColor" => p.TextSecondaryColor,
            "TextMutedColor" => p.TextMutedColor,
            "AccentColor" => p.AccentColor,
            "GreenColor" => p.GreenColor,
            "RedColor" => p.RedColor,
            "OrangeColor" => p.OrangeColor,
            "TealColor" => p.TealColor,
            _ => Colors.Transparent
        };

        private static void SetColor(ThemePalette p, string prop, Color c)
        {
            switch (prop)
            {
                case "BgColor": p.BgColor = c; break;
                case "SidebarColor": p.SidebarColor = c; break;
                case "CardColor": p.CardColor = c; break;
                case "BorderColor": p.BorderColor = c; break;
                case "TextColor": p.TextColor = c; break;
                case "TextSecondaryColor": p.TextSecondaryColor = c; break;
                case "TextMutedColor": p.TextMutedColor = c; break;
                case "AccentColor": p.AccentColor = c; break;
                case "GreenColor": p.GreenColor = c; break;
                case "RedColor": p.RedColor = c; break;
                case "OrangeColor": p.OrangeColor = c; break;
                case "TealColor": p.TealColor = c; break;
            }
        }
    }
}
