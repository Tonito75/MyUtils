using System;
using System.Collections.Generic;
using System.Text;

namespace Common.Freebox
{
    public class LanDevice
    {
        public int Id { get; set; }

        public required string OriginalName { get; set; }

        public string? CustomName { get; set; }

        public required string IpAddress { get; set; }

        public required string MacAddress { get; set; }

        public bool IsConnected { get; set; }

        public DateTime ConnectedSince { get; set; }

        public DateTime LastConnected { get; set; }
    }
}
