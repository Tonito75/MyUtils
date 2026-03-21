namespace MonsterHub.Api.Models;

public class Photo
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser User { get; set; } = null!;
    public string FilePath { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int MonsterId { get; set; }
    public MonsterMapping Monster { get; set; } = null!;
    public int LikesCount { get; set; }
    public ICollection<PhotoLike> Likes { get; set; } = [];
}
