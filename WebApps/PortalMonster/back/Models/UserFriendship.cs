namespace MonsterHub.Api.Models;

public enum FriendshipStatus { Pending, Accepted }

public class UserFriendship
{
    public string RequesterId { get; set; } = "";
    public AppUser Requester { get; set; } = null!;
    public string AddresseeId { get; set; } = "";
    public AppUser Addressee { get; set; } = null!;
    public FriendshipStatus Status { get; set; } = FriendshipStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
