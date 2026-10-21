using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Danelix.Modules.Tenancy.Infrastructure.Persistence;

public sealed class TenancyDbContextFactory : IDesignTimeDbContextFactory<TenancyDbContext>
{
    private const string ConnectionStringEnvironmentVariable = "DANELIX_TENANCY_CONNECTION_STRING";

    public TenancyDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Set the {ConnectionStringEnvironmentVariable} environment variable to run EF Core tooling.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<TenancyDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            postgresOptions => postgresOptions.MigrationsHistoryTable(TenancyDbContext.MigrationsHistoryTableName));

        return new TenancyDbContext(optionsBuilder.Options);
    }
}
