using Danelix.Modules.Tenancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Danelix.Modules.Tenancy.Infrastructure.Persistence;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("branches", table =>
            table.HasCheckConstraint(
                "ck_branches_code_format",
                "char_length(code) BETWEEN 2 AND 20 AND code ~ '^[A-Z0-9-]+$'"));

        builder.HasKey(branch => branch.Id)
            .HasName("pk_branches");

        builder.Property(branch => branch.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(branch => branch.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(branch => branch.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(branch => branch.Code)
            .HasColumnName("code")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(branch => branch.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(branch => branch.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasAlternateKey(branch => new { branch.TenantId, branch.Id })
            .HasName("ak_branches_tenant_id_id");

        builder.HasIndex(branch => new { branch.TenantId, branch.Code })
            .IsUnique()
            .HasDatabaseName("ux_branches_tenant_id_code");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(branch => branch.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
