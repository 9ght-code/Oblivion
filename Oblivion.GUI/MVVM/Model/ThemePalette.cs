using System.Windows.Media;

namespace Oblivion.GUI.MVVM.Model
{
    public class ThemePalette
    {
        public string Name { get; set; } = "";
        public Color BgColor { get; set; }
        public Color SidebarColor { get; set; }
        public Color CardColor { get; set; }
        public Color BorderColor { get; set; }
        public Color TextColor { get; set; }
        public Color TextSecondaryColor { get; set; }
        public Color TextMutedColor { get; set; }
        public Color AccentColor { get; set; }
        public Color GreenColor { get; set; }
        public Color RedColor { get; set; }
        public Color OrangeColor { get; set; }
        public Color TealColor { get; set; }
        public Color GlowColor { get; set; }
        public Color AccentSecondaryColor { get; set; }

        public ThemePalette Clone() => new()
        {
            Name = Name,
            BgColor = BgColor,
            SidebarColor = SidebarColor,
            CardColor = CardColor,
            BorderColor = BorderColor,
            TextColor = TextColor,
            TextSecondaryColor = TextSecondaryColor,
            TextMutedColor = TextMutedColor,
            AccentColor = AccentColor,
            GreenColor = GreenColor,
            RedColor = RedColor,
            OrangeColor = OrangeColor,
            TealColor = TealColor,
            GlowColor = GlowColor,
            AccentSecondaryColor = AccentSecondaryColor
        };

        public static ThemePalette CyberpunkNeon => new()
        {
            Name = "Cyberpunk Neon",
            BgColor = (Color)ColorConverter.ConvertFromString("#0a0e17"),
            SidebarColor = (Color)ColorConverter.ConvertFromString("#060a12"),
            CardColor = (Color)ColorConverter.ConvertFromString("#111827"),
            BorderColor = (Color)ColorConverter.ConvertFromString("#1e293b"),
            TextColor = (Color)ColorConverter.ConvertFromString("#e2e8f0"),
            TextSecondaryColor = (Color)ColorConverter.ConvertFromString("#94a3b8"),
            TextMutedColor = (Color)ColorConverter.ConvertFromString("#475569"),
            AccentColor = (Color)ColorConverter.ConvertFromString("#00f0ff"),
            GreenColor = (Color)ColorConverter.ConvertFromString("#00ff88"),
            RedColor = (Color)ColorConverter.ConvertFromString("#ff2d55"),
            OrangeColor = (Color)ColorConverter.ConvertFromString("#ff9500"),
            TealColor = (Color)ColorConverter.ConvertFromString("#00ff88"),
            GlowColor = (Color)ColorConverter.ConvertFromString("#00f0ff"),
            AccentSecondaryColor = (Color)ColorConverter.ConvertFromString("#bf00ff")
        };

        public static ThemePalette GitHubDark => new()
        {
            Name = "GitHub Dark",
            BgColor = (Color)ColorConverter.ConvertFromString("#0d1117"),
            SidebarColor = (Color)ColorConverter.ConvertFromString("#010409"),
            CardColor = (Color)ColorConverter.ConvertFromString("#161b22"),
            BorderColor = (Color)ColorConverter.ConvertFromString("#30363d"),
            TextColor = (Color)ColorConverter.ConvertFromString("#c9d1d9"),
            TextSecondaryColor = (Color)ColorConverter.ConvertFromString("#8b949e"),
            TextMutedColor = (Color)ColorConverter.ConvertFromString("#484f58"),
            AccentColor = (Color)ColorConverter.ConvertFromString("#58a6ff"),
            GreenColor = (Color)ColorConverter.ConvertFromString("#3fb950"),
            RedColor = (Color)ColorConverter.ConvertFromString("#f85149"),
            OrangeColor = (Color)ColorConverter.ConvertFromString("#d29922"),
            TealColor = (Color)ColorConverter.ConvertFromString("#39d353"),
            GlowColor = (Color)ColorConverter.ConvertFromString("#58a6ff"),
            AccentSecondaryColor = (Color)ColorConverter.ConvertFromString("#bc8cff")
        };

