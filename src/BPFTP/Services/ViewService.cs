using Avalonia.Threading;
using BPFTP.ViewModels;
using R3;
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

        public static Task ShowDialogAsync(DialogViewModelBase viewModel)
        {
            if (Instance._shellViewModel == null)
            {
                return Task.CompletedTask;
            }
            ShowDialog(viewModel);
            return viewModel.DialogCloseTCS.Task;
        }

        public static void ShowDialog(DialogViewModelBase viewModel)
        {
            if (Instance._shellViewModel == null) return;
            if (!Instance._shellViewModel.Dialogs.Contains(viewModel))
            {
                Instance._shellViewModel.Dialogs.Add(viewModel);
            }
        }

        public static void HideDialog(DialogViewModelBase viewModel)
        {
            if (Instance._shellViewModel == null) return;
            if (Instance._shellViewModel.Dialogs.Contains(viewModel))
            {
                Instance._shellViewModel.Dialogs.Remove(viewModel);
                viewModel.DialogCloseTCS.SetResult();
            }
        }

        public static void ShowPopupShort(PopupViewModelBase viewModel, int autoCloseDelay = 2500) => ShowPopup(viewModel, autoCloseDelay);
        public static void ShowPopup(PopupViewModelBase viewModel,int autoCloseDelay = 0)
        {
            if (Instance._shellViewModel == null) return;
            if (!Instance._shellViewModel.Popups.Contains(viewModel))
            {
                Instance._shellViewModel.Popups.Add(viewModel);
            }
            Task.Delay(100).ContinueWith(_ =>  viewModel.IsVisible = true);
            if (autoCloseDelay > 0)
            {
                Task.Delay(autoCloseDelay).ContinueWith(_ => HidePopup(viewModel));
            }
        }

        public static async void HidePopup(PopupViewModelBase viewModel)
        {
            if (Instance._shellViewModel == null) return;

            viewModel.IsVisible = false;
            await Task.Delay(300);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Instance._shellViewModel.Popups.Remove(viewModel);
            });
        }

        public static IDisposable PopupScope(PopupViewModelBase viewModel, int autoCloseDelay = 0)
        {
            ShowPopup(viewModel, autoCloseDelay);
            return Disposable.Create(() => HidePopup(viewModel));
        }
    }

}
