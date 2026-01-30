using System.Text.Json.Serialization;

namespace Domain.FreeBox.Device
{
    public class FreeBoxDevice
    {
        public L2Ident L2Ident { get; set; }
        public bool Active { get; set; }
        public bool Persistent { get; set; }
        public List<NameSource> Names { get; set; }

        [JsonPropertyName("vendor_name")]
        public string VendorName { get; set; }

        [JsonPropertyName("host_type")]
        public string HostType { get; set; }
        public string Interface { get; set; }
        public string Id { get; set; }

        [JsonPropertyName("last_time_reachable")]
        public long LastTimeReachable { get; set; }
        public bool PrimaryNameManual { get; set; }
        public List<L3Connectivity> L3connectivities { get; set; }
        public string DefaultName { get; set; }

        [JsonPropertyName("first_activity")]
        public long FirstActivity { get; set; }
        public bool Reachable { get; set; }

        
        public long LastActivity { get; set; }
        [JsonPropertyName("primary_name")]
        public string PrimaryName { get; set; }
        public AccessPoint AccessPoint { get; set; }
    }
}
