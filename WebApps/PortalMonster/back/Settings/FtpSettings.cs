namespace MonsterHub.Api.Settings;

public class FtpSettings
{
    public string Host { get; set; } = "";
    public string Port { get; set; } = "21";
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
    public string BaseRemotePath { get; set; } = "/monsterhub";
}
