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
        "You are a Monster Energy can identifier. Your output is always one of two things:\n" +
        "1. The exact product name (e.g. Monster Energy Ultra White)\n" +
        "2. An empty string\n" +
        "Nothing else. Ever. No explanation. No apology. No sentence. Just the name or nothing.\n" +
        "\n" +
        "To identify the product, use the text on the can first. If unreadable, use the color:\n" +
        "- White can → Monster Energy Ultra White\n" +
        "- Pink can with slightly blue on one side and slightly red on the other → Monster Energy Ultra Fantasy\n" +
        "\n" +
        "If the image contains no Monster Energy can: output an empty string. No words. Empty.";

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
