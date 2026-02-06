using Common.Classes.Configuration;

namespace CameraWatcher.Configuration;

public class WatcherConfiguration
{
    public string WebHookUrl { get; set; } = string.Empty;

    public string ApiMeteoUrl {  get; set; } = string.Empty;

    public FtpConfiguration FtpConfiguration { get; set; } = new();
}
