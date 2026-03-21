using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MonsterHub.Api.Models;

namespace MonsterHub.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<PhotoLike> PhotoLikes => Set<PhotoLike>();
    public DbSet<UserFriendship> UserFriendships => Set<UserFriendship>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<MonsterMapping> MonsterMappings => Set<MonsterMapping>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<PhotoLike>()
            .HasKey(l => new { l.PhotoId, l.UserId });

        builder.Entity<UserFriendship>()
            .HasKey(f => new { f.RequesterId, f.AddresseeId });

        builder.Entity<UserFriendship>()
            .HasOne(f => f.Requester)
            .WithMany()
            .HasForeignKey(f => f.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserFriendship>()
            .HasOne(f => f.Addressee)
            .WithMany()
            .HasForeignKey(f => f.AddresseeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Photo>()
            .HasOne(p => p.User)
            .WithMany(u => u.Photos)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PhotoLike>()
            .HasOne(l => l.Photo)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PhotoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PhotoLike>()
            .HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed MonsterMapping
        builder.Entity<MonsterMapping>().HasData(GetMonsterSeed());
    }

    private static MonsterMapping[] GetMonsterSeed() =>
    [
        new() { Id = 1,  Name = "Ultra White",            Emoji = "⚪", KeywordsJson = """["ultra white","white monster","the white monster"]""" },
        new() { Id = 2,  Name = "Ultra Paradise",         Emoji = "🟢", KeywordsJson = """["ultra paradise"]""" },
        new() { Id = 3,  Name = "Ultra Black",            Emoji = "⚫", KeywordsJson = """["ultra black"]""" },
        new() { Id = 4,  Name = "Ultra Fiesta",           Emoji = "🔵", KeywordsJson = """["ultra fiesta"]""" },
        new() { Id = 5,  Name = "Ultra Red",              Emoji = "🔴", KeywordsJson = """["ultra red","red"]""" },
        new() { Id = 6,  Name = "Ultra Violet",           Emoji = "🟣", KeywordsJson = """["ultra violet","purple monster","the purple monster"]""" },
        new() { Id = 7,  Name = "Ultra Gold",             Emoji = "🟡", KeywordsJson = """["ultra gold","gold"]""" },
        new() { Id = 8,  Name = "Ultra Blue",             Emoji = "🔵", KeywordsJson = """["ultra blue","blue monster","the blue monster","blue"]""" },
        new() { Id = 9,  Name = "Ultra Watermelon",       Emoji = "🔴", KeywordsJson = """["ultra watermelon"]""" },
        new() { Id = 10, Name = "Ultra Rosé",             Emoji = "🟣", KeywordsJson = """["ultra rosé","ultra rose"]""" },
        new() { Id = 11, Name = "Ultra Strawberry Dreams",Emoji = "🟣", KeywordsJson = """["ultra strawberry","strawberry dreams"]""" },
        new() { Id = 12, Name = "Ultra Fantasy",          Emoji = "🟣", KeywordsJson = """["ultra fantasy"]""" },
        new() { Id = 13, Name = "Energy Nitro",           Emoji = "⚫", KeywordsJson = """["nitro"]""" },
        new() { Id = 14, Name = "VR46",                   Emoji = "🟡", KeywordsJson = """["vr46","the doctor","valentino"]""" },
        new() { Id = 15, Name = "Full Throttle",          Emoji = "🔵", KeywordsJson = """["full throttle"]""" },
        new() { Id = 16, Name = "Lando Norris",           Emoji = "🟣", KeywordsJson = """["lando norris","lambo norris","norris"]""" },
        new() { Id = 17, Name = "The Original",           Emoji = "🟢", KeywordsJson = """["original green","the original","original energy"]""" },
        new() { Id = 18, Name = "Aussie Lemonade",        Emoji = "🟡", KeywordsJson = """["aussie lemonade","aussie"]""" },
        new() { Id = 19, Name = "Pacific Punch",          Emoji = "🟤", KeywordsJson = """["pacific"]""" },
        new() { Id = 20, Name = "Mango Loco",             Emoji = "🔵", KeywordsJson = """["mango loco","mango"]""" },
        new() { Id = 21, Name = "Khaos",                  Emoji = "🟠", KeywordsJson = """["khaos"]""" },
        new() { Id = 22, Name = "Ripper",                 Emoji = "🩷", KeywordsJson = """["ripper"]""" },
        new() { Id = 23, Name = "Pipeline Punch",         Emoji = "🩷", KeywordsJson = """["pipeline"]""" },
        new() { Id = 24, Name = "Mixxd",                  Emoji = "🟣", KeywordsJson = """["mixxd","mixd"]""" },
    ];
}
