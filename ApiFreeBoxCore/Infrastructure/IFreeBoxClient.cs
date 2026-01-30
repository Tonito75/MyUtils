namespace Infrastructure
{
    public interface IFreeBoxClient
    {
        Task<(bool,string, List<FreeBoxDevice>?)> GetConnectedDevicesAsync();
    }
}
