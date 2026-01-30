using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Discord
{
    public interface IDiscordWebHookService
    {
        Task LogInfoAsync(string text);

        Task SendAsync(string text);

        Task<(bool, string)> SendAsync(string filePath, string message);

        Task SendWarnAsync(string text);

        Task SendStartAsync(string text);

        Task SendErrorAsync(string text);

        Task SendOkAsync(string text);

        Task SendStopAsync(string text);
    }
}
