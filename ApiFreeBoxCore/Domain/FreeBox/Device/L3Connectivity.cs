using System.Text.Json.Serialization;

namespace Domain.FreeBox.Device
{
    public class L3Connectivity
    {
        public string Addr { get; set; }
        public bool Active { get; set; }
        public bool Reachable { get; set; }

        [JsonPropertyName("last_activity")]
        public long LastActivity { get; set; }
        public string Af { get; set; }

        [JsonPropertyName("last_time_reachable")]
        public long LastTimeReachable { get; set; }
    }
}
