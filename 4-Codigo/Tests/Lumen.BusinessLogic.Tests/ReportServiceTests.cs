using Lumen.BusinessLogic.Services;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Moq;

namespace Lumen.BusinessLogic.Tests;

public sealed class ReportServiceTests
{
    [Fact]
    public async Task GetSummaryAsync_RejectsInvertedRange()
    {
        var service = new ReportService(
            Mock.Of<IReportRepository>(),
            new TestCurrentUser { Role = UserRole.Administrador });

        await Assert.ThrowsAsync<InvalidDateRangeException>(() =>
            service.GetSummaryAsync(new DateTime(2026, 2, 1), new DateTime(2026, 1, 1)));
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsRepositoryCounts()
    {
        var repository = new Mock<IReportRepository>();
        repository.Setup(x => x.GetCountsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportCounts(4, 2, 3));
        var service = new ReportService(repository.Object, new TestCurrentUser { Role = UserRole.Administrador });

        var result = await service.GetSummaryAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

        Assert.Equal(4, result.RegisteredUsers);
        Assert.Equal(2, result.CreatedCourses);
        Assert.Equal(3, result.RequestedEnrollments);
    }
}
