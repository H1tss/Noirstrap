using System.Windows;

using Noirstrap.UI.Elements.Base;
using Noirstrap.UI.ViewModels.Settings;
using Noirstrap.UI.Elements.Settings;

namespace Noirstrap.UI.Elements.Settings.Pages
{
    public partial class ConfigPage
    {
        public ConfigPage()
        {
            var viewModel = new ConfigViewModel();

            viewModel.ThemeChanged += (_, _) =>
            {
                if (Window.GetWindow(this) is WpfUiWindow window)
                    window.ApplyTheme();
            };

            viewModel.BackgroundChanged += (_, _) =>
            {
                if (Window.GetWindow(this) is MainWindow window)
                    window.ApplyBackgroundGif();
            };

            DataContext = viewModel;
            InitializeComponent();
        }
    }
}
