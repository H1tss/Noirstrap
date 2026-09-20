using System.Drawing;

namespace Noirstrap.Extensions
{
    static class BootstrapperIconEx
    {
        public static IReadOnlyCollection<BootstrapperIcon> Selections => new BootstrapperIcon[]
        {
            BootstrapperIcon.IconNoirstrap,
            BootstrapperIcon.IconNoirstrapAlt,
            BootstrapperIcon.Icon2022,
            BootstrapperIcon.Icon2019,
            BootstrapperIcon.Icon2017,
            BootstrapperIcon.IconLate2015,
            BootstrapperIcon.IconEarly2015,
            BootstrapperIcon.Icon2011,
            BootstrapperIcon.Icon2008,
            BootstrapperIcon.IconNoirstrapClassic,
            BootstrapperIcon.IconCustom
        };

        public static Icon GetIcon(this BootstrapperIcon icon)
        {
            const string LOG_IDENT = "BootstrapperIconEx::GetIcon";

            if (icon == BootstrapperIcon.IconCustom)
            {
                Icon? customIcon = null;
                string location = App.Settings.Prop.BootstrapperIconCustomLocation;

                if (String.IsNullOrEmpty(location))
                {
                    App.Logger.WriteLine(LOG_IDENT, "Warning: custom icon is not set.");
                }
                else
                {
                    try
                    {
                        customIcon = new Icon(location);
                    }
                    catch (Exception ex)
                    {
                        App.Logger.WriteLine(LOG_IDENT, $"Failed to load custom icon!");
                        App.Logger.WriteException(LOG_IDENT, ex);
                    }
                }

                return customIcon ?? Properties.Resources.IconNoirstrap;
            }

            return icon switch
            {
                BootstrapperIcon.IconNoirstrap => GetBrandIcon(),
                BootstrapperIcon.IconNoirstrapAlt => GetBrandIcon(),
                BootstrapperIcon.Icon2008 => Properties.Resources.Icon2008,
                BootstrapperIcon.Icon2011 => Properties.Resources.Icon2011,
                BootstrapperIcon.IconEarly2015 => Properties.Resources.IconEarly2015,
                BootstrapperIcon.IconLate2015 => Properties.Resources.IconLate2015,
                BootstrapperIcon.Icon2017 => Properties.Resources.Icon2017,
                BootstrapperIcon.Icon2019 => Properties.Resources.Icon2019,
                BootstrapperIcon.Icon2022 => Properties.Resources.Icon2022,
                BootstrapperIcon.IconNoirstrapClassic => Properties.Resources.IconNoirstrapClassic,
                _ => GetBrandIcon()
            };
        }

        private static Icon? _brandIcon;

        public static Icon GetBrandIcon()
        {
            if (_brandIcon is not null)
                return _brandIcon;

            try
            {
                var info = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/Noirstrap.ico"));
                if (info?.Stream is Stream stream)
                    using (stream)
                        _brandIcon = new Icon(stream);
            }
            catch (Exception ex)
            {
                App.Logger.WriteException("BootstrapperIconEx::GetBrandIcon", ex);
            }

            return _brandIcon ?? Properties.Resources.IconNoirstrap;
        }
    }
}
