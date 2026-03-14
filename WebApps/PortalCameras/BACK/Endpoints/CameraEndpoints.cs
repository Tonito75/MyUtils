using Microsoft.Extensions.Options;
using PortalCameras.Models;
using PortalCameras.Services;

namespace PortalCameras.Endpoints;

public static class CameraEndpoints
{
    public static void MapCameraEndpoints(this WebApplication app)
    {
        // GET /api/cameras — liste les caméras
        app.MapGet("/api/cameras", (IOptions<List<CameraConfig>> cameras) =>
        {
            var result = cameras.Value.Select(c => new
            {
                c.Name,
                c.Ip,
                c.Url,
                c.Group
            });
            return Results.Ok(result);
        }).RequireAuthorization();

        // GET /api/cameras/{name}/ping — ping une caméra
        app.MapGet("/api/cameras/{name}/ping", async (
            string name,
            IOptions<List<CameraConfig>> cameras,
            PingService pingService) =>
        {
            var camera = cameras.Value.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (camera is null) return Results.NotFound();

            var isOnline = await pingService.PingAsync(camera.Ip);
            return Results.Ok(new { name = camera.Name, isOnline });
        }).RequireAuthorization();

        // GET /api/cameras/{name}/history?useAI=false — images récentes
        app.MapGet("/api/cameras/{name}/history", async (
            string name,
            bool? useAI,
            IOptions<List<CameraConfig>> cameras,
            IConfiguration config,
            IOService ioService,
            DateService dateService,
            DetectThingsService detectService,
            DiscordService discordService,
            ILogger<Program> logger) =>
        {
            var camera = cameras.Value.FirstOrDefault(c =>
                c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (camera is null || string.IsNullOrEmpty(camera.HistoryFolder))
                return Results.NotFound();

            var baseFolder = config["BaseHistoryFolder"] ?? string.Empty;
            var apiDetectUrl = config["ApiDetectThingsUrl"] ?? string.Empty;
            var physicalBase = Path.Combine(baseFolder, camera.HistoryFolder);
            var urlBase = $"/camera-history/{camera.HistoryFolder.Replace("\\", "/")}";

            await discordService.SendAsync($"⚠️ Quelqu'un consulte l'historique de {name}.");

            var images = new List<object>();
            const int maxImages = 10;
            const int maxIterations = 200;
            const int maxApiFails = 5;

            int days = 0, found = 0, iterations = 0, apiFails = 0;

            while (found < maxImages && iterations < maxIterations)
            {
                var dateFolder = dateService.GetDateFolderDaysAgo(days);
                var physicalDir = Path.Combine(physicalBase, dateFolder);

                if (ioService.DirectoryExists(physicalDir))
                {
                    var files = ioService.ListFileNames(physicalDir, "*.jpg", true);

                    foreach (var (fileName, creationDate) in files)
                    {
                        if (found >= maxImages) break;

                        var imageUrl = $"{urlBase}/{dateFolder}/{fileName}";

                        if (useAI == true)
                        {
                            var physicalFile = Path.Combine(physicalDir, fileName);
                            var (detected, error) = await detectService.DetectAsync(physicalFile, apiDetectUrl);

                            if (!string.IsNullOrEmpty(error))
                            {
                                logger.LogError("Detect API error for {File}: {Error}", fileName, error);
                                apiFails++;
                                if (apiFails > maxApiFails) goto Done;
                                continue;
                            }

                            if (!detected) continue;
                        }

                        images.Add(new
                        {
                            url = imageUrl,
                            date = creationDate,
                            timeAgo = dateService.FormatTimeAgoFrench(creationDate)
                        });
                        found++;
                    }
                }

                days++;
                iterations++;
            }

            Done:
            return Results.Ok(images);
        }).RequireAuthorization();
    }
}
