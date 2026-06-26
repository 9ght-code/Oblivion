using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media;
using Oblivion.GUI.MVVM.Model;

namespace Oblivion.GUI.Services
{
    public class ThemeService
    {
        private static readonly string ThemeFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Oblivion", "theme.json");

        private static readonly Dictionary<string, string> BrushKeyMap = new()
        {
            ["BgColor"] = "OblivionBg",
            ["SidebarColor"] = "OblivionSidebar",
            ["CardColor"] = "OblivionCard",
            ["BorderColor"] = "OblivionBorder",
            ["TextColor"] = "OblivionText",
            ["TextSecondaryColor"] = "OblivionTextSecondary",
            ["TextMutedColor"] = "OblivionTextMuted",
            ["AccentColor"] = "OblivionAccent",
            ["GreenColor"] = "OblivionGreen",
            ["RedColor"] = "OblivionRed",
            ["OrangeColor"] = "OblivionOrange",
            ["TealColor"] = "OblivionTeal",
            ["GreenBgColor"] = "OblivionGreenBg",
            ["GreenBorderColor"] = "OblivionGreenBorder",
            ["OrangeBgColor"] = "OblivionOrangeBg",
            ["RedBgColor"] = "OblivionRedBg"
        };

        public List<ThemePalette> Palettes { get; } =
        [
            ThemePalette.CyberpunkNeon,
            ThemePalette.GitHubDark,
            ThemePalette.Monokai,
            ThemePalette.Nord,
            ThemePalette.SolarizedDark,
            ThemePalette.Light
        ];

        public ThemePalette CurrentPalette { get; private set; } = ThemePalette.CyberpunkNeon;

        public void ApplyTheme(ThemePalette palette)
        {
            CurrentPalette = palette;
            var resources = Application.Current.Resources;

            resources["OblivionBg"] = new SolidColorBrush(palette.BgColor);
            resources["OblivionSidebar"] = new SolidColorBrush(palette.SidebarColor);
            resources["OblivionCard"] = new SolidColorBrush(palette.CardColor);
            resources["OblivionBorder"] = new SolidColorBrush(palette.BorderColor);
            resources["OblivionText"] = new SolidColorBrush(palette.TextColor);
            resources["OblivionTextSecondary"] = new SolidColorBrush(palette.TextSecondaryColor);
            resources["OblivionTextMuted"] = new SolidColorBrush(palette.TextMutedColor);
            resources["OblivionAccent"] = new SolidColorBrush(palette.AccentColor);
            resources["OblivionGreen"] = new SolidColorBrush(palette.GreenColor);
            resources["OblivionRed"] = new SolidColorBrush(palette.RedColor);
            resources["OblivionOrange"] = new SolidColorBrush(palette.OrangeColor);
            resources["OblivionTeal"] = new SolidColorBrush(palette.TealColor);

            // Derived colors for status badge backgrounds
            resources["OblivionGreenBg"] = new SolidColorBrush(DeriveBackground(palette.GreenColor, palette.BgColor, 0.15));
            resources["OblivionGreenBorder"] = new SolidColorBrush(palette.GreenColor);
            resources["OblivionOrangeBg"] = new SolidColorBrush(DeriveBackground(palette.OrangeColor, palette.BgColor, 0.15));
            resources["OblivionRedBg"] = new SolidColorBrush(DeriveBackground(palette.RedColor, palette.BgColor, 0.15));
            resources["OblivionTealBg"] = new SolidColorBrush(DeriveBackground(palette.TealColor, palette.BgColor, 0.15));

            resources["OblivionGlow"] = new SolidColorBrush(palette.GlowColor);
            resources["OblivionGlowDim"] = new SolidColorBrush(Color.FromArgb(26, palette.GlowColor.R, palette.GlowColor.G, palette.GlowColor.B));
            resources["OblivionCardHover"] = new SolidColorBrush(DeriveBackground(palette.AccentColor, palette.CardColor, 0.08));
            resources["OblivionSurface"] = new SolidColorBrush(DeriveBackground(palette.CardColor, palette.BgColor, 0.5));
            resources["OblivionAccentSecondary"] = new SolidColorBrush(palette.AccentSecondaryColor);
        }

