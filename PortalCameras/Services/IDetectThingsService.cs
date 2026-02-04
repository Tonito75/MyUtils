namespace BlazorPortalCamera.Services;

public interface IDetectThingsService
{
    Task<(bool, string)> DetectThingsAsync(string imagePath, string apiUrl);
}
