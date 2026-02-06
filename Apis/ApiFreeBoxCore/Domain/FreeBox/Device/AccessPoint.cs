namespace Domain.FreeBox.Device
{
    public class AccessPoint
    {
        public string? Mac { get; set; }
        public string? Type { get; set; }
        public long TxBytes { get; set; }
        public long RxBytes { get; set; }
        public string? ConnectivityType { get; set; }
        public string? Uid { get; set; }
        public EthernetInformation? EthernetInformation { get; set; }
    }
}
