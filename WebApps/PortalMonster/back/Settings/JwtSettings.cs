namespace MonsterHub.Api.Settings;

public class JwtSettings
{
    public string Secret { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public int ExpiresInDays { get; set; } = 30;
}
