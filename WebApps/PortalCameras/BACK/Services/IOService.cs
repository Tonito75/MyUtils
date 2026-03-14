namespace PortalCameras.Services;

public class IOService
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public List<(string FileName, DateTime CreationDate)> ListFileNames(
        string directory, string pattern, bool descending)
    {
        if (!Directory.Exists(directory))
            return [];

        var files = Directory.GetFiles(directory, pattern)
            .Select(f => (
                FileName: Path.GetFileName(f),
                CreationDate: File.GetLastWriteTime(f)
            ));

        return descending
            ? files.OrderByDescending(f => f.CreationDate).ToList()
            : files.OrderBy(f => f.CreationDate).ToList();
    }
}
