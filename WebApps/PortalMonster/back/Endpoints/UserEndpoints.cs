using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MonsterHub.Api.Data;
using MonsterHub.Api.Dtos.Users;
using MonsterHub.Api.Models;
using MonsterHub.Api.Services;

namespace MonsterHub.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").RequireAuthorization();

        group.MapGet("/me", async (
            ClaimsPrincipal principal,
            UserManager<AppUser> userManager,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return Results.NotFound();

            var photoCount = await db.Photos.CountAsync(p => p.UserId == userId);
            var friendCount = await db.UserFriendships.CountAsync(f =>
                (f.RequesterId == userId || f.AddresseeId == userId)
                && f.Status == FriendshipStatus.Accepted);

            return Results.Ok(new UserDto(user.Id, user.UserName!, user.Email!,
                user.ProfilePicturePath != null ? $"/api/images/{user.ProfilePicturePath}" : null,
                photoCount, friendCount));
        });

        group.MapPut("/me", async (
            UpdateProfileRequest req,
            ClaimsPrincipal principal,
            UserManager<AppUser> userManager) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await userManager.FindByIdAsync(userId);
            if (user == null) return Results.NotFound();

            if (!await userManager.CheckPasswordAsync(user, req.CurrentPassword))
                return Results.BadRequest(new { error = "Current password is incorrect." });

            if (!string.IsNullOrWhiteSpace(req.NewPassword))
            {
                var pwResult = await userManager.ChangePasswordAsync(user, req.CurrentPassword, req.NewPassword);
                if (!pwResult.Succeeded)
                    return Results.BadRequest(pwResult.Errors.Select(e => e.Description));
            }

            if (!string.IsNullOrWhiteSpace(req.NewEmail) && req.NewEmail != user.Email)
            {
                // Use Identity's proper pipeline (updates SecurityStamp + NormalizedEmail)
                await userManager.SetEmailAsync(user, req.NewEmail);
            }

            return Results.Ok(new { email = user.Email, username = user.UserName });
        });

        group.MapPut("/me/avatar", async (
            HttpRequest request,
            ClaimsPrincipal principal,
            UserManager<AppUser> userManager,
            IStorageService storage) =>
        {
            if (!request.HasFormContentType || request.Form.Files.Count == 0)
                return Results.BadRequest("No file provided.");

            var file = request.Form.Files[0];
            var ext = Path.GetExtension(file.FileName).TrimStart('.');
            if (ext is not ("jpg" or "jpeg" or "png" or "gif" or "webp"))
                return Results.BadRequest("Unsupported image format.");

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var relativePath = await storage.SaveAsync(ms.ToArray(), "avatars", userId, ext);

            var user = await userManager.FindByIdAsync(userId);
            user!.ProfilePicturePath = relativePath;
            await userManager.UpdateAsync(user);

            return Results.Ok(new { avatarUrl = $"/api/images/{relativePath}" });
        });

        group.MapGet("/search", async (
            string q,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Get IDs of users already in a friendship with current user
            var relatedIds = await db.UserFriendships
                .Where(f => f.RequesterId == userId || f.AddresseeId == userId)
                .Select(f => f.RequesterId == userId ? f.AddresseeId : f.RequesterId)
                .ToListAsync();

            var users = await db.Users
                .Where(u => u.Id != userId
                    && !relatedIds.Contains(u.Id)
                    && u.UserName!.Contains(q))
                .Take(20)
                .Select(u => new FriendSearchResult(
                    u.Id, u.UserName!,
                    u.ProfilePicturePath != null ? $"/api/images/{u.ProfilePicturePath}" : null))
                .ToListAsync();

            return Results.Ok(users);
        });

        group.MapGet("/{id}/photos", async (
            string id,
            int limit,
            int? cursor,
            AppDbContext db,
            ClaimsPrincipal principal) =>
        {
            var currentUserId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var pageSize = Math.Clamp(limit == 0 ? 10 : limit, 1, 50);

            var query = db.Photos
                .Where(p => p.UserId == id)
                .OrderByDescending(p => p.Id);

            if (cursor.HasValue)
                query = (IOrderedQueryable<Photo>)query.Where(p => p.Id < cursor.Value);

            var photos = await query
                .Take(pageSize)
                .Include(p => p.User)
                .Include(p => p.Monster)
                .ToListAsync();

            var likedIds = photos.Select(p => p.Id).ToList();
            var myLikes = await db.PhotoLikes
                .Where(l => l.UserId == currentUserId && likedIds.Contains(l.PhotoId))
                .Select(l => l.PhotoId)
                .ToHashSetAsync();

            return Results.Ok(photos.Select(p => ToDto(p, myLikes.Contains(p.Id))));
        });
    }

    internal static object ToDto(Models.Photo p, bool likedByMe) => new Dtos.Photos.PhotoDto(
        p.Id,
        $"/api/images/{p.FilePath}",
        p.UserId,
        p.User.UserName!,
        p.User.ProfilePicturePath != null ? $"/api/images/{p.User.ProfilePicturePath}" : null,
        p.CreatedAt,
        p.MonsterId,
        p.Monster.Name,
        p.Monster.Emoji,
        p.LikesCount,
        likedByMe);

    private record FriendSearchResult(string UserId, string Username, string? AvatarUrl);
}
