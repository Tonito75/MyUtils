namespace MonsterHub.Api.Models;

public class MonsterMapping
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Emoji { get; set; } = "";
    public string? Color { get; set; }
    public string KeywordsJson { get; set; } = "[]";
    public ICollection<Photo> Photos { get; set; } = [];
}
