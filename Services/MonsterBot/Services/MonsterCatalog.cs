namespace MonsterBot.Services;

public record MonsterEntry(string[] Keywords, string CanonicalName, string Emoji);

public static class MonsterCatalog
{
    // Order matters: more specific entries must come before generic ones
    private static readonly MonsterEntry[] Entries =
    [
        // ── Ultras ──────────────────────────────────────────────────────────
        new(["ultra white", "white monster", "the white monster"],    "Ultra White",            "⚪"),
        new(["ultra paradise"],                                        "Ultra Paradise",         "🟢"),
        new(["ultra black"],                                           "Ultra Black",            "⚫"),
        new(["ultra fiesta"],                                          "Ultra Fiesta",           "🔵"),
        new(["ultra red", "red"],                                      "Ultra Red",              "🔴"),
        new(["ultra violet", "purple monster", "the purple monster"],  "Ultra Violet",           "🟣"),
        new(["ultra gold", "gold"],                                    "Ultra Gold",             "🟡"),
        new(["ultra blue", "blue monster", "the blue monster", "blue"],"Ultra Blue",             "🔵"),
        new(["ultra watermelon"],                                      "Ultra Watermelon",       "🔴"),
        new(["ultra rosé", "ultra rose"],                              "Ultra Rosé",             "🟣"),
        new(["ultra strawberry", "strawberry dreams"],                 "Ultra Strawberry Dreams", "🟣"),
        new(["ultra fantasy"],                                         "Ultra Fantasy",          "🟣"),

        // ── Originals ───────────────────────────────────────────────────────
        new(["nitro"],                                                 "Energy Nitro",           "⚫"),
        new(["vr46", "the doctor", "valentino"],                       "VR46",                   "🟡"),
        new(["full throttle"],                                         "Full Throttle",          "🔵"),
        new(["lando norris", "lambo norris", "norris"],                "Lando Norris",           "🟣"),
        new(["original green", "the original", "original energy"],     "The Original",           "🟢"),

        // ── Juiced ──────────────────────────────────────────────────────────
        new(["aussie lemonade", "aussie"],                             "Aussie Lemonade",        "🟡"),
        new(["pacific punch", "pacific"],                              "Pacific Punch",          "🟤"),
        new(["mango loco", "mango"],                                   "Mango Loco",             "🔵"),
        new(["khaos"],                                                 "Khaos",                  "🟠"),
        new(["ripper"],                                                "Ripper",                 "🩷"),
        new(["pipeline"],                                              "Pipeline Punch",         "🩷"),
        new(["mixxd", "mixd"],                                         "Mixxd",                  "🟣"),
    ];

    /// <summary>
    /// Résout le nom retourné par l'API vers une entrée canonique.
    /// Retourne null si aucune correspondance trouvée.
    /// </summary>
    public static MonsterEntry? Resolve(string? apiResponse)
    {
        if (string.IsNullOrWhiteSpace(apiResponse))
            return null;

        var lower = apiResponse.ToLowerInvariant();
        return Entries.FirstOrDefault(e => e.Keywords.Any(k => lower.Contains(k)));
    }
}
