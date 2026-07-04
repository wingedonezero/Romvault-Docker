using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace DarkAvalonia
{
    public static class dark
    {
        // Match Fluent Dark palette: SystemChromeLow/Medium/MediumLow
        public static Color bg0 = Color.FromRgb(0x17, 0x17, 0x17);   // #171717 SystemChromeLow
        public static Color bg = Color.FromRgb(0x1F, 0x1F, 0x1F);    // #1F1F1F SystemChromeMedium
        public static Color bg1 = Color.FromRgb(0x2B, 0x2B, 0x2B);   // #2B2B2B SystemChromeMediumLow
        public static Color fg = Color.FromRgb(0xFF, 0xFF, 0xFF);    // #FFFFFF SystemBaseHigh

        // Lazy-initialized to avoid creating Avalonia objects before platform init
        private static IBrush _sb_bg;
        private static IBrush _sb_bg1;
        private static IBrush _sb_fg;

        public static IBrush sb_bg => _sb_bg ??= new SolidColorBrush(bg);
        public static IBrush sb_bg1 => _sb_bg1 ??= new SolidColorBrush(bg1);
        public static IBrush sb_fg => _sb_fg ??= new SolidColorBrush(fg);

        public static bool darkEnabled;

        /// <summary>
        /// Returns true if the app is currently rendering in dark mode.
        /// Checks the actual resolved theme variant, not just the setting.
        /// </summary>
        public static bool IsDarkTheme =>
            Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

        public static void SetTheme(Application app, bool isDark)
        {
            darkEnabled = isDark;

            // When dark is enabled, force Dark theme (system detection is unreliable on Linux).
            // When dark is disabled, follow system theme (Default).
            app.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Default;
        }

        /// <summary>
        /// Convert a light-mode pastel status color to an appropriate dark-mode equivalent.
        /// Preserves the hue/tint direction while shifting to a dark range that's visible
        /// against Fluent Dark backgrounds (~#1F1F1F to #2B2B2B).
        /// </summary>
        public static Color ToDarkVariant(Color c)
        {
            // Scale to ~40-110 range to be clearly visible against Fluent Dark backgrounds
            byte r = (byte)Math.Clamp(c.R * 0.40, c.R > 0 ? 30 : 0, 110);
            byte g = (byte)Math.Clamp(c.G * 0.40, c.G > 0 ? 30 : 0, 110);
            byte b = (byte)Math.Clamp(c.B * 0.40, c.B > 0 ? 30 : 0, 110);
            return Color.FromRgb(r, g, b);
        }

        /// <summary>
        /// Returns the appropriate status color for the current theme.
        /// In dark mode, converts the light-mode color to a darker variant.
        /// </summary>
        public static Color StatusColor(Color lightColor)
        {
            return IsDarkTheme ? ToDarkVariant(lightColor) : lightColor;
        }

        public static Color bgColor(Color c)
        {
            return IsDarkTheme ? bg : c;
        }

        public static Color bgColor1(Color c)
        {
            return IsDarkTheme ? bg1 : c;
        }

        public static IBrush bgBrush(IBrush b)
        {
            return IsDarkTheme ? sb_bg : b;
        }

        public static IBrush bgBrush1(IBrush b)
        {
            return IsDarkTheme ? sb_bg1 : b;
        }

        public static IBrush fgBrush(IBrush b)
        {
            return IsDarkTheme ? sb_fg : b;
        }

        public static Color Down(Color c)
        {
            if (!IsDarkTheme)
                return c;

            return Color.FromArgb(255, (byte)(c.R * 0.8), (byte)(c.G * 0.8), (byte)(c.B * 0.8));
        }

        public static bool IsUnix
        {
            get
            {
                int p = (int)Environment.OSVersion.Platform;
                return ((p == 4) || (p == 6) || (p == 128));
            }
        }
    }
}
