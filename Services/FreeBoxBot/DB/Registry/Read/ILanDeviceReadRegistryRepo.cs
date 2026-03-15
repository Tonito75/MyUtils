namespace DiscordBot.DB.Registry.Read
{
    public interface ILanDeviceReadRegistryRepo
    {
        public Task<IList<LanDevice>> GetConnectedDevices();

        public Task<IList<LanDevice>> GetAllKnownDevices();
    }
}
