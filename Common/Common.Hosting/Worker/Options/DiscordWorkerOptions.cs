namespace Common.Hosting.Worker.Options;

public class DiscordWorkerOptions : WorkerOptions
{
    public string WebHookUrl { get; set; } = string.Empty;
}
