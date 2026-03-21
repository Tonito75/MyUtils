using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MonsterHub.Api.Data;
using MonsterHub.Api.Dtos.Friends;
using MonsterHub.Api.Models;

namespace MonsterHub.Api.Endpoints;

public static class FriendEndpoints
{
    public static void MapFriendEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/friends").RequireAuthorization();

        // GET /api/friends — accepted friends
        group.MapGet("/", async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var friendships = await db.UserFriendships
                .Where(f => (f.RequesterId == userId || f.AddresseeId == userId)
                    && f.Status == FriendshipStatus.Accepted)
                .Include(f => f.Requester)
                .Include(f => f.Addressee)
                .ToListAsync();

            var friends = friendships.Select(f =>
            {
                var friend = f.RequesterId == userId ? f.Addressee : f.Requester;
                return new FriendDto(friend.Id, friend.UserName!,
                    friend.ProfilePicturePath != null ? $"/api/images/{friend.ProfilePicturePath}" : null);
            });

            return Results.Ok(friends);
        });

        // GET /api/friends/requests — pending requests received
        group.MapGet("/requests", async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var requests = await db.UserFriendships
                .Where(f => f.AddresseeId == userId && f.Status == FriendshipStatus.Pending)
                .Include(f => f.Requester)
                .ToListAsync();

            return Results.Ok(requests.Select(f => new FriendDto(
                f.Requester.Id, f.Requester.UserName!,
                f.Requester.ProfilePicturePath != null
                    ? $"/api/images/{f.Requester.ProfilePicturePath}"
                    : null)));
        });

        // POST /api/friends/{userId} — send request
        group.MapPost("/{targetUserId}", async (
            string targetUserId,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (userId == targetUserId) return Results.BadRequest("Cannot add yourself.");

            var existing = await db.UserFriendships.FindAsync(userId, targetUserId)
                ?? await db.UserFriendships.FindAsync(targetUserId, userId);
            if (existing != null) return Results.Conflict("Friendship already exists or pending.");

            var target = await db.Users.FindAsync(targetUserId);
            if (target == null) return Results.NotFound();

            db.UserFriendships.Add(new UserFriendship
            {
                RequesterId = userId,
                AddresseeId = targetUserId,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });

            db.Notifications.Add(new Notification
            {
                RecipientId = targetUserId,
                Type = NotificationType.FriendRequest,
                RelatedEntityId = userId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });

            await db.SaveChangesAsync();
            return Results.Ok();
        });

        // PUT /api/friends/{requesterId}/accept
        group.MapPut("/{requesterId}/accept", async (
            string requesterId,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var friendship = await db.UserFriendships.FindAsync(requesterId, userId);
            if (friendship == null) return Results.NotFound();

            friendship.Status = FriendshipStatus.Accepted;
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        // DELETE /api/friends/{otherUserId} — decline or remove
        group.MapDelete("/{otherUserId}", async (
            string otherUserId,
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var friendship = await db.UserFriendships.FindAsync(userId, otherUserId)
                ?? await db.UserFriendships.FindAsync(otherUserId, userId);

            if (friendship == null) return Results.NotFound();

            db.UserFriendships.Remove(friendship);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
