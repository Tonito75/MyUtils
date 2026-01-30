using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Classes.Configuration;

namespace TimelapseCreator.Configuration
{
    public class TimelapseConfiguration
    {
        public string WebHookUrl { get; set; }

        public string AppName { get; set; }

        public RtspConfiguration RtspConfiguration { get; set; }

        public FtpConfiguration FtpConfiguration { get; set; }
    }
}
