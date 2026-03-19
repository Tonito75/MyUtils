using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MonsterBot.Services.Vision;

public class MistralVisionService(
    IHttpClientFactory httpClientFactory,
    IOptions<AppSettings> options,
    ILogger<MistralVisionService> logger) : IVisionService
{
    private const string Endpoint = "https://api.mistral.ai/v1/chat/completions";
    private const string Model = "pixtral-large-2411";

    private const string Prompt =
        "You are a Monster Energy can identifier. Your output is always one of two things:\n" +
        "1. The exact product name from the list below\n" +
        "2. An empty string\n" +
        "Nothing else. Ever. No explanation. No apology. No sentence. Just the name or nothing.\n" +
        "\n" +
        "Known products — always return the name exactly as written here:\n" +
        "Monster Ultra White, Monster Ultra Paradise, Monster Ultra Black, Monster Ultra Fiesta,\n" +
        "Monster Ultra Red, Monster Ultra Violet, Monster Ultra Gold, Monster Ultra Blue,\n" +
        "Monster Ultra Watermelon, Monster Ultra Rosé, Monster Ultra Strawberry Dreams, Monster Ultra Fantasy,\n" +
        "Monster Energy Nitro, Monster Energy VR46,\n" +
        "Monster Energy The Original, Monster Energy Full Throttle,\n" +
        "Monster Energy Lando Norris, Monster Energy Pipeline Punch\n" +
        "Juiced Monster Aussie Lemonade, Juiced Monster Pacific Punch, Juiced Monster Mango Loco,\n" +
        "Juiced Monster Khaos, Juiced Monster Ripper\n" +
        "\n" +
        "If the text is unreadable, identify by color:\n" +
        "- White can → Monster Ultra White\n" +
        "- Green can → Monster Energy The Original\n" +
        "- Black can → Monster Ultra Black\n" +
        "- Teal/cyan can → Monster Ultra Fiesta\n" +
        "- Red can → Monster Ultra Red\n" +
        "- Purple can → Monster Ultra Violet\n" +
        "- Yellow/gold can → Monster Ultra Gold\n" +
        "- Light blue can → Monster Ultra Blue\n" +
        "- Pink can (blue+red sides) → Monster Ultra Fantasy\n" +
        "- Pink can → Monster Ultra Rosé\n" +
        "\n" +
        "If the image contains no Monster Energy can: output an empty string. No words. Empty.";

    public async Task<string?> AnalyzeAsync(byte[] imageBytes, string mediaType)
    {
        var base64 = Convert.ToBase64String(imageBytes);
        var imageUrl = $"data:{mediaType};base64,{base64}";

        var requestBody = new
        {
            model = Model,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = Prompt
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "image_url", image_url = new { url = imageUrl } }
                    }
                }
            }
        };

        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", options.Value.Ai.MistralApiKey);

        logger.LogInformation("Mistral: envoi image {MediaType} ({Size} bytes)", mediaType, imageBytes.Length);

        try
        {
            var json = JsonSerializer.Serialize(requestBody);
            using var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(Endpoint, httpContent);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                logger.LogError("Mistral API erreur {Status}: {Body}", (int)response.StatusCode, body);
                response.EnsureSuccessStatusCode();
            }

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            logger.LogInformation("Mistral: réponse brute = {Content}", content);

            return VisionResponseParser.Parse(content, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Mistral vision API call failed");
            throw;
        }
    }
}