        public void SaveToFile()
        {
            var dir = Path.GetDirectoryName(ThemeFilePath)!;
            Directory.CreateDirectory(dir);

            var dto = new ThemeDto
            {
                Name = CurrentPalette.Name,
                Colors = new Dictionary<string, string>
                {
                    ["Bg"] = CurrentPalette.BgColor.ToString(),
                    ["Sidebar"] = CurrentPalette.SidebarColor.ToString(),
                    ["Card"] = CurrentPalette.CardColor.ToString(),
                    ["Border"] = CurrentPalette.BorderColor.ToString(),
                    ["Text"] = CurrentPalette.TextColor.ToString(),
                    ["TextSecondary"] = CurrentPalette.TextSecondaryColor.ToString(),
                    ["TextMuted"] = CurrentPalette.TextMutedColor.ToString(),
                    ["Accent"] = CurrentPalette.AccentColor.ToString(),
                    ["Green"] = CurrentPalette.GreenColor.ToString(),
                    ["Red"] = CurrentPalette.RedColor.ToString(),
                    ["Orange"] = CurrentPalette.OrangeColor.ToString(),
                    ["Teal"] = CurrentPalette.TealColor.ToString(),
                    ["Glow"] = CurrentPalette.GlowColor.ToString(),
                    ["AccentSecondary"] = CurrentPalette.AccentSecondaryColor.ToString()
                }
            };

            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ThemeFilePath, json);
        }

        public void LoadFromFile()
        {
            if (!File.Exists(ThemeFilePath))
            {
                ApplyTheme(ThemePalette.CyberpunkNeon);
                return;
            }

            try
            {
                var json = File.ReadAllText(ThemeFilePath);
                var dto = JsonSerializer.Deserialize<ThemeDto>(json);

                if (dto?.Colors == null)
                {
                    ApplyTheme(ThemePalette.CyberpunkNeon);
                    return;
                }

                var palette = new ThemePalette
                {
                    Name = dto.Name ?? "Custom",
                    BgColor = ParseColor(dto.Colors, "Bg", "#0d1117"),
                    SidebarColor = ParseColor(dto.Colors, "Sidebar", "#010409"),
                    CardColor = ParseColor(dto.Colors, "Card", "#161b22"),
                    BorderColor = ParseColor(dto.Colors, "Border", "#30363d"),
                    TextColor = ParseColor(dto.Colors, "Text", "#c9d1d9"),
                    TextSecondaryColor = ParseColor(dto.Colors, "TextSecondary", "#8b949e"),
                    TextMutedColor = ParseColor(dto.Colors, "TextMuted", "#484f58"),
                    AccentColor = ParseColor(dto.Colors, "Accent", "#58a6ff"),
                    GreenColor = ParseColor(dto.Colors, "Green", "#3fb950"),
                    RedColor = ParseColor(dto.Colors, "Red", "#f85149"),
                    OrangeColor = ParseColor(dto.Colors, "Orange", "#d29922"),
                    TealColor = ParseColor(dto.Colors, "Teal", "#00ff88"),
                    GlowColor = ParseColor(dto.Colors, "Glow", "#00f0ff"),
                    AccentSecondaryColor = ParseColor(dto.Colors, "AccentSecondary", "#bf00ff")
                };

                ApplyTheme(palette);
            }
            catch
            {
                ApplyTheme(ThemePalette.CyberpunkNeon);
            }
        }

        private static Color ParseColor(Dictionary<string, string> colors, string key, string fallback)
        {
            var hex = colors.TryGetValue(key, out var val) ? val : fallback;
            try
            {
                return (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                return (Color)ColorConverter.ConvertFromString(fallback);
            }
        }

        private static Color DeriveBackground(Color accent, Color bg, double ratio)
        {
            byte r = (byte)(bg.R + (accent.R - bg.R) * ratio);
            byte g = (byte)(bg.G + (accent.G - bg.G) * ratio);
            byte b = (byte)(bg.B + (accent.B - bg.B) * ratio);
            return Color.FromRgb(r, g, b);
        }

        private class ThemeDto
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }

            [JsonPropertyName("colors")]
            public Dictionary<string, string>? Colors { get; set; }
        }
    }
}
