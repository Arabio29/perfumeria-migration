using Danelix.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Danelix.Modules.Identity.Infrastructure.Persistence;

internal sealed class PlatformRoleAssignmentConfiguration : IEntityTypeConfiguration<PlatformRoleAssignment>
{
    public void Configure(EntityTypeBuilder<PlatformRoleAssignment> builder)
    {
        builder.ToTable("platform_roles", table =>
            table.HasCheckConstraint(
                "ck_platform_roles_role",
                "role IN ('PlatformAdmin', 'SupportOperator')"));

        // La pareja usuario/rol es la identidad de la asignación: un usuario no acumula filas duplicadas.
        builder.HasKey(assignment => new { assignment.UserId, assignment.Role })
            .HasName("pk_platform_roles");

        builder.Property(assignment => assignment.UserId)
            .HasColumnName("user_id")
            .ValueGeneratedNever();

        builder.Property(assignment => assignment.Role)
            .HasColumnName("role")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(assignment => assignment.GrantedAtUtc)
            .HasColumnName("granted_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(assignment => assignment.Role)
            .HasDatabaseName("ix_platform_roles_role");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(assignment => assignment.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
