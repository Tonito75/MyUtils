namespace MonsterBot.Services.Vision;

public interface IVisionService
{
    Task<List<string>> AnalyzeAsync(byte[] imageBytes, string mediaType);
}
