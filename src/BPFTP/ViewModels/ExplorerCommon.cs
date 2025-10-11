using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using R3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public partial class ExplorerCommon : ViewModelBase
    {
        public List<FileItemViewModel> AllFolders = [];
        public List<FileItemViewModel> AllFiles = [];
        [ObservableProperty] private bool _isSelectMode = false;
        [ObservableProperty] private string _curPath = string.Empty;
        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private List<FileItemViewModel> _filteredFolders = [];
        [ObservableProperty] private List<FileItemViewModel> _filteredFiles = [];
        [RelayCommand]
        private void GoUp()
        {
            if (!string.IsNullOrEmpty(CurPath))
            {
                var parent = Path.GetDirectoryName(CurPath)?.Replace('\\', '/');
                if (parent != null)
                {
                    CurPath = parent;
                }
            }
        }

        [RelayCommand]
        public void NavigateInto(FileItemViewModel item)
        {
            if (item != null && item.IsDirectory)
            {
                CurPath = item.Path;
            }
        }

        [RelayCommand]
        public Task Refresh() => UpdateFoldersAndFiles(CurPath);
        public Func<string, Task> UpdateFoldersAndFiles { get; set; } = (_) => Task.CompletedTask;
        public ExplorerCommon()
        {
            Observable.EveryValueChanged(this, x => x.CurPath)
                .Debounce(TimeSpan.FromMicroseconds(300))
                .Subscribe(async x => await UpdateFoldersAndFiles(x));
            Observable.EveryValueChanged(this, x => x.SearchText)
                .Debounce(TimeSpan.FromMilliseconds(300))
                .Subscribe(_ => FilterView());
        }
        public void FilterView()
        {
            FilteredFolders = [.. AllFolders.Where(i => i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))];
            FilteredFiles = [.. AllFiles.Where(i => i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))];
        }

    }

    public partial class RemoteExplorer:ExplorerCommon;
    public partial class LocalExplorer:ExplorerCommon;
}
