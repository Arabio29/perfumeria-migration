using Danelix.Modules.Tenancy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Danelix.Modules.Tenancy.Infrastructure.Persistence;

internal sealed class BranchMembershipConfiguration : IEntityTypeConfiguration<BranchMembership>
{
    public void Configure(EntityTypeBuilder<BranchMembership> builder)
    {
        builder.ToTable("branch_memberships", table =>
        {
            table.HasCheckConstraint("ck_branch_memberships_role", "role IN ('Manager', 'Staff')");
            table.HasCheckConstraint(
                "ck_branch_memberships_status",
                "status IN ('Active', 'Suspended', 'Revoked')");
        });

        builder.HasKey(membership => membership.Id)
            .HasName("pk_branch_memberships");

        builder.Property(membership => membership.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(membership => membership.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(membership => membership.BranchId)
            .HasColumnName("branch_id")
            .IsRequired();

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

        builder.HasIndex(membership => new
        {
            membership.TenantId,
            membership.BranchId,
            membership.UserId,
        })
            .IsUnique()
            .HasDatabaseName("ux_branch_memberships_tenant_branch_user");

        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(membership => new { membership.TenantId, membership.BranchId })
            .HasPrincipalKey(branch => new { branch.TenantId, branch.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TenantMembership>()
            .WithMany()
            .HasForeignKey(membership => new { membership.TenantId, membership.UserId })
            .HasPrincipalKey(tenantMembership => new
            {
                tenantMembership.TenantId,
                tenantMembership.UserId,
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
