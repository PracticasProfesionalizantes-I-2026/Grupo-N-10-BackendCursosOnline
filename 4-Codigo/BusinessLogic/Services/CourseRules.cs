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

    internal static DateTime NextTimestamp(DateTime previous)
    {
        var now = DateTime.UtcNow;
        if (now > previous) return now;
        if (previous == DateTime.MaxValue)
        {
            throw new CourseStateConflictException("La fecha de actualización del curso no permite registrar cambios.");
        }
        return previous.AddTicks(1);
    }
}
