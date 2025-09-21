using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BPFTP.Models
{
    public enum AuthroizeMethod
    {
        Password,
        SSH
    }
    public partial class ConnectionProfile : ObservableObject
    {
        [ObservableProperty]
        [property: SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        private int _id;

        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private string _host = string.Empty;

        [ObservableProperty]
        private int _port = 22;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private AuthroizeMethod _authMethod = AuthroizeMethod.Password;

        [ObservableProperty]
        [property: SugarColumn(IsNullable = true)]
        private string? _password = string.Empty;

        [ObservableProperty]
        [property: SugarColumn(IsNullable = true)]
        private string? _privateKeyPath = string.Empty;

        [ObservableProperty]
        [property: SugarColumn(IsNullable = true)]
        private string? _privateKeyPassword = string.Empty;

        [ObservableProperty]
        [property: SugarColumn(IsNullable = true)]
        private string? _defaultRemotePath = string.Empty;

        [ObservableProperty]
        [property: SugarColumn(IsJson = true, IsNullable = true)]
        private string? _jsonExtra = string.Empty;

        public ConnectionProfile Clone()
        {
            return (ConnectionProfile)this.MemberwiseClone();
        }
    }
}
