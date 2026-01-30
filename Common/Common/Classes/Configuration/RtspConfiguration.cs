using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.Classes.Configuration
{
    public class RtspConfiguration
    {
        public string Host {  get; set; }

        public string Port { get; set; }

        public string UserName { get; set; }

        public string Password { get; set; }

        public string StreamName { get; set; }

        public string GetRtspUrl => $"rtsp://{UserName}:{Password}@{Host}:{Port}:{StreamName}";
    }
}
