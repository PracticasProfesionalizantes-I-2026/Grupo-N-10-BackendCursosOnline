using Lumen.DataAccess.Entities;
using Lumen.Shared.Validation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.DataAccess.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(UserFieldLimits.Email).IsRequired();
        builder.Property(x => x.NormalizedEmail).HasMaxLength(UserFieldLimits.Email).IsRequired();
        builder.HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.FirstName).HasMaxLength(UserFieldLimits.Name).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(UserFieldLimits.Name).IsRequired();
        builder.Property(x => x.Dni).HasMaxLength(UserFieldLimits.Dni).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(UserFieldLimits.Phone).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(UserFieldLimits.Address).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(UserFieldLimits.PostalCode).IsRequired();
        builder.HasIndex(x => x.Role);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}

