using Microsoft.AspNetCore.Identity;

namespace MonsterHub.Api.Models;

public class AppUser : IdentityUser
{
    public string? ProfilePicturePath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Photo> Photos { get; set; } = [];
    public ICollection<PhotoLike> Likes { get; set; } = [];
}
