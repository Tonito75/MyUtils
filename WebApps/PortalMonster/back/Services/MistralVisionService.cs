using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MonsterHub.Api.Settings;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace MonsterHub.Api.Services;

public class MistralVisionService(
    IHttpClientFactory httpClientFactory,
    IOptions<MistralSettings> options,
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
        "Known products:\n" +
        "Monster Ultra White, Monster Ultra Paradise, Monster Ultra Black, Monster Ultra Fiesta,\n" +
        "Monster Ultra Red, Monster Ultra Violet, Monster Ultra Gold, Monster Ultra Blue,\n" +
        "Monster Ultra Watermelon, Monster Ultra Rosé, Monster Ultra Strawberry Dreams, Monster Ultra Fantasy,\n" +
        "Monster Energy Nitro, Monster Energy VR46, Monster Energy The Original, Monster Energy Full Throttle,\n" +
        "Monster Energy Lando Norris, Monster Energy Pipeline Punch,\n" +
        "Juiced Monster Aussie Lemonade, Juiced Monster Pacific Punch, Juiced Monster Mango Loco,\n" +
        "Juiced Monster Khaos, Juiced Monster Ripper, Juiced Monster Mixxd\n" +
        "\n" +
        "If the image contains no Monster Energy can: output an empty string.";

    private static byte[] ResizeForAnalysis(byte[] imageBytes)
    {
        using var image = Image.Load(imageBytes);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(512, 512),
            Mode = ResizeMode.Max
        }));
        using var ms = new MemoryStream();
        image.SaveAsJpeg(ms, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 50 });
        return ms.ToArray();
    }

    public async Task<string?> AnalyzeAsync(byte[] imageBytes, string mediaType)
    {
        var resized = ResizeForAnalysis(imageBytes);
        var base64 = Convert.ToBase64String(resized);
        var imageUrl = $"data:image/jpeg;base64,{base64}";

        var requestBody = new
        {
            model = Model,
            messages = new object[]
            {
                new { role = "system", content = Prompt },
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
            new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);

        var json = JsonSerializer.Serialize(requestBody);
        using var httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var response = await client.PostAsync(Endpoint, httpContent);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            logger.LogError("Mistral API error {Status}: {Body}", (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        logger.LogInformation("Mistral response: {Content}", content);
        return string.IsNullOrWhiteSpace(content) ? null : content.Trim();
    }
}
