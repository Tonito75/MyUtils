namespace Domain.FreeBox.Device
{
    public class EthernetInformation
    {
        public string Duplex { get; set; }
        public int Speed { get; set; }
        public int MaxPortSpeed { get; set; }
        public string Link { get; set; }
    }
}
