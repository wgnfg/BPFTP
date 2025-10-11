
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Xaml.Interactions.DragAndDrop;
using BPFTP.ViewModels;
using Avalonia.Platform.Storage;
using Avalonia.Interactivity;

namespace BPFTP.Handlers
{
    public class RemoteDropHandler(SftpWorkspaceViewModel viewModel) : IDropHandler
    {
        private readonly SftpWorkspaceViewModel _viewModel = viewModel;

        public bool Validate(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
        {
            if (e.DataTransfer.Items is IEnumerable<IDataTransferItem>)
            {
                return true;
            }

            if (sourceContext is FileItemViewModel)
            {
                return true;
            }

            return false;
        }

        public bool Execute(object? sender, DragEventArgs e, object? sourceContext, object? targetContext, object? state)
        {
            if (sourceContext is FileItemViewModel sourceItem)
            {
                if (sourceItem is RemoteFile or RemoteFolder)
                {
                    // TODO:远程内部文件移动
                    return false;
                }
                else
                {
                    // 本地移动到远程
                    _viewModel.UploadItemCommand.Execute(sourceItem);
                    
                }
                return true;
            }
            if (e.DataTransfer.TryGetFiles() is IEnumerable<IStorageItem> files)
            {
                foreach (var file in files)
                {
                    var fileItem = new FileItemViewModel
                    {
                        Name = file.Name,
                        Path = file.Path.LocalPath,
                        IsDirectory = file is IStorageFolder
                    };

                    _viewModel.UploadItemCommand.Execute(fileItem);
                }
                return true;
            }

            return false;
        }

        public void Cancel(object? sender, DragEventArgs e)
        {
        }

        void IDropHandler.Enter(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
        {
           // throw new System.NotImplementedException();
        }

        void IDropHandler.Over(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
        {
          //  throw new System.NotImplementedException();
        }

        void IDropHandler.Drop(object? sender, DragEventArgs e, object? sourceContext, object? targetContext)
        {
            // throw new System.NotImplementedException();
            if (Validate(sender, e, sourceContext, targetContext, null))
            {
                Execute(sender, e, sourceContext, targetContext, null);
            }
        }

        void IDropHandler.Leave(object? sender, RoutedEventArgs e)
        {
           // throw new System.NotImplementedException();
        }

        void IDropHandler.Cancel(object? sender, RoutedEventArgs e)
        {
            //throw new System.NotImplementedException();
        }
    }
}
