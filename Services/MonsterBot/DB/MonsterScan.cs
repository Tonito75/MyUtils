namespace MonsterBot.DB;

public class MonsterScan
{
    public int Id { get; set; }
    public required string Nom { get; set; }
    public required string UtilisateurDiscord { get; set; }
    public DateTime Date { get; set; }
}
