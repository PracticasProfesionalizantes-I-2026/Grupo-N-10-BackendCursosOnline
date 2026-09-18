using Lumen.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.DataAccess.Configurations;

internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OperationalStatus).HasConversion<string>().HasMaxLength(30);
        builder.HasIndex(x => x.OperationalStatus);
        builder.HasIndex(x => x.OwnerProfessorId);
        builder.HasIndex(x => x.CreatedAtUtc);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany(x => x.CreatedCourses)
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OwnerProfessor)
            .WithMany(x => x.OwnedCourses)
            .HasForeignKey(x => x.OwnerProfessorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Revisions)
            .WithOne(x => x.Course)
            .HasForeignKey(x => x.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.WorkingRevision)
            .WithMany()
            .HasForeignKey(x => x.WorkingRevisionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PublishedRevision)
            .WithMany()
            .HasForeignKey(x => x.PublishedRevisionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

