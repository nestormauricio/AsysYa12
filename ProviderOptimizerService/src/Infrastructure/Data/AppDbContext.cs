using Microsoft.EntityFrameworkCore;
using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Infrastructure.Data.Configurations;

namespace ProviderOptimizerService.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<AssistanceRequest> AssistanceRequests => Set<AssistanceRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProviderConfiguration());
        modelBuilder.ApplyConfiguration(new ApplicationUserConfiguration());
        modelBuilder.ApplyConfiguration(new AssistanceRequestConfiguration());
        base.OnModelCreating(modelBuilder);
    }
}
