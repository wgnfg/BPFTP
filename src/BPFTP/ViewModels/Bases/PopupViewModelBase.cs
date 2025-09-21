using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public partial class PopupViewModelBase : ViewModelBase
    {
        [ObservableProperty]
        private bool _isVisible = false;
    }
}
