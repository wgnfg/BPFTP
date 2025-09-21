using BPFTP.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace BPFTP.ViewModels
{
    public partial class EditConnectionViewModel : DialogViewModelBase
    {
        [ObservableProperty]
        private ConnectionProfile _profile;

        [ObservableProperty]
        private bool _isPasswordAuth;

        [ObservableProperty]
        private bool _isPrivateKeyAuth;

        public AuthroizeMethod[] AuthMethods { get; } = [ AuthroizeMethod.Password, AuthroizeMethod.SSH ];

        public Action<ConnectionProfile?>? CloseAction { get; set; }

        public EditConnectionViewModel(ConnectionProfile profile, Func<Task>? OnConfirm = null, Func<Task>? OnCancel = null):base(OnConfirm,OnCancel)
        {
            _profile = profile;
            UpdateAuthVisibility();
            _profile.PropertyChanged += OnProfilePropertyChanged;
        }

        private void OnProfilePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConnectionProfile.AuthMethod))
            {
                UpdateAuthVisibility();
            }
        }

        private void UpdateAuthVisibility()
        {
            IsPasswordAuth = Profile.AuthMethod == AuthroizeMethod.Password;
            IsPrivateKeyAuth = Profile.AuthMethod == AuthroizeMethod.SSH;
        }
    }
}
