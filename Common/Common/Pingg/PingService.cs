using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;

namespace Common.Pingg;

public class PingService
{
    private readonly ILogger<PingService> _logger;

    public PingService(ILogger<PingService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> PingAsync(string ipAddress)
    {
        _logger.LogInformation("Tentative de ping vers {IpAddress}", ipAddress);

        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(ipAddress, 3000);

            if (reply.Status == IPStatus.Success)
            {
                _logger.LogInformation("Ping vers {IpAddress} reussi en {RoundtripTime}ms", ipAddress, reply.RoundtripTime);
                return true;
            }
            else
            {
                _logger.LogWarning("Ping vers {IpAddress} echoue: {Status}", ipAddress, reply.Status);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du ping vers {IpAddress}", ipAddress);
            return false;
        }
    }
}
