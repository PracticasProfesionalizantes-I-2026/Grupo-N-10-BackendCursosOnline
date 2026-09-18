using Lumen.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lumen.DataAccess.Configurations;

internal sealed class EnrollmentModuleCompletionConfiguration : IEntityTypeConfiguration<EnrollmentModuleCompletion>
{
    public void Configure(EntityTypeBuilder<EnrollmentModuleCompletion> builder)
    {
        builder.ToTable("EnrollmentModuleCompletions");
        builder.HasKey(x => new { x.EnrollmentId, x.ModuleId });
        builder.HasOne(x => x.Enrollment)
            .WithMany(x => x.ModuleCompletions)
            .HasForeignKey(x => x.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
