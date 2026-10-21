using Danelix.Modules.Tenancy.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Danelix.Modules.Tenancy.Infrastructure.Persistence;

public static class TenancyPersistenceExtensions
{
    public static IServiceCollection AddTenancyPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("La conexión de Tenancy es obligatoria.", nameof(connectionString));
        }

        services.AddDbContext<TenancyDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                postgresOptions => postgresOptions.MigrationsHistoryTable(TenancyDbContext.MigrationsHistoryTableName)));
        services.AddScoped<ITenantMembershipAdministration, EfTenantMembershipAdministration>();

        return services;
    }

    public static async Task InitializeTenancyDevelopmentAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await TenancyDevelopmentDataSeeder.SeedAsync(dbContext, cancellationToken);
    }
}
