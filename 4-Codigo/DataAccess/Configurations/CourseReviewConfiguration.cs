using Lumen.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.DataAccess.Configurations;

internal sealed class CourseReviewConfiguration : IEntityTypeConfiguration<CourseReview>
{
    public void Configure(EntityTypeBuilder<CourseReview> builder)
    {
        builder.ToTable("CourseReviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Decision).HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.Observation).HasMaxLength(2000);
        builder.HasIndex(x => x.Decision);
        builder.HasIndex(x => x.RequestedAtUtc);
        builder.HasOne(x => x.CourseRevision)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.CourseRevisionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RequestedByUser)
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReviewedByUser)
            .WithMany()
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

