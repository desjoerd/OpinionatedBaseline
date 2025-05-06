using Heroes.Api.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace Heroes.Api.Application;

public class HeroesDbContext : DbContext
{
    public HeroesDbContext(DbContextOptions<HeroesDbContext> options)
        : base(options)
    {
    }

    public DbSet<Hero> Heroes { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HeroesDbContext).Assembly);
    }
}