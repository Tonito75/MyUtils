namespace PortalCameras.Services;

public class DetectThingsService
{
    private readonly ILogger<DetectThingsService> _logger;
    private readonly HttpClient _client = new();

    public DetectThingsService(ILogger<DetectThingsService> logger)
    {
        _logger = logger;
    }

    public async Task<(bool Detected, string Error)> DetectAsync(string imagePath, string apiUrl)
    {
        try
        {
            var fileBytes = await File.ReadAllBytesAsync(imagePath);
            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            form.Add(fileContent, "file", Path.GetFileName(imagePath));

            var response = await _client.PostAsync(apiUrl, form);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return (false, $"API {response.StatusCode}: {err}");
            }

            var body = await response.Content.ReadAsStringAsync();
            var detected = System.Text.Json.JsonSerializer.Deserialize<bool>(body);
            return (detected, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError("DetectThings error: {Message}", ex.Message);
            return (false, ex.Message);
        }
    }
}
