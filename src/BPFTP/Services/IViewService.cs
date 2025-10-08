using BPFTP.ViewModels;
using System;
using System.Threading.Tasks;

namespace BPFTP.Services
{
    public interface IViewService
    {
        void HideDialog(DialogViewModelBase viewModel);
        Task HidePopup(PopupViewModelBase viewModel);
        IDisposable PopupScope(PopupViewModelBase viewModel, int autoCloseDelay = 0);
        void RegisterShell(ShellViewModel vm);
        void ShowDialog(DialogViewModelBase viewModel);
        Task ShowDialogAsync(DialogViewModelBase viewModel);
        void ShowPopup(PopupViewModelBase viewModel, int autoCloseDelay = 0);
        void ShowPopupShort(PopupViewModelBase viewModel, int autoCloseDelay = 2500);
    }
}