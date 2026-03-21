namespace MonsterHub.Api.Dtos.Users;
public record UserDto(string Id, string Username, string Email, string? AvatarUrl,
    int PhotoCount, int FriendCount);
