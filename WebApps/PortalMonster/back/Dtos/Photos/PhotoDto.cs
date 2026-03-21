namespace MonsterHub.Api.Dtos.Photos;
public record PhotoDto(int Id, string ImageUrl, string UserId, string Username,
    string? AvatarUrl, DateTime CreatedAt, int MonsterId, string MonsterName,
    string MonsterEmoji, int LikesCount, bool LikedByMe);
