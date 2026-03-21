using FluentFTP;
using Microsoft.Extensions.Options;
using MonsterHub.Api.Settings;

namespace MonsterHub.Api.Services;

public class FtpStorageService(IOptions<FtpSettings> options, ILogger<FtpStorageService> logger)
    : IStorageService
{
    private readonly FtpSettings _settings = options.Value;

    private AsyncFtpClient CreateClient() =>
        new(_settings.Host, _settings.UserName, _settings.Password, int.Parse(_settings.Port));

    public async Task<string> SaveAsync(byte[] data, string folder, string userId, string extension)
    {
        var relativePath = $"{folder}/{userId}/{Guid.NewGuid():N}.{extension.TrimStart('.')}";
        var remotePath = $"{_settings.BaseRemotePath}/{relativePath}";

        using var client = CreateClient();
        await client.AutoConnect();
        using var stream = new MemoryStream(data);
        await client.UploadStream(stream, remotePath, FtpRemoteExists.OverwriteInPlace, true);
        await client.Disconnect();

        logger.LogInformation("Uploaded {RelativePath}", relativePath);
        return relativePath;
    }

    public async Task<(byte[] Data, string ContentType)> GetAsync(string relativePath)
    {
        var remotePath = $"{_settings.BaseRemotePath}/{relativePath}";
        var ext = Path.GetExtension(relativePath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        using var client = CreateClient();
        await client.AutoConnect();
        using var ms = new MemoryStream();
        await client.DownloadStream(ms, remotePath);
        await client.Disconnect();

        return (ms.ToArray(), contentType);
    }

    public async Task DeleteAsync(string relativePath)
    {
        var remotePath = $"{_settings.BaseRemotePath}/{relativePath}";
        using var client = CreateClient();
        await client.AutoConnect();
        await client.DeleteFile(remotePath);
        await client.Disconnect();
    }
}
