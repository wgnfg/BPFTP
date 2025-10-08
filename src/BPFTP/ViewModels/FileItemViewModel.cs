using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public partial class FileItemViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _path = string.Empty;

        [ObservableProperty]
        private bool _isDirectory;

        [ObservableProperty]
        private long _size;

        [ObservableProperty]
        private bool _isSelected;
    }
    public interface IRemoteItem;
    public interface ILocalItem;
    public partial class RemoteFile : FileItemViewModel,IRemoteItem;
    public partial class RemoteFolder : FileItemViewModel,IRemoteItem;
    public partial class LocalFile : FileItemViewModel,ILocalItem;
    public partial class LocalFolder : FileItemViewModel, ILocalItem;
}
