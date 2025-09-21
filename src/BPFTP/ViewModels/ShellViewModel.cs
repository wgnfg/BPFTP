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

        private readonly ViewService _viewService;


        [ObservableProperty]
        private bool _isDialogVisible;

        public ObservableCollection<PopupViewModelBase> Popups { get; } = [];
        public ObservableCollection<ViewModelBase> Dialogs { get; } = [];

        public ShellViewModel(ViewService viewService)
        {
            _viewService = viewService;
            _viewService.RegisterShell(this);

            Dialogs.CollectionChanged += OnDialogsChanged;
        }

        private void OnDialogsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            IsDialogVisible = Dialogs.Count > 0;
        }
    }
}
