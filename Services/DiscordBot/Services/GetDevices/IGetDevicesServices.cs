namespace DiscordBot.Services.GetDevices
{
    public interface IGetDevicesServices
    {
        Task<(string? Error, List<LanDevice>? Devices)> GetDevices();
    }
}
