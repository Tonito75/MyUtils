using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot
{
    public record DiscordSettings(string Token);

    public record DbSettings(string DefaultConnection);

    public class AppSettings
    {
        public required DiscordSettings Discord {  get; set; }

        public required string ApiFreeboxUrl { get; set; }

        public ulong LanChannelId { get; set; }

        public ulong LanAlertRoleId { get; set; }

        public ulong ServerGuildId { get; set; }

        public required DbSettings ConnectionString { get; set; }
    }
}
