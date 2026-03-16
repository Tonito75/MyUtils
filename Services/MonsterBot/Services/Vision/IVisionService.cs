namespace MonsterBot.Services.Vision;

public interface IVisionService
{
    Task<string?> AnalyzeAsync(byte[] imageBytes, string mediaType);
}
