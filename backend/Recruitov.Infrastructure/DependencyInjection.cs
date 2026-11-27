using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Recruitov.Application.Interfaces;
using Recruitov.Infrastructure.Data;
using Recruitov.Infrastructure.Data.DataServices;
using Recruitov.Infrastructure.Data.Repositories;
using Recruitov.Infrastructure.Services;

namespace Recruitov.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Generic Repository (Dukaan Clean Architecture pattern)
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Feature Data Services (Optimized SQL projections & Npgsql queries)
        services.AddScoped<IAdminDataService, AdminDataService>();
        services.AddScoped<IAttributeDataService, AttributeDataService>();
        services.AddScoped<IAuthDataService, AuthDataService>();
        services.AddScoped<IDashboardDataService, DashboardDataService>();
        services.AddScoped<IPositionDataService, PositionDataService>();
        services.AddScoped<IProfileDataService, ProfileDataService>();
        services.AddScoped<ISearchDataService, SearchDataService>();
        services.AddScoped<ISupportTicketDataService, SupportTicketDataService>();

        // Security, Domain Evaluation & External Services
        services.AddScoped<IClaimsTransformation, SupabaseClaimsTransformation>();
        services.AddSingleton<ICloudinaryService, CloudinaryService>();
        services.AddHttpClient();
        services.AddScoped<ICloudStorageService, CloudStorageService>();
        services.AddScoped<IPositionAccessEvaluator, PositionAccessEvaluator>();
        services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();

        // Automatic startup migrations (Dukaan DatabaseMigrationHostedService pattern)
        services.AddHostedService<DatabaseMigrationHostedService>();

        return services;
    }
}

public class DatabaseMigrationHostedService(
    IServiceProvider serviceProvider,
    ILogger<DatabaseMigrationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        logger.LogInformation("Applying pending EF Core database migrations...");
        await db.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Database migrations applied.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
