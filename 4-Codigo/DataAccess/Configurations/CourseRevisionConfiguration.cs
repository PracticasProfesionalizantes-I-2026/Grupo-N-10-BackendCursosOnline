using Lumen.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.DataAccess.Configurations;

internal sealed class CourseRevisionConfiguration : IEntityTypeConfiguration<CourseRevision>
{
    public void Configure(EntityTypeBuilder<CourseRevision> builder)
    {
        builder.ToTable("CourseRevisions");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CourseId, x.Version }).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Level).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Modality).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LearningObjectives)
            .HasConversion(JsonListConversion.Converter)
            .Metadata.SetValueComparer(JsonListConversion.Comparer);
        builder.Property(x => x.SuggestedPrerequisites)
            .HasConversion(JsonListConversion.Converter)
            .Metadata.SetValueComparer(JsonListConversion.Comparer);
    }
}

