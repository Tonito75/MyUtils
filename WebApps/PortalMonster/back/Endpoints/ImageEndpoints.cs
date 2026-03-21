using MonsterHub.Api.Services;

namespace MonsterHub.Api.Endpoints;

public static class ImageEndpoints
{
    public static void MapImageEndpoints(this WebApplication app)
    {
        app.MapGet("/api/images/{*path}", async (
            string path,
            HttpContext ctx,
            IStorageService storage) =>
        {
            try
            {
                var (data, contentType) = await storage.GetAsync(path);
                // Set Cache-Control for browsers and CDNs (spec §7)
                ctx.Response.Headers.CacheControl = "public, max-age=86400";
                return Results.File(data, contentType,
                    enableRangeProcessing: false,
                    lastModified: null,
                    entityTag: null);
            }
            catch
            {
                return Results.NotFound();
            }
        });
    }
}
