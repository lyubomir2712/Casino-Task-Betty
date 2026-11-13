using Games.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Games.Api.Persistence;

public class GamesDbContext(DbContextOptions<GamesDbContext> options) : DbContext(options)
{
    public DbSet<Round>  Rounds => Set<Round>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GamesDbContext).Assembly);
    }
}