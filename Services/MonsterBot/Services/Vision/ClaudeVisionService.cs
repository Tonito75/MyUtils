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
        "List the colors of Monster Energy drink cans visible in this image. " +
        "Return ONLY a JSON array of color strings in French (e.g. [\"Verte\", \"Noire\"]). " +
        "If no Monster can is visible, return an empty array [].";

    private readonly AnthropicClient _client = new(options.Value.Ai.ClaudeApiKey);

    public async Task<List<string>> AnalyzeAsync(byte[] imageBytes, string mediaType)
    {
        var client = _client;

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
            var response = await client.Messages.GetClaudeMessageAsync(request);
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
