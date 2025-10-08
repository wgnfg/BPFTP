
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactions.DragAndDrop;
using BPFTP.ViewModels;

namespace BPFTP.Handlers
{
    public class LocalDropHandler(SftpWorkspaceViewModel viewModel) : IDropHandler
    {
        private readonly SftpWorkspaceViewModel _viewModel = viewModel;

        public bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
        {
            if (sourceContext is FileItemViewModel)
            {
                return true;
            }
            return false;
        }

        public bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
        {
            if (sourceContext is RemoteFolder sourceFolder)
            {
                _viewModel.DownloadItemCommand.Execute(sourceFolder);
                return true;
            }else if(sourceContext is RemoteFile sourceFile)
            {
                _viewModel.DownloadItemCommand.Execute(sourceFile);
                return true;
            }
            return false;
        }

        public void Cancel(object? sender, DragEventArgs e)
        {
        }

        void IDropHandler.Enter(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
        {
            //throw new System.NotImplementedException();
        }

        void IDropHandler.Over(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
        {
            //throw new System.NotImplementedException();
        }

        void IDropHandler.Drop(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
        {
            if (Validate(sender, e, sourceContext, targetContext,null))
            {
                Execute(sender, e, sourceContext, targetContext, null);
            }
            //throw new System.NotImplementedException();
        }

        void IDropHandler.Leave(object? sender, RoutedEventArgs e)
        {
            //throw new System.NotImplementedException();
        }

        void IDropHandler.Cancel(object? sender, RoutedEventArgs e)
        {
            //throw new System.NotImplementedException();
        }
    }
}
