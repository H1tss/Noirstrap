using System.Windows;
using System.Windows.Media;

namespace Noirstrap.UI.Theming
{
    public static class AccentTheme
    {
        public static readonly Color DefaultPrimary = Color.FromRgb(0xFF, 0xFF, 0xFF);

        private static T Settings<T>(Func<Noirstrap.Models.Persistable.Settings, T> read, T fallback)
        {
            try
            {
                var settings = App.Settings?.Prop;

                if (settings is not null)
                    return read(settings);
            }
            catch (Exception)
            {
            }

            return fallback;
        }

        public static Color Primary => Parse(Settings(x => x.AccentColor, "#FFFFFF"), DefaultPrimary);

        public static Color Parse(string? value, Color fallback)
        {
            if (String.IsNullOrWhiteSpace(value))
                return fallback;

            try
            {
                value = value.Trim();

                if (!value.StartsWith('#'))
                    value = "#" + value;

                if (ColorConverter.ConvertFromString(value) is Color color)
                {
                    if (color.A == 0)
                        color.A = 255;

                    return color;
                }
            }
            catch (Exception)
            {
            }

            return fallback;
        }

        public static string ToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        public static Brush MakeBrush()
        {
            var brush = new SolidColorBrush(Primary);
            brush.Freeze();

            return brush;
        }
    }
}
