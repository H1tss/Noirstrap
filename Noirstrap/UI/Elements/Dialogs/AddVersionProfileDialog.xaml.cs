using Noirstrap.Models.Persistable;
using Noirstrap.UI.ViewModels.Dialogs;

namespace Noirstrap.UI.Elements.Dialogs
{
    public partial class AddVersionProfileDialog
    {
        public VersionProfile? CreatedProfile { get; private set; }

        public AddVersionProfileDialog()
        {
            var vm = new AddVersionProfileViewModel();
            vm.CloseRequested += (_, profile) =>
            {
                CreatedProfile = profile;
                Close();
            };
            DataContext = vm;
            InitializeComponent();
        }
    }
}

