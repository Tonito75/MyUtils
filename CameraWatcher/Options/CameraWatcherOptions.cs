using Common.Hosting.Worker.Options;
using CameraWatcher.Configuration;

namespace CameraWatcher.Options;

public class CameraWatcherOptions : DiscordWorkerOptions
{
    public List<WatcherConfiguration> WatcherConfigurations { get; set; } = new();
}
