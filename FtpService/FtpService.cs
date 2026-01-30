using FluentFTP;
using OneOf;

namespace FtpService
{
    public class FtpService : IFtpService
    {
        private string _host;
        private string _password;
        private string _username;

        AsyncFtpClient _ftpClient;

        public Task<OneOf<Task, string>> Connect(string host, string userName, string password)
        {
            _host = host;
            _password = password;
            _username = userName;

            _ftpClient = new AsyncFtpClient(_host, _username, password);

            _ftpClient.AutoConnect();
        }
    }
}
