using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Common.Logger
{
    public class WorkerLogService : ILogService
    {
        private readonly Serilog.Core.Logger _serilog;

        public WorkerLogService()
        {
            _serilog = new LoggerConfiguration().WriteTo.Console().CreateLogger();
        }

        public void Error(string message, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "")
        {
            string className = Path.GetFileNameWithoutExtension(sourceFilePath);
            _serilog.Error($"[{className}] : {message}");
        }

        public void Log(string message, [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "")
        {
            string className = Path.GetFileNameWithoutExtension(sourceFilePath);
            _serilog.Information($"[{className}] : {message}");
        }
    }
}
