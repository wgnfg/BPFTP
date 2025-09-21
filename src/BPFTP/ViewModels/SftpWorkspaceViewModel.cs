using BPFTP.Services;

namespace BPFTP.ViewModels
{
    public partial class SftpWorkspaceViewModel(DatabaseService databaseService) : ViewModelBase
    {
        private readonly DatabaseService _databaseService = databaseService;
    }
}
