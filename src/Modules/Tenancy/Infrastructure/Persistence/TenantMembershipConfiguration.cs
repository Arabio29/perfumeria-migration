using Danelix.Modules.Tenancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Danelix.Modules.Tenancy.Infrastructure.Persistence;

internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("tenant_memberships", table =>
        {
            table.HasCheckConstraint("ck_tenant_memberships_role", "role IN ('Owner', 'Admin', 'Member')");
            table.HasCheckConstraint(
                "ck_tenant_memberships_status",
                "status IN ('Active', 'Suspended', 'Revoked')");
        });

        builder.HasKey(membership => membership.Id)
            .HasName("pk_tenant_memberships");

        builder.Property(membership => membership.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(membership => membership.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        // UserId is an opaque Identity ID; ADR-003 intentionally has no cross-module FK.
        builder.Property(membership => membership.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(membership => membership.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(membership => membership.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(membership => membership.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasAlternateKey(membership => new { membership.TenantId, membership.UserId })
            .HasName("ak_tenant_memberships_tenant_id_user_id");

        builder.HasIndex(membership => membership.UserId)
            .HasDatabaseName("ix_tenant_memberships_user_id");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(membership => membership.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
