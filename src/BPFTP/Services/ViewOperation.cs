using Avalonia.Threading;
using BPFTP.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.Services
{
    public static class ViewOperation
    {
        static IViewService _viewService;
        public static void Init()
        {
            _viewService = App.ServiceProvider!.GetRequiredService<IViewService>();
        }

        public static async Task ShowDialogAsync(DialogViewModelBase viewModel)
        {
            await _viewService.ShowDialogAsync(viewModel);
        }

        public static void ShowDialog(DialogViewModelBase viewModel)
        {
            _viewService.ShowDialog(viewModel);
        }

        public static void HideDialog(DialogViewModelBase viewModel)
        {
            _viewService.HideDialog(viewModel);
        }

        public static void ShowPopupShort(PopupViewModelBase viewModel, int autoCloseDelay = 2500) => ShowPopup(viewModel, autoCloseDelay);
        public static void ShowPopup(PopupViewModelBase viewModel, int autoCloseDelay = 0)
        {
            _viewService.ShowPopup(viewModel, autoCloseDelay);
        }

        public static void HidePopup(PopupViewModelBase viewModel)
        {
            _viewService?.HidePopup(viewModel);
        }

        public static IDisposable PopupScope(PopupViewModelBase viewModel, int autoCloseDelay = 0)
        {
            return _viewService.PopupScope(viewModel, autoCloseDelay);
        }
    }
    public class ViewService : IViewService
    {
        private ShellViewModel? _shellViewModel;

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

        public void ShowPopupShort(PopupViewModelBase viewModel, int autoCloseDelay = 2500) => ShowPopup(viewModel, autoCloseDelay);
        public void ShowPopup(PopupViewModelBase viewModel, int autoCloseDelay = 0)
        {
            if (_shellViewModel == null) return;
            if (!_shellViewModel.Popups.Contains(viewModel))
            {
                _shellViewModel.Popups.Add(viewModel);
            }
            Task.Delay(100).ContinueWith(_ => viewModel.IsVisible = true);
            if (autoCloseDelay > 0)
            {
                Task.Delay(autoCloseDelay).ContinueWith(_ => HidePopup(viewModel));
            }
        }

        public async Task HidePopup(PopupViewModelBase viewModel)
        {
            if (_shellViewModel == null) return;

            viewModel.IsVisible = false;
            await Task.Delay(300);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _shellViewModel.Popups.Remove(viewModel);
            });
        }

        public IDisposable PopupScope(PopupViewModelBase viewModel, int autoCloseDelay = 0)
        {
            ShowPopup(viewModel, autoCloseDelay);
            return Disposable.Create(() => HidePopup(viewModel));
        }
    }

}
