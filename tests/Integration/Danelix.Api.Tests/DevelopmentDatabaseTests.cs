using System.Globalization;
using Danelix.Modules.Identity.Infrastructure.Persistence;
using Danelix.Modules.Tenancy.Application;
using Danelix.Modules.Tenancy.Domain;
using Danelix.Modules.Tenancy.Infrastructure.Persistence;
using Danelix.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Danelix.Api.Tests;

public class DevelopmentDatabaseTests
{
    [Fact]
    public async Task DevelopmentInitialization_MigratesAndSeedsIdempotently_AndEnforcesTenantConstraints()
    {
        await using var postgres = new PostgreSqlBuilder("pgvector/pgvector:pg17")
            .WithDatabase("danelix_test")
            .WithUsername("danelix")
            .WithPassword("danelix_test")
            .Build();

        await postgres.StartAsync();

        var connectionString = postgres.GetConnectionString();
        await using var serviceProvider = new ServiceCollection()
            .AddIdentityPersistence(connectionString)
            .AddTenancyPersistence(connectionString)
            .BuildServiceProvider();

        await serviceProvider.InitializeIdentityDevelopmentAsync();
        await serviceProvider.InitializeTenancyDevelopmentAsync();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        await AssertSeedCountsAsync(connection);
        Assert.Equal(2L, await ExecuteCountAsync(connection, "SELECT count(*) FROM __ef_migrations_history_identity"));
        Assert.Equal(1L, await ExecuteCountAsync(connection, "SELECT count(*) FROM __ef_migrations_history_tenancy"));

        var camilaId = await ExecuteGuidAsync(
            connection,
            "SELECT id FROM users WHERE normalized_email = @email",
            ("email", "camila@danelix.local"));
        var marioId = await ExecuteGuidAsync(
            connection,
            "SELECT id FROM users WHERE normalized_email = @email",
            ("email", "mario@danelix.local"));
        var diegoId = await ExecuteGuidAsync(
            connection,
            "SELECT id FROM users WHERE normalized_email = @email",
            ("email", "diego@danelix.local"));
        var valentinaId = await ExecuteGuidAsync(
            connection,
            "SELECT id FROM users WHERE normalized_email = @email",
            ("email", "valentina@danelix.local"));
        var auroraTenantId = await ExecuteGuidAsync(
            connection,
            "SELECT id FROM tenants WHERE slug = @slug",
            ("slug", "aurora"));
        var brisaTenantId = await ExecuteGuidAsync(
            connection,
            "SELECT id FROM tenants WHERE slug = @slug",
            ("slug", "brisa"));
        var auroraCentralBranchId = await ExecuteGuidAsync(
            connection,
            "SELECT id FROM branches WHERE tenant_id = @tenant_id AND code = @code",
            ("tenant_id", auroraTenantId),
            ("code", "CENTRAL"));
        var brisaBranchId = await ExecuteGuidAsync(
            connection,
            "SELECT id FROM branches WHERE tenant_id = @tenant_id AND code = @code",
            ("tenant_id", brisaTenantId),
            ("code", "MAIN"));

        Assert.Equal(
            2L,
            await ExecuteCountAsync(
                connection,
                "SELECT count(*) FROM branch_memberships WHERE user_id = @user_id AND role = 'Manager'",
                ("user_id", camilaId)));

        Assert.Equal(
            1L,
            await ExecuteCountAsync(
                connection,
                "SELECT count(*) FROM platform_roles WHERE user_id = @user_id AND role = 'PlatformAdmin'",
                ("user_id", marioId)));
        Assert.Equal(
            1L,
            await ExecuteCountAsync(
                connection,
                "SELECT count(*) FROM platform_roles WHERE user_id = @user_id AND role = 'SupportOperator'",
                ("user_id", valentinaId)));

        Assert.Equal(
            "23503",
            await ExpectPostgresErrorAsync(
                connection,
                "INSERT INTO platform_roles (user_id, role, granted_at_utc) VALUES (@user_id, 'PlatformAdmin', @granted_at_utc)",
                ("user_id", Guid.NewGuid()),
                ("granted_at_utc", DateTimeOffset.UtcNow)));

        Assert.Equal(
            "23505",
            await ExpectPostgresErrorAsync(
                connection,
                "INSERT INTO platform_roles (user_id, role, granted_at_utc) VALUES (@user_id, 'PlatformAdmin', @granted_at_utc)",
                ("user_id", marioId),
                ("granted_at_utc", DateTimeOffset.UtcNow)));

        Assert.Equal(
            "23514",
            await ExpectPostgresErrorAsync(
                connection,
                "INSERT INTO platform_roles (user_id, role, granted_at_utc) VALUES (@user_id, 'SuperBoss', @granted_at_utc)",
                ("user_id", valentinaId),
                ("granted_at_utc", DateTimeOffset.UtcNow)));

        Assert.Equal(
            "23503",
            await ExpectPostgresErrorAsync(
                connection,
                """
                INSERT INTO branch_memberships
                    (id, tenant_id, branch_id, user_id, role, status, created_at_utc)
                VALUES (@id, @tenant_id, @branch_id, @user_id, 'Staff', 'Active', @created_at_utc)
                """,
                ("id", Guid.NewGuid()),
                ("tenant_id", auroraTenantId),
                ("branch_id", brisaBranchId),
                ("user_id", camilaId),
                ("created_at_utc", DateTimeOffset.UtcNow)));

        Assert.Equal(
            "23503",
            await ExpectPostgresErrorAsync(
                connection,
                """
                INSERT INTO branch_memberships
                    (id, tenant_id, branch_id, user_id, role, status, created_at_utc)
                VALUES (@id, @tenant_id, @branch_id, @user_id, 'Staff', 'Active', @created_at_utc)
                """,
                ("id", Guid.NewGuid()),
                ("tenant_id", brisaTenantId),
                ("branch_id", brisaBranchId),
                ("user_id", camilaId),
                ("created_at_utc", DateTimeOffset.UtcNow)));

        Assert.Equal(
            "23505",
            await ExpectPostgresErrorAsync(
                connection,
                """
                INSERT INTO branch_memberships
                    (id, tenant_id, branch_id, user_id, role, status, created_at_utc)
                VALUES (@id, @tenant_id, @branch_id, @user_id, 'Staff', 'Active', @created_at_utc)
                """,
                ("id", Guid.NewGuid()),
                ("tenant_id", auroraTenantId),
                ("branch_id", auroraCentralBranchId),
                ("user_id", camilaId),
                ("created_at_utc", DateTimeOffset.UtcNow)));

        Assert.Equal(
            "23505",
            await ExpectPostgresErrorAsync(
                connection,
                """
                INSERT INTO tenant_memberships
                    (id, tenant_id, user_id, role, status, created_at_utc)
                VALUES (@id, @tenant_id, @user_id, 'Member', 'Active', @created_at_utc)
                """,
                ("id", Guid.NewGuid()),
                ("tenant_id", auroraTenantId),
                ("user_id", camilaId),
                ("created_at_utc", DateTimeOffset.UtcNow)));

        Assert.Equal(
            "23505",
            await ExpectPostgresErrorAsync(
                connection,
                """
                INSERT INTO users (id, normalized_email, display_name, status, created_at_utc)
                VALUES (@id, 'mario@danelix.local', 'Duplicate Mario', 'Active', @created_at_utc)
                """,
                ("id", Guid.NewGuid()),
                ("created_at_utc", DateTimeOffset.UtcNow)));

        await serviceProvider.InitializeIdentityDevelopmentAsync();
        await serviceProvider.InitializeTenancyDevelopmentAsync();
        await AssertSeedCountsAsync(connection);

        var marioMembershipId = await ExecuteGuidAsync(
            connection,
            "SELECT id FROM tenant_memberships WHERE tenant_id = @tenant_id AND user_id = @user_id",
            ("tenant_id", auroraTenantId),
            ("user_id", marioId));
        var secondOwnerMembershipId = Guid.NewGuid();
        await ExecuteAsync(
            connection,
            """
            INSERT INTO tenant_memberships
                (id, tenant_id, user_id, role, status, created_at_utc)
            VALUES (@id, @tenant_id, @user_id, 'Owner', 'Active', @created_at_utc)
            """,
            ("id", secondOwnerMembershipId),
            ("tenant_id", auroraTenantId),
            ("user_id", diegoId),
            ("created_at_utc", DateTimeOffset.UtcNow));

        var updateResults = await Task.WhenAll(
            TryDemoteOwnerAsync(serviceProvider, auroraTenantId, marioMembershipId),
            TryDemoteOwnerAsync(serviceProvider, auroraTenantId, secondOwnerMembershipId));

        Assert.Equal(1, updateResults.Count(succeeded => succeeded));
        Assert.Equal(1, updateResults.Count(succeeded => !succeeded));
        Assert.Equal(
            1L,
            await ExecuteCountAsync(
                connection,
                "SELECT count(*) FROM tenant_memberships WHERE tenant_id = @tenant_id AND role = 'Owner' AND status = 'Active'",
                ("tenant_id", auroraTenantId)));
    }

