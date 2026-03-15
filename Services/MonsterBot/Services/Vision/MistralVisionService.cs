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
        "List the colors of Monster Energy drink cans visible in this image. " +
        "Return ONLY a JSON array of color strings in French (e.g. [\"Verte\", \"Noire\"]). " +
        "If no Monster can is visible, return an empty array [].";

    public async Task<List<string>> AnalyzeAsync(byte[] imageBytes, string mediaType)
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

        try
        {
            var response = await client.PostAsJsonAsync(Endpoint, requestBody);
            response.EnsureSuccessStatusCode();

            using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            return VisionResponseParser.Parse(content, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Mistral vision API call failed");
            throw;
        }
    }
}
