using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Classes.Configuration;

namespace MinecraftWorldToNAS
{
    public class Settings
    {
        public string WorldFolderPath { get; set; }

        public string DelayInHours { get; set; }

        public FtpConfiguration FtpConfiguration { get; set; }
    }
}
