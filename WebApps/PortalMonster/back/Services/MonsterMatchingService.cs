using MonsterHub.Api.Models;
using System.Text.Json;

namespace MonsterHub.Api.Services;

public class MonsterMatchingService
{
    public MonsterMapping? Match(string mistralOutput, IEnumerable<MonsterMapping> mappings)
    {
        if (string.IsNullOrWhiteSpace(mistralOutput))
            return null;

        var lower = mistralOutput.ToLowerInvariant();

        foreach (var mapping in mappings.OrderBy(m => m.Id))
        {
            var keywords = JsonSerializer.Deserialize<string[]>(mapping.KeywordsJson) ?? [];
            if (keywords.Any(k => lower.Contains(k.ToLowerInvariant())))
                return mapping;
        }

        return null;
    }
}
