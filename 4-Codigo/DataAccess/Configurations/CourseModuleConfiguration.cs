using Lumen.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.DataAccess.Configurations;

internal sealed class CourseModuleConfiguration : IEntityTypeConfiguration<CourseModule>
{
    public void Configure(EntityTypeBuilder<CourseModule> builder)
    {
        builder.ToTable("CourseModules");
        builder.HasKey(x => new { x.RevisionId, x.Id });
        builder.HasIndex(x => new { x.RevisionId, x.Order }).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Content).HasMaxLength(8000).IsRequired();
        builder.Property(x => x.Resources)
            .HasConversion(JsonListConversion.Converter)
            .Metadata.SetValueComparer(JsonListConversion.Comparer);
        builder.HasOne(x => x.Revision)
            .WithMany(x => x.Modules)
            .HasForeignKey(x => x.RevisionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

