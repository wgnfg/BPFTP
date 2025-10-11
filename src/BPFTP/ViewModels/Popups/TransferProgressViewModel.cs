using BPFTP.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using R3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public readonly record struct DownloadProgress(string Name, double Percentage, double Speed);
    public partial class TransferProgressViewModel : PopupViewModelBase
    {
        [ObservableProperty]
        private string _title = "正在下载...";

        [ObservableProperty]
        private string _fileName = string.Empty;

        [ObservableProperty]
        private string _currentFileName = string.Empty;

        [ObservableProperty]
        private int _currentFileIndex = 0;
        [ObservableProperty]
        private int _totalFileCount = 0;
        [ObservableProperty]
        private double _percentage;

        [ObservableProperty]
        private double _speed = 0;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsError))]
        private string _errorMessage = string.Empty;


        private readonly CancellationTokenSource CancellationTokenSource = new();
        public CancellationToken CancellationToken => CancellationTokenSource.Token;
        public bool IsError => !string.IsNullOrEmpty(ErrorMessage);

        [RelayCommand]
        public void Cancel()
        {
            CancellationTokenSource.Cancel();
            ErrorMessage = "已取消";
            Task.Delay(5000).ContinueWith(_ => ViewOperation.HidePopup(this));
        }
    }
}
