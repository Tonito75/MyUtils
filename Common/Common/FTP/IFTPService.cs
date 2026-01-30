using FluentFTP;
using OneOf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Classes.Configuration;

namespace Common.FTP
{
    public interface IFTPService
    {
        void Init(string host, string port, string username, string password);

        void Init(FtpConfiguration config);

        Task<(bool, string)> Send(string filePath, string remotePath);

        Task<(bool, string)> CleanFolder(string remoteFolder, string extension);

        Task<(bool, string)> Send(byte[] fileContent, string remotePath);

        Task<(bool, string)> DownloadFilesFromFtpToLocalFolder(string remoteFolder, string localFolder);

        Task<(bool, string)> DownloadFile(string filePath, string destinationPath);

        Task<(bool, string, List<FtpListItem>?)> ListFiles(string folder);
    }
}
