namespace PortalCameras.Endpoints;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this WebApplication app)
    {
        app.MapGet("/camera-history/{**path}", (HttpContext ctx, string path, IConfiguration config) =>
        {
            var baseFolder = config["BaseHistoryFolder"];
            if (string.IsNullOrEmpty(baseFolder)) return Results.NotFound();

            var fullPath = Path.Combine(baseFolder, path);
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
