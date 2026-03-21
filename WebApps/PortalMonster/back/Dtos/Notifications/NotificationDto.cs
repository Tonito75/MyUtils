using MonsterHub.Api.Models;
namespace MonsterHub.Api.Dtos.Notifications;
public record NotificationDto(int Id, NotificationType Type, string RelatedUserId,
    string RelatedUsername, string? RelatedAvatarUrl, DateTime CreatedAt);
