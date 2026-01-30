using System.ComponentModel.DataAnnotations.Schema;

namespace Domain
{
    public class LanDevice
    {
        public int Id { get; set; }

        public AddressType AddressType { get; set; }

        public string? OriginalName { get; set; }

        public string? CustomName { get; set; }

        [JsonIgnore]
        [NotMapped]
        public string Name => (string.IsNullOrEmpty(CustomName) ? OriginalName : CustomName) ?? string.Empty;

        public string? IpAddress { get; set; }

        public required string MacAddress { get; set; }

        public string? Vendor {  get; set; }

        public bool IsConnected { get; set; }

        public bool Isfavourite { get; set; }

        public string? HostType { get; set; }

        public DateTime ConnectedSince { get; set; }

        public DateTime LastConnected { get; set; }
    }
}
