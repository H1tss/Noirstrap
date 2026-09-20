using Noirstrap.UI.ViewModels.Settings;

namespace Noirstrap.UI.Elements.Settings.Pages
{
    public partial class VersionsManagerPage
    {
        public VersionsManagerPage()
        {
            DataContext = new VersionsManagerViewModel();
            InitializeComponent();
        }
    }
}

