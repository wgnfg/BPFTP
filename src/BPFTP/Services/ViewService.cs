using BPFTP.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.Services
{
    public class ViewService
    {
        public ViewService()
        {
            Instance = this;
        }
        private ShellViewModel? _shellViewModel;

        public static ViewService Instance { get; private set; }
        public void RegisterShell(ShellViewModel vm)
        {
            _shellViewModel = vm;
        }

        public Task ShowDialogAsync(DialogViewModelBase viewModel)
        {
            if (_shellViewModel == null)
            {
                return Task.CompletedTask;
            }
            ShowDialog(viewModel);
            return viewModel.DialogCloseTCS.Task;
        }

        public void ShowDialog(DialogViewModelBase viewModel)
        {
            if (_shellViewModel == null) return;
            if (!_shellViewModel.Dialogs.Contains(viewModel))
            {
                _shellViewModel.Dialogs.Add(viewModel);
            }
        }

        public void HideDialog(DialogViewModelBase viewModel)
        {
            if (_shellViewModel == null) return;
            if (_shellViewModel.Dialogs.Contains(viewModel))
            {
                _shellViewModel.Dialogs.Remove(viewModel);
                viewModel.DialogCloseTCS.SetResult();
            }
        }

        public void ShowPopup(PopupViewModelBase viewModel,int autoCloseDelay = 1000)
        {
            if (_shellViewModel == null) return;
            if (!_shellViewModel.Popups.Contains(viewModel))
            {
                _shellViewModel.Popups.Add(viewModel);
            }
            Task.Delay(100).ContinueWith(_ =>  viewModel.IsVisible = true);
            if (autoCloseDelay > 0)
            {
                Task.Delay(autoCloseDelay).ContinueWith(_ => viewModel.IsVisible = false);
            }
        }

        public async void HidePopup(PopupViewModelBase viewModel)
        {
            if (_shellViewModel == null) return;

            viewModel.IsVisible = false;
            // Wait for the animation to complete before removing
            await Task.Delay(300); // This duration should match the animation duration

            _shellViewModel.Popups.Remove(viewModel);
        }
    }

}
