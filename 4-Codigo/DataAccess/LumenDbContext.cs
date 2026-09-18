using Lumen.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lumen.DataAccess;

public sealed class LumenDbContext(DbContextOptions<LumenDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseRevision> CourseRevisions => Set<CourseRevision>();
    public DbSet<CourseModule> CourseModules => Set<CourseModule>();
    public DbSet<CourseReview> CourseReviews => Set<CourseReview>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<EnrollmentModuleCompletion> EnrollmentModuleCompletions => Set<EnrollmentModuleCompletion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LumenDbContext).Assembly);
    }
}

