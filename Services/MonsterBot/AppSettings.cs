namespace MonsterBot;

public record DiscordSettings(string Token, ulong[] ChannelIds, ulong ServerGuildId);
public record AiSettings(string Provider, string MistralApiKey, string ClaudeApiKey);
public record DbSettings(string DefaultConnection);

public class AppSettings
{
    public required DiscordSettings Discord { get; set; }
    public required AiSettings Ai { get; set; }
    public required DbSettings ConnectionString { get; set; }
}
