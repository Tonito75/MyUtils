using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace MonsterBot.Services.Vision;

public static class VisionResponseParser
{
    private static readonly Regex MarkdownFenceRegex =
        new(@"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.Compiled);

    public static List<string> Parse(string rawResponse, ILogger logger)
    {
        try
        {
            var json = rawResponse.Trim();

            var fenceMatch = MarkdownFenceRegex.Match(json);
            if (fenceMatch.Success)
                json = fenceMatch.Groups[1].Value.Trim();

            var result = JsonSerializer.Deserialize<List<string>>(json);
            return result ?? [];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse vision API response: {Raw}", rawResponse);
            return [];
        }
    }
}
