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
    private const string Model = "pixtral-12b-2409";
    private const string Prompt =
        "You are an expert in Monster Energy drink products. Carefully examine this image. " +
        "Identify the exact Monster Energy product visible. " +
        "If the text on the can is hard to read, use the can's colors, design, and visual cues to determine which Monster product it is — every Monster variant has a distinctive color scheme. " +
        "\n\nColor reference dictionary — use this to identify the product when unsure:\n" +
        "- White can → Monster Energy Ultra White\n" +
        "- Pink can with a slightly blue side and a slightly red side → Monster Energy Ultra Fantasy\n" +
        "\n" +
        "Return ONLY the exact product name as a plain string (e.g. \"Monster Energy Ultra White\"). " +
        "If you are certain there is no Monster Energy product in the image, return an empty string.";

    public async Task<string?> AnalyzeAsync(byte[] imageBytes, string mediaType)
    {
        var base64 = Convert.ToBase64String(imageBytes);
        var imageUrl = $"data:{mediaType};base64,{base64}";

        var requestBody = new
        {
            model = Model,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "image_url", image_url = new { url = imageUrl } },
                        new { type = "text", text = Prompt }
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
