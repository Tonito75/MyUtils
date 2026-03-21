namespace MonsterHub.Api.Models;

public enum NotificationType { FriendRequest }

public class Notification
{
    public int Id { get; set; }
    public string RecipientId { get; set; } = "";
    public AppUser Recipient { get; set; } = null!;
    public NotificationType Type { get; set; }
    public string RelatedEntityId { get; set; } = "";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
