using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProviderOptimizerService.Domain.Interfaces;
using ProviderOptimizerService.Infrastructure.Data;
using ProviderOptimizerService.Infrastructure.Repositories;
using ProviderOptimizerService.Infrastructure.Services;
using StackExchange.Redis;

namespace ProviderOptimizerService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Redis
        var redisConn = configuration["Redis:ConnectionString"] ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn));

        // Repositories
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<ICacheService, RedisCacheService>();
        services.AddScoped<IOptimizationService, WeightedScoringOptimizationService>();

        // Health checks
        services.AddHealthChecks()
            .AddNpgsql(configuration.GetConnectionString("DefaultConnection")!, name: "postgres")
            .AddRedis(redisConn, name: "redis");

        return services;
    }
}
