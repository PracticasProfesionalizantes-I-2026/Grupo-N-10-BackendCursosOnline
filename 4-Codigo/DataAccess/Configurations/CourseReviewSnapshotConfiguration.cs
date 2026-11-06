using Lumen.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.DataAccess.Configurations;

internal sealed class CourseReviewSnapshotConfiguration : IEntityTypeConfiguration<CourseReviewSnapshot>
{
    public void Configure(EntityTypeBuilder<CourseReviewSnapshot> builder)
    {
        builder.ToTable("CourseReviewSnapshots");
        builder.HasKey(x => x.ReviewId);
        builder.HasOne<CourseReview>().WithOne(x => x.Snapshot)
            .HasForeignKey<CourseReviewSnapshot>(x => x.ReviewId).OnDelete(DeleteBehavior.Restrict);
    }
}
