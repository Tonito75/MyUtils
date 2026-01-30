using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.FileWatcher
{
    public interface IFileWatcherService
    {
        (bool, string) Init(string filePath, Action<string> onFileChanged);
    }
}
