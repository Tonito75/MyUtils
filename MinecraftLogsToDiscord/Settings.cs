using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinecraftLogsToDiscord
{
    public class Settings
    {
        public string WebhookUrl { get; set; }

        public string LatestLogPath { get; set; }

        public string DeathMessagesPatterns { get; set; }

        public string AdvancementPatterns { get; set; }

        public string ConnexionPatterns { get; set; }

        public string DeconnexionPattern { get; set; }

        public string LagPattern { get; set; }

        public string StartPattern { get; set; }

        public string StopPattern { get; set; }
    }
}