        public static ThemePalette Monokai => new()
        {
            Name = "Monokai",
            BgColor = (Color)ColorConverter.ConvertFromString("#272822"),
            SidebarColor = (Color)ColorConverter.ConvertFromString("#1e1f1c"),
            CardColor = (Color)ColorConverter.ConvertFromString("#3e3d32"),
            BorderColor = (Color)ColorConverter.ConvertFromString("#49483e"),
            TextColor = (Color)ColorConverter.ConvertFromString("#f8f8f2"),
            TextSecondaryColor = (Color)ColorConverter.ConvertFromString("#a6a28c"),
            TextMutedColor = (Color)ColorConverter.ConvertFromString("#75715e"),
            AccentColor = (Color)ColorConverter.ConvertFromString("#66d9ef"),
            GreenColor = (Color)ColorConverter.ConvertFromString("#a6e22e"),
            RedColor = (Color)ColorConverter.ConvertFromString("#f92672"),
            OrangeColor = (Color)ColorConverter.ConvertFromString("#e6db74"),
            TealColor = (Color)ColorConverter.ConvertFromString("#a6e22e"),
            GlowColor = (Color)ColorConverter.ConvertFromString("#66d9ef"),
            AccentSecondaryColor = (Color)ColorConverter.ConvertFromString("#f92672")
        };

        public static ThemePalette Nord => new()
        {
            Name = "Nord",
            BgColor = (Color)ColorConverter.ConvertFromString("#2e3440"),
            SidebarColor = (Color)ColorConverter.ConvertFromString("#242933"),
            CardColor = (Color)ColorConverter.ConvertFromString("#3b4252"),
            BorderColor = (Color)ColorConverter.ConvertFromString("#4c566a"),
            TextColor = (Color)ColorConverter.ConvertFromString("#eceff4"),
            TextSecondaryColor = (Color)ColorConverter.ConvertFromString("#d8dee9"),
            TextMutedColor = (Color)ColorConverter.ConvertFromString("#616e88"),
            AccentColor = (Color)ColorConverter.ConvertFromString("#88c0d0"),
            GreenColor = (Color)ColorConverter.ConvertFromString("#a3be8c"),
            RedColor = (Color)ColorConverter.ConvertFromString("#bf616a"),
            OrangeColor = (Color)ColorConverter.ConvertFromString("#d08770"),
            TealColor = (Color)ColorConverter.ConvertFromString("#8fbcbb"),
            GlowColor = (Color)ColorConverter.ConvertFromString("#88c0d0"),
            AccentSecondaryColor = (Color)ColorConverter.ConvertFromString("#b48ead")
        };

        public static ThemePalette SolarizedDark => new()
        {
            Name = "Solarized Dark",
            BgColor = (Color)ColorConverter.ConvertFromString("#002b36"),
            SidebarColor = (Color)ColorConverter.ConvertFromString("#001e26"),
            CardColor = (Color)ColorConverter.ConvertFromString("#073642"),
            BorderColor = (Color)ColorConverter.ConvertFromString("#586e75"),
            TextColor = (Color)ColorConverter.ConvertFromString("#839496"),
            TextSecondaryColor = (Color)ColorConverter.ConvertFromString("#657b83"),
            TextMutedColor = (Color)ColorConverter.ConvertFromString("#586e75"),
            AccentColor = (Color)ColorConverter.ConvertFromString("#268bd2"),
            GreenColor = (Color)ColorConverter.ConvertFromString("#859900"),
            RedColor = (Color)ColorConverter.ConvertFromString("#dc322f"),
            OrangeColor = (Color)ColorConverter.ConvertFromString("#b58900"),
            TealColor = (Color)ColorConverter.ConvertFromString("#2aa198"),
            GlowColor = (Color)ColorConverter.ConvertFromString("#268bd2"),
            AccentSecondaryColor = (Color)ColorConverter.ConvertFromString("#6c71c4")
        };

        public static ThemePalette Light => new()
        {
            Name = "Light",
            BgColor = (Color)ColorConverter.ConvertFromString("#ffffff"),
            SidebarColor = (Color)ColorConverter.ConvertFromString("#f6f8fa"),
            CardColor = (Color)ColorConverter.ConvertFromString("#f0f0f0"),
            BorderColor = (Color)ColorConverter.ConvertFromString("#d0d7de"),
            TextColor = (Color)ColorConverter.ConvertFromString("#24292f"),
            TextSecondaryColor = (Color)ColorConverter.ConvertFromString("#57606a"),
            TextMutedColor = (Color)ColorConverter.ConvertFromString("#8b949e"),
            AccentColor = (Color)ColorConverter.ConvertFromString("#0969da"),
            GreenColor = (Color)ColorConverter.ConvertFromString("#1a7f37"),
            RedColor = (Color)ColorConverter.ConvertFromString("#cf222e"),
            OrangeColor = (Color)ColorConverter.ConvertFromString("#9a6700"),
            TealColor = (Color)ColorConverter.ConvertFromString("#0550ae"),
            GlowColor = (Color)ColorConverter.ConvertFromString("#0969da"),
            AccentSecondaryColor = (Color)ColorConverter.ConvertFromString("#8250df")
        };

        public override string ToString() => Name;
    }
}
