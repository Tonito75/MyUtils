namespace MonsterHub.Api.Dtos.Users;
public record UpdateProfileRequest(string CurrentPassword, string? NewEmail, string? NewPassword);
