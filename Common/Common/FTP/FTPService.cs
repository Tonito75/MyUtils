using FluentFTP;
using OneOf;
using Common.Classes.Configuration;
using FluentFTP.Helpers;

namespace Common.FTP
{
    public class FTPService : IFTPService
    {
        private AsyncFtpClient? _ftpClient;

        public void Init(string host, string port, string username, string password)
        {
            _ftpClient = new AsyncFtpClient(host, username, password, Convert.ToInt32(port));
        }

        public void Init(FtpConfiguration config)
        {
            _ftpClient = new AsyncFtpClient(config.Host, config.UserName, config.Password, Convert.ToInt32(config.Port));
        }

        public async Task<(bool, string)> Send(string filePath, string remotePath)
        {
            try
            {
                if (!Path.HasExtension(remotePath))
                {
                    var fileName = Path.GetFileName(filePath);
                    remotePath = $"{remotePath}/{fileName}";
                }

                return await Send(File.ReadAllBytes(filePath), remotePath);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<(bool, string)> Send(byte[] fileContent, string remotePath)
        {
            if(_ftpClient == null)
            {
                return (false, "Le client ftp n'a pas été initialisé");
            }
            try
            {
                await _ftpClient.AutoConnect();

                using (var stream = new MemoryStream(fileContent))
                {
                    await _ftpClient.UploadStream(stream, remotePath, FtpRemoteExists.OverwriteInPlace, true);
                }
                
                await _ftpClient.Disconnect();

                return (true, string.Empty);
            }
            catch(Exception ex)
            {
                if (ex.InnerException == null)
                {
                    return (false, ex.Message);
                }
                return (false, ex.InnerException.Message);
            }
        }

        public async Task<(bool,string)> DownloadFilesFromFtpToLocalFolder(string remoteFolder, string localFolder)
        {
            if (_ftpClient == null)
            {
                return (false,"Le client ftp n'a pas été initialisé");
            }
            try
            {
                await _ftpClient.AutoConnect();

                await _ftpClient.DownloadDirectory(localFolder, remoteFolder, FtpFolderSyncMode.Mirror);

                await _ftpClient.Disconnect();

                return (true,string.Empty);
            }
            catch(Exception ex)
            {
                return (false,ex.Message);
            }
        }

        public async Task<(bool, string)> DownloadFile(string filePath, string destinationPath)
        {
            if (_ftpClient == null)
            {
                return (false,"Le client ftp n'a pas été initialisé");
            }
            try
            {
                await _ftpClient.AutoConnect();

                var status = await _ftpClient.DownloadFile(destinationPath, filePath, FtpLocalExists.Overwrite,FtpVerify.Retry);

                await _ftpClient.Disconnect();

                if (status.IsFailure())
                {
                    return (false, $"Echec du téléchargement du fichier suite à un status : {status}");
                }

                var fileInfo = new FileInfo(destinationPath);
                if (fileInfo.Length == 0)
                {
                    return (false, "Le fichier téléchargé est vide.");
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                if (ex.InnerException == null)
                {
                    return (false, ex.Message);
                }
                return (false, $"Fichier {filePath} : {ex.InnerException.Message}");
            }
        }

        public async Task<(bool, string, List<FtpListItem>?)> ListFiles(string folder)
        {
            if (_ftpClient == null)
            {
                return (false, "Le client ftp n'a pas été initialisé", null);
            }
            try
            {
                await _ftpClient.AutoConnect();

                var list = await _ftpClient.GetListing(folder);

                await _ftpClient.Disconnect();

                return (true,string.Empty,list.ToList());
            }
            catch(Exception e) {
                return (false, e.Message, null);
            }
        }

        public async Task<(bool, string)> CleanFolder(string remoteFolder, string extension)
        {
            if (_ftpClient == null)
            {
                return (false, "Le client ftp n'a pas été initialisé");
            }
            try
            {
                var (success, error, files) = await ListFiles(remoteFolder);
                if (!success)
                {
                    return (false, error);
                }

                await _ftpClient.AutoConnect();

                if (files != null)
                {
                    foreach (var item in files)
                    {
                        if (item.Type == FtpObjectType.File && item.Name.Contains(extension))
                        {
                            await _ftpClient.DeleteFile(item.FullName);
                        }
                    }
                }

                await _ftpClient.Disconnect();

                return (true, string.Empty);
                
            }
            catch(Exception e)
            {
                if (e.InnerException == null)
                {
                    return (false, e.Message);
                }
                return (false, e.InnerException.Message);
            }

        }
    }
}
