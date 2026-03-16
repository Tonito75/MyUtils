using Microsoft.Extensions.Logging;

namespace MonsterBot.Services.Vision;

public static class VisionResponseParser
{
    public static string? Parse(string rawResponse, ILogger logger)
    {
        var name = rawResponse.Trim().Trim('"');

        if (string.IsNullOrWhiteSpace(name))
            return null;

        logger.LogInformation("Monster identifié : {Name}", name);
        return name;
    }
}
