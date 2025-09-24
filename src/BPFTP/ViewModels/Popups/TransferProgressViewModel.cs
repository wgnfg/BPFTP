using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public readonly record struct DownloadProgress(string Name, double Percentage);
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
        private string _errorMessage = string.Empty;



        public bool IsError => !string.IsNullOrEmpty(ErrorMessage);
    }
}
