using System.Windows.Input;

using Noirstrap.UI.Utility;
using Noirstrap.UI.ViewModels.Settings;

namespace Noirstrap.UI.Elements.Settings.Pages
{
    public partial class VersionPage
    {
        public VersionPage()
        {
            DataContext = new VersionViewModel();
            InitializeComponent();
        }

        private void ComboBox_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
            => ComboBoxScrollFix.HandleWheel(sender, e);
    }
}