    private static async Task<bool> TryDemoteOwnerAsync(
        IServiceProvider services,
        Guid tenantId,
        Guid membershipId)
    {
        await using var scope = services.CreateAsyncScope();
        var administration = scope.ServiceProvider.GetRequiredService<ITenantMembershipAdministration>();

        try
        {
            await administration.ChangeAsync(
                tenantId,
                membershipId,
                TenantRole.Member,
                MembershipStatus.Active);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }

    private static async Task AssertSeedCountsAsync(NpgsqlConnection connection)
    {
        Assert.Equal(6L, await ExecuteCountAsync(connection, "SELECT count(*) FROM users"));
        Assert.Equal(2L, await ExecuteCountAsync(connection, "SELECT count(*) FROM platform_roles"));
        Assert.Equal(2L, await ExecuteCountAsync(connection, "SELECT count(*) FROM tenants"));
        Assert.Equal(3L, await ExecuteCountAsync(connection, "SELECT count(*) FROM branches"));
        Assert.Equal(6L, await ExecuteCountAsync(connection, "SELECT count(*) FROM tenant_memberships"));
        Assert.Equal(4L, await ExecuteCountAsync(connection, "SELECT count(*) FROM branch_memberships"));
    }

    private static async Task<long> ExecuteCountAsync(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        AddParameters(command, parameters);
        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private static async Task<Guid> ExecuteGuidAsync(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        AddParameters(command, parameters);
        var result = await command.ExecuteScalarAsync();
        return Assert.IsType<Guid>(result);
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        AddParameters(command, parameters);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> ExpectPostgresErrorAsync(
        NpgsqlConnection connection,
        string sql,
        params (string Name, object Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        AddParameters(command, parameters);
        var exception = await Assert.ThrowsAsync<PostgresException>(() => command.ExecuteNonQueryAsync());
        return exception.SqlState;
    }

    private static void AddParameters(NpgsqlCommand command, IEnumerable<(string Name, object Value)> parameters)
    {
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
    }
}
