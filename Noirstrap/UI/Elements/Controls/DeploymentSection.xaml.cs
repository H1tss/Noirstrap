using System.Windows.Controls;

using Noirstrap.UI.ViewModels.Settings;

namespace Noirstrap.UI.Elements.Controls
{
    public partial class DeploymentSection : UserControl
    {
        public DeploymentSection()
        {
            DataContext = new ChannelViewModel();
            InitializeComponent();
        }
    }
}
