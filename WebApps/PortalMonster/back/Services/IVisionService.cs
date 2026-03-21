namespace MonsterHub.Api.Services;

public interface IVisionService
{
    Task<string?> AnalyzeAsync(byte[] imageBytes, string mediaType);
}
