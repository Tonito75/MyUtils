using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Options
{
    public class FreeBoxClientOptions
    {
        public required string AppToken { get; set; }

        public required string BaseFreeBoxUrl { get; set; }

        public required string AppId { get; set; }
    }
}
