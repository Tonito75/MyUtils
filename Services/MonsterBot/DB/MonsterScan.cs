namespace MonsterBot.DB;

public class MonsterScan
{
    public int Id { get; set; }
    public required string Couleur { get; set; }
    public required string UtilisateurDiscord { get; set; }
    public DateTime Date { get; set; }
}
