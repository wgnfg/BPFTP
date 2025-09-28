using BPFTP.Services;
using BPFTP.Utils;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public partial class DialogViewModelBase(Func<Task>? onConfirmOption = null, Func<Task>? onCancelOption = null):ViewModelBase
    {
        internal TaskCompletionSource DialogCloseTCS = new();
        private readonly Func<Task> OnConfrimOption = onConfirmOption ?? GlobalDefines.DummyTask;
        private readonly Func<Task> OnCancelOption = onCancelOption ?? GlobalDefines.DummyTask;
        [RelayCommand]
        async Task OnConfirm()
        {
            await OnConfrimOption.Invoke();
            ViewService.HideDialog(this);
        }
        [RelayCommand]
        async Task OnCancel()
        {
            await OnCancelOption.Invoke();
            ViewService.HideDialog(this);
        }
    }
}
