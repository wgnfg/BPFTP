using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public partial class NormalPopupViewModel : PopupViewModelBase
    {
        [ObservableProperty]
        public string message;
    }
}
