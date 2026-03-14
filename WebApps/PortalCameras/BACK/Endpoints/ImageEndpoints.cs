namespace PortalCameras.Endpoints;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this WebApplication app)
    {
        app.MapGet("/camera-history/{**path}", (HttpContext ctx, string path, IConfiguration config) =>
        {
            var baseFolder = config["BaseHistoryFolder"];
            if (string.IsNullOrEmpty(baseFolder)) return Results.NotFound();

            // Résoudre les chemins en absolu pour éviter le path traversal
            var resolvedBase = Path.GetFullPath(baseFolder);
            var fullPath = Path.GetFullPath(Path.Combine(resolvedBase, path));

            // Vérifier que le chemin résolu est bien sous baseFolder
            if (!fullPath.StartsWith(resolvedBase + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !fullPath.Equals(resolvedBase, StringComparison.OrdinalIgnoreCase))
            {
                return Results.Forbid();
            }

            if (!File.Exists(fullPath)) return Results.NotFound();

            var contentType = Path.GetExtension(fullPath).ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };

            ctx.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            return Results.File(fullPath, contentType);
        }).RequireAuthorization();
    }
}
