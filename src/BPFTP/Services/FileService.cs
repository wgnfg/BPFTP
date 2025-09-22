using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPFTP.Services
{
    public class FileService
    {
        public Task<(IEnumerable<DirectoryInfo>, IEnumerable<FileInfo>)> ListDirectoryAsync(string path)
        {
            return Task.Run(() =>
            {
                try
                {
                    var directoryInfo = new DirectoryInfo(path);
                    if (!directoryInfo.Exists)
                    {
                        throw new DirectoryNotFoundException($"Directory not found: {path}");
                    }

                    var directories = directoryInfo.GetDirectories();
                    var files = directoryInfo.GetFiles();

                    return (directories.AsEnumerable(), files.AsEnumerable());
                }
                catch (Exception ex)
                {

                }
                return ([], []);
            });
        }
    }
}
