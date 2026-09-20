using Noirstrap.UI.ViewModels.Settings;

namespace Noirstrap.UI.Elements.Settings.Pages
{
    /// <summary>
    /// Interaction logic for ModsPage.xaml
    /// </summary>
    public partial class ModsPage
    {
        public ModsPage()
        {
            DataContext = new ModsViewModel();
            InitializeComponent();
        }
    }
}
