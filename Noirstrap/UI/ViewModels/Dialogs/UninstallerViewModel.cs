using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

using Noirstrap.Resources;

namespace Noirstrap.UI.ViewModels.Dialogs
{
    public class UninstallerViewModel
    {
        public string Text => String.Format(
            Strings.Uninstaller_Text, 
            "https://github.com/noirstraplabs/noirstrap/wiki/Roblox-crashes-or-does-not-launch",
            Paths.Base
        );

        public bool KeepData { get; set; } = true;

        public ICommand ConfirmUninstallCommand => new RelayCommand(ConfirmUninstall);

        public event EventHandler? ConfirmUninstallRequest;

        private void ConfirmUninstall() => ConfirmUninstallRequest?.Invoke(this, new EventArgs());
    }
}
