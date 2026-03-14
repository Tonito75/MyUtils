namespace PortalCameras.Services;

public class DiscordService
{
    private readonly ILogger<DiscordService> _logger;
    private readonly string _webHookUrl;
    private readonly HttpClient _client = new();

    public DiscordService(IConfiguration config, ILogger<DiscordService> logger)
    {
        _logger = logger;
        _webHookUrl = config["WebHookUrl"] ?? string.Empty;
    }

    public async Task SendAsync(string message)
    {
        if (string.IsNullOrEmpty(_webHookUrl)) return;

        try
        {
            var payload = System.Text.Json.JsonSerializer.Serialize(new { content = message });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            await _client.PostAsync(_webHookUrl, content);
        }
        catch (Exception ex)
        {
            _logger.LogError("Discord webhook error: {Message}", ex.Message);
        }
    }
}
