using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MonsterHub.Api.Data;
using MonsterHub.Api.Dtos.Notifications;

namespace MonsterHub.Api.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        app.MapGet("/api/notifications", async (
            ClaimsPrincipal principal,
            AppDbContext db) =>
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var notifications = await db.Notifications
                .Where(n => n.RecipientId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Include(n => n.Recipient)
                .ToListAsync();

            // Intentional: GET with write side-effect for simplicity (spec §5)
            var unread = notifications.Where(n => !n.IsRead).ToList();
            if (unread.Count > 0)
            {
                unread.ForEach(n => n.IsRead = true);
                await db.SaveChangesAsync();
            }

            // Load related user info for each notification
            var senderIds = notifications.Select(n => n.RelatedEntityId).Distinct().ToList();
            var senders = await db.Users
                .Where(u => senderIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            return Results.Ok(notifications.Select(n =>
            {
                senders.TryGetValue(n.RelatedEntityId, out var sender);
                return new NotificationDto(
                    n.Id, n.Type, n.RelatedEntityId,
                    sender?.UserName ?? "Unknown",
                    sender?.ProfilePicturePath != null
                        ? $"/api/images/{sender.ProfilePicturePath}"
                        : null,
                    n.CreatedAt);
            }));
        }).RequireAuthorization();
    }
}
