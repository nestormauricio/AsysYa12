using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProviderOptimizerService.Domain.Entities;
using ProviderOptimizerService.Domain.Interfaces;
using ProviderOptimizerService.Domain.ValueObjects;
using ProviderOptimizerService.Infrastructure.Data;
using StackExchange.Redis;
using Moq;
using Xunit;

namespace IntegrationTests;

public class ProviderOptimizerWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebApplication(Microsoft.AspNetCore.Builder.WebApplicationBuilder builder)
    {
        // Not used in this override pattern
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace real DB with in-memory
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);
            services.AddDbContext<AppDbContext>(opts => opts.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

            // Replace Redis with mock
            var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
            if (redisDescriptor != null) services.Remove(redisDescriptor);
            var mockRedis = new Mock<IConnectionMultiplexer>();
            var mockDb = new Mock<IDatabase>();
            mockRedis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDb.Object);
            mockDb.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(RedisValue.Null);
            mockDb.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                  It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                  .ReturnsAsync(true);
            services.AddSingleton(mockRedis.Object);

            // Replace health checks that need real infrastructure
            var healthDescriptors = services.Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true).ToList();
            foreach (var d in healthDescriptors) services.Remove(d);
            services.AddHealthChecks();
        });
    }
}

public class OptimizationIntegrationTests : IClassFixture<ProviderOptimizerWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ProviderOptimizerWebApplicationFactory _factory;

    public OptimizationIntegrationTests(ProviderOptimizerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAvailableProviders_Unauthenticated_ShouldReturn401()
    {
        var response = await _client.GetAsync("/api/providers/available");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostOptimize_Unauthenticated_ShouldReturn401()
    {
        var response = await _client.PostAsJsonAsync("/optimize", new
        {
            Latitude = -12.046374,
            Longitude = -77.042793,
            RequiredType = (int?)null,
            Weights = (object?)null
        });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RegisterAndLogin_ValidCredentials_ShouldReturnToken()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "SecurePass123!"
        });

        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        content!.Token.Should().NotBeNullOrEmpty();
    }

    private record AuthResponse(string Token, DateTime ExpiresAt, string Username, string Email, string Role);
}
