using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MonsterBot.Services.Vision;

public class ClaudeVisionService(
    IOptions<AppSettings> options,
    ILogger<ClaudeVisionService> logger) : IVisionService
{
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

    private readonly AnthropicClient _client = new(options.Value.Ai.ClaudeApiKey);

    public async Task<string?> AnalyzeAsync(byte[] imageBytes, string mediaType)
    {
        var base64 = Convert.ToBase64String(imageBytes);

        var messages = new List<Message>
        {
            new()
            {
                Role = RoleType.User,
                Content = new List<ContentBase>
                {
                    new ImageContent
                    {
                        Source = new ImageSource
                        {
                            MediaType = mediaType,
                            Data = base64
                        }
                    },
                    new TextContent { Text = Prompt }
                }
            }
        };

        var request = new MessageParameters
        {
            Model = AnthropicModels.Claude46Sonnet,
            MaxTokens = 256,
            Messages = messages,
            Stream = false
        };

        try
        {
            var response = await _client.Messages.GetClaudeMessageAsync(request);
            var text = response.Content.OfType<TextContent>().FirstOrDefault()?.Text
                       ?? response.FirstMessage?.Text
                       ?? string.Empty;
            return VisionResponseParser.Parse(text, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Claude vision API call failed");
            throw;
        }
    }
}
