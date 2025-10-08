using BPFTP.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public partial class ShellViewModel : ViewModelBase
    {
        [ObservableProperty]
        ViewModelBase _content = App.ServiceProvider!.GetService<SftpWorkspaceViewModel>()!;

        [ObservableProperty]
        private bool _isDialogVisible;
        [ObservableProperty]
        public ViewModelBase _leftContent;
        public ObservableCollection<PopupViewModelBase> Popups { get; } = [];
        public ObservableCollection<ViewModelBase> Dialogs { get; } = [];

        public ShellViewModel(IViewService viewService)
        {
            viewService.RegisterShell(this);
            Dialogs.CollectionChanged += OnDialogsChanged;
        }

        private void OnDialogsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            IsDialogVisible = Dialogs.Count > 0;
        }
    }
}
