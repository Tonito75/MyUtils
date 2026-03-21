namespace MonsterHub.Api.Services;

public interface IStorageService
{
    /// <summary>Saves bytes to the NAS. Returns the relative path (without BaseRemotePath).</summary>
    Task<string> SaveAsync(byte[] data, string folder, string userId, string extension);

    /// <summary>Downloads bytes from the NAS by relative path.</summary>
    Task<(byte[] Data, string ContentType)> GetAsync(string relativePath);

    /// <summary>Deletes a file from the NAS by relative path.</summary>
    Task DeleteAsync(string relativePath);
}
