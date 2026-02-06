namespace DiscordBot.DB
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
    {
        public DbSet<LanDevice> LanDevices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LanDevice>().HasKey(e => e.Id);
            
            base.OnModelCreating(modelBuilder);
        }

    }
}
