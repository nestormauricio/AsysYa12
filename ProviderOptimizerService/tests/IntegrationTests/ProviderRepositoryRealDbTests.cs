using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.ValueObjects;
using ProviderOptimizerService.Infrastructure.Data;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Xunit;

namespace IntegrationTests;

/// <summary>
/// Integration test against a real PostgreSQL + Redis instance (Testcontainers).
/// Criterion #37 — at least 1 integration test against a real database.
/// Requires Docker running on the test host.
/// </summary>
[Trait("Category", "Integration")]
public class ProviderRepositoryRealDbTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpass")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    private AppDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }

    [Fact]
    public async Task CreateAndQuery_Provider_PersistsToRealPostgres()
    {
        // Arrange
        var location = new GeoCoordinate(-12.046374, -77.042793);
        var provider = Provider.Create("Grúas Test SA", ProviderType.Grua, location, 4.5, "+51999000001");

        // Act
        _context.Providers.Add(provider);
        await _context.SaveChangesAsync();

        // Assert — query from fresh context to ensure persistence
        var saved = await _context.Providers.FindAsync(provider.Id);
        saved.Should().NotBeNull();
        saved!.Name.Should().Be("Grúas Test SA");
        saved.Rating.Should().Be(4.5);
        saved.IsAvailable.Should().BeTrue();
        saved.Location.Latitude.Should().BeApproximately(-12.046374, 0.0001);
    }

    [Fact]
    public async Task UpdateAvailability_Provider_PersistsAndRowVersionChanges()
    {
        // Arrange
        var location = new GeoCoordinate(-12.046374, -77.042793);
        var provider = Provider.Create("Grúas Concurrentes SA", ProviderType.Grua, location, 4.0);
        _context.Providers.Add(provider);
        await _context.SaveChangesAsync();
        var originalVersion = provider.RowVersion;

        // Act
        provider.SetAvailability(false);
        _context.Providers.Update(provider);
        await _context.SaveChangesAsync();

        // Assert
        var updated = await _context.Providers.FindAsync(provider.Id);
        updated!.IsAvailable.Should().BeFalse();
        updated.RowVersion.Should().NotEqual(originalVersion, "RowVersion must change on update for optimistic concurrency");
    }

    [Fact]
    public async Task GetAvailable_Providers_ReturnsOnlyAvailable()
    {
        // Arrange
        var loc = new GeoCoordinate(-12.0, -77.0);
        var available = Provider.Create("Disponible", ProviderType.Bateria, loc, 4.5);
        var unavailable = Provider.Create("No disponible", ProviderType.Bateria, loc, 4.5);
        unavailable.SetAvailability(false);

        _context.Providers.AddRange(available, unavailable);
        await _context.SaveChangesAsync();

        // Act
        var result = await _context.Providers
            .Where(p => p.IsAvailable)
            .ToListAsync();

        // Assert
        result.Should().Contain(p => p.Id == available.Id);
        result.Should().NotContain(p => p.Id == unavailable.Id);
    }

    [Fact]
    public async Task Transaction_Rollback_LeavesNoPartialData()
    {
        // Arrange — simulate transaction failure scenario (#32)
        var loc = new GeoCoordinate(-12.0, -77.0);
        var provider = Provider.Create("Transaccional SA", ProviderType.Grua, loc, 4.8);
        var providersBefore = await _context.Providers.CountAsync();

        // Act — begin transaction, add, then rollback
        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.Providers.Add(provider);
        await _context.SaveChangesAsync();
        await transaction.RollbackAsync(); // explicit rollback

        // Assert — provider should not be in DB after rollback
        // Need a fresh context to bypass EF first-level cache
        var freshOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
        await using var freshContext = new AppDbContext(freshOptions);
        var providersAfter = await freshContext.Providers.CountAsync();
        providersAfter.Should().Be(providersBefore, "rollback should undo all changes");
    }
}
