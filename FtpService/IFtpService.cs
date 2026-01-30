using OneOf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FtpService
{
    public interface IFtpService
    {
        Task<OneOf<Task,string>> Connect(string host, string userName, string password);
    }
}
