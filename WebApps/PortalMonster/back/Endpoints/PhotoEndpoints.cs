using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MonsterHub.Api.Data;
using MonsterHub.Api.Models;
using MonsterHub.Api.Services;

namespace MonsterHub.Api.Endpoints;

public static class PhotoEndpoints
{
    public static void MapPhotoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/photos").RequireAuthorization();

        // POST /api/photos/analyze — detect Monster, do NOT publish
        group.MapPost("/analyze", async (
            HttpRequest request,
            IVisionService vision,
            MonsterMatchingService matching,
            AppDbContext db) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
                return Results.BadRequest("No file provided.");

            var file = request.Form.Files[0];
            var contentType = file.ContentType;
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var mistralOutput = await vision.AnalyzeAsync(ms.ToArray(), contentType);
            if (string.IsNullOrWhiteSpace(mistralOutput))
                return Results.UnprocessableEntity(new { error = "No Monster can detected." });

            var mappings = await db.MonsterMappings.ToListAsync();
            var match = matching.Match(mistralOutput, mappings);
            if (match == null)
                return Results.UnprocessableEntity(new { error = "No Monster can detected." });

            return Results.Ok(new Dtos.Photos.AnalyzeResultDto(match.Id, match.Name, match.Emoji));
        });

        // POST /api/photos — publish photo (after analyze confirmation)
        group.MapPost("/", async (
            HttpRequest request,
            ClaimsPrincipal principal,
            IStorageService storage,
            AppDbContext db) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
                return Results.BadRequest("No file provided.");
            if (!int.TryParse(request.Form["monsterId"], out var monsterId))
                return Results.BadRequest("monsterId is required.");

            var monster = await db.MonsterMappings.FindAsync(monsterId);
            if (monster == null) return Results.BadRequest("Invalid monsterId.");

            var file = request.Form.Files[0];
            var ext = Path.GetExtension(file.FileName).TrimStart('.');
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var relativePath = await storage.SaveAsync(ms.ToArray(), "photos", userId, ext);

            var photo = new Photo
            {
                UserId = userId,
                FilePath = relativePath,
                MonsterId = monsterId,
                CreatedAt = DateTime.UtcNow
            };
            db.Photos.Add(photo);
            await db.SaveChangesAsync();

            return Results.Created($"/api/photos/{photo.Id}",
                new { photoId = photo.Id, imageUrl = $"/api/images/{relativePath}" });
        });

        // DELETE /api/photos/{id}
        group.MapDelete("/{id:int}", async (
            int id,
            ClaimsPrincipal principal,
            IStorageService storage,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var photo = await db.Photos.FindAsync(id);
            if (photo == null) return Results.NotFound();
            if (photo.UserId != userId) return Results.Forbid();

            await storage.DeleteAsync(photo.FilePath);
            db.Photos.Remove(photo);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // POST /api/photos/{id}/like
        group.MapPost("/{id:int}/like", async (
            int id,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var photo = await db.Photos.FindAsync(id);
            if (photo == null) return Results.NotFound();

            var exists = await db.PhotoLikes.AnyAsync(l => l.PhotoId == id && l.UserId == userId);
            if (!exists)
            {
                db.PhotoLikes.Add(new PhotoLike { PhotoId = id, UserId = userId });
                photo.LikesCount++;
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { likesCount = photo.LikesCount });
        });

        // DELETE /api/photos/{id}/like
        group.MapDelete("/{id:int}/like", async (
            int id,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var photo = await db.Photos.FindAsync(id);
            if (photo == null) return Results.NotFound();

            var like = await db.PhotoLikes.FindAsync(id, userId);
            if (like != null)
            {
                db.PhotoLikes.Remove(like);
                photo.LikesCount = Math.Max(0, photo.LikesCount - 1);
                await db.SaveChangesAsync();
            }

            return Results.Ok(new { likesCount = photo.LikesCount });
        });

        // GET /api/photos/feed
        group.MapGet("/feed", async (
            ClaimsPrincipal principal,
            AppDbContext db,
            int limit = 10,
            int? cursor = null) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var pageSize = Math.Clamp(limit, 1, 50);

            // Get IDs of accepted friends
            var friendIds = await db.UserFriendships
                .Where(f => (f.RequesterId == userId || f.AddresseeId == userId)
                    && f.Status == FriendshipStatus.Accepted)
                .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
                .ToListAsync();

            var query = db.Photos
                .Where(p => friendIds.Contains(p.UserId))
                .OrderByDescending(p => p.Id);

            if (cursor.HasValue)
                query = (IOrderedQueryable<Photo>)query.Where(p => p.Id < cursor.Value);

            var photos = await query
                .Take(pageSize)
                .Include(p => p.User)
                .Include(p => p.Monster)
                .ToListAsync();

            var photoIds = photos.Select(p => p.Id).ToList();
            var myLikes = await db.PhotoLikes
                .Where(l => l.UserId == userId && photoIds.Contains(l.PhotoId))
                .Select(l => l.PhotoId)
                .ToHashSetAsync();

            return Results.Ok(photos.Select(p => UserEndpoints.ToDto(p, myLikes.Contains(p.Id))));
        });

        // GET /api/photos/explore
        group.MapGet("/explore", async (
            ClaimsPrincipal principal,
            AppDbContext db,
            int offset = 0,
            int limit = 20,
            int seed = 1,
            string? monsterIds = null) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var pageSize = Math.Clamp(limit, 1, 50);

            var monsterIdList = monsterIds?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s, out var i) ? (int?)i : null)
                .Where(i => i.HasValue)
                .Select(i => i!.Value)
                .ToList();

            var query = db.Photos.AsQueryable();

            if (monsterIdList?.Count > 0)
                query = query.Where(p => monsterIdList.Contains(p.MonsterId));

            // Deterministic pseudo-random order using seed
            var photos = await query
                .OrderBy(p => (p.Id * seed) % 999983) // large prime for distribution
                .Skip(offset)
                .Take(pageSize)
                .Include(p => p.User)
                .Include(p => p.Monster)
                .ToListAsync();

            var photoIds = photos.Select(p => p.Id).ToList();
            var myLikes = await db.PhotoLikes
                .Where(l => l.UserId == userId && photoIds.Contains(l.PhotoId))
                .Select(l => l.PhotoId)
                .ToHashSetAsync();

            return Results.Ok(photos.Select(p => UserEndpoints.ToDto(p, myLikes.Contains(p.Id))));
        });
    }
}
