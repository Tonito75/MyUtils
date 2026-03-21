using Microsoft.EntityFrameworkCore;
using MonsterHub.Api.Data;

namespace MonsterHub.Api.Endpoints;

public static class MonsterEndpoints
{
    public static void MapMonsterEndpoints(this WebApplication app)
    {
        app.MapGet("/api/monsters", async (AppDbContext db) =>
        {
            var monsters = await db.MonsterMappings
                .OrderBy(m => m.Id)
                .Select(m => new { m.Id, m.Name, m.Emoji })
                .ToListAsync();
            return Results.Ok(monsters);
        }).RequireAuthorization();
    }
}
