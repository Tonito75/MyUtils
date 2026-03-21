namespace MonsterHub.Api.Models;

public class PhotoLike
{
    public int PhotoId { get; set; }
    public Photo Photo { get; set; } = null!;
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
}
