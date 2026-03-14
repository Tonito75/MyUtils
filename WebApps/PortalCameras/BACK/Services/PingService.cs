namespace PortalCameras.Services;

public class PingService
{
    private readonly ILogger<PingService> _logger;

    public PingService(ILogger<PingService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> PingAsync(string ip)
    {
        try
        {
            using var ping = new System.Net.NetworkInformation.Ping();
            var reply = await ping.SendPingAsync(ip, timeout: 2000);
            return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Ping failed for {Ip}: {Message}", ip, ex.Message);
            return false;
        }
    }
}
