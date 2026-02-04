
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BlazorPortalCamera.Services;

public class DetectThingsService : IDetectThingsService
{
    private readonly IConfiguration _configuration;

    private readonly ILogger<DetectThingsService> _logger;

    private readonly HttpClient _client;

    private readonly string _baseurl;

    public DetectThingsService(IConfiguration configuration, ILogger<DetectThingsService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _baseurl = _configuration["BaseUrl"];

        _client = new HttpClient();
    }

    public async Task<(bool, string)> DetectThingsAsync(string imagePath, string apiUrl)
    {
        if (string.IsNullOrEmpty(apiUrl))
        {
            throw new ArgumentNullException(nameof(apiUrl));
        }

        if (string.IsNullOrEmpty(imagePath))
        {
            throw new ArgumentNullException(nameof(imagePath));
        }

        return await DetectThings(imagePath, apiUrl);
    }

    private async Task<(bool success, string error)> DetectThings(string imagePath, string apiUrl)
    {
        try
        {
            using var form = new MultipartFormDataContent();

            // Lire le fichier et l'ajouter au formulaire
            var fileBytes = await File.ReadAllBytesAsync(imagePath);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

            // Le nom "file" doit correspondre au paramètre attendu par l'API
            form.Add(fileContent, "file", Path.GetFileName(imagePath));

            // Envoyer la requête
            var response = await _client.PostAsync(apiUrl, form);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var res = JsonSerializer.Deserialize<bool>(responseContent);
                return (res, string.Empty);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, $"API returned {response.StatusCode}: {errorContent}");
            }
        }
        catch (Exception ex)
        {
            return (false, $"Error calling API: {ex.Message}");
        }
    }

}
