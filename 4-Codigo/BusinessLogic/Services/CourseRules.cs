using Lumen.BusinessLogic.Abstractions;
using Lumen.DataAccess.Entities;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;

namespace Lumen.BusinessLogic.Services;

internal static class CourseRules
{
    internal static void EnsureCanManage(Course course, ICurrentUser currentUser)
    {
        if (currentUser.Role == UserRole.Profesor && course.OwnerProfessorId != currentUser.UserId)
        {
            throw new CourseOwnershipException();
        }
    }

    internal static DateTime Touch(Course course)
    {
        if (course.RowVersion == long.MaxValue)
            throw new CourseStateConflictException("La versión del curso no permite registrar más cambios.");
        course.RowVersion++;
        course.UpdatedAtUtc = DateTime.UtcNow;
        return course.UpdatedAtUtc;
    }
}
