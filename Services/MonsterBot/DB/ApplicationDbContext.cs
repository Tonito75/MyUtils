namespace MonsterBot.DB;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<MonsterScan> MonsterScans { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MonsterScan>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Couleur).HasMaxLength(100).IsRequired();
            entity.Property(e => e.UtilisateurDiscord).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Date).HasColumnType("datetime2");
        });

        base.OnModelCreating(modelBuilder);
    }
}
