using Lumen.BusinessLogic.Interfaces;
using Lumen.BusinessLogic.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Lumen.BusinessLogic;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}
