using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Common.Logger
{
    public interface ILogService
    {
        void Log(string message,[CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "");

        void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "");
    }
}
