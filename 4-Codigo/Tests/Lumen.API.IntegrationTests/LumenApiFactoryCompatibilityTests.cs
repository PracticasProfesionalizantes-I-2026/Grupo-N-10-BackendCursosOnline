using System.Data.Common;
using System.Net;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Lumen.API.IntegrationTests;

public sealed class LumenApiFactoryCompatibilityTests
{
    [Fact]
    public async Task Constructor_WithoutArgumentsAndNullConfiguration_StartsIsolatedHost()
    {
        using var factory = new LumenApiFactory { ConfigureTestServices = null };
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json")).StatusCode);
    }

    [Fact]
    public async Task Constructor_IndividualDbCommandInterceptor_CompilesCu06CallAndRegistersInterceptor()
    {
        var gate = new CountingCommandInterceptor();
        // Same call and interceptor base type used by CU-06 PR #5.
        using var factory = new LumenApiFactory(gate);
        using var client = await CourseTestClient.LoginAsync(factory);
        Assert.True(gate.ReaderCalls > 0);
    }

    [Fact]
    public async Task Constructor_InterceptorArray_RegistersEveryInterceptor()
    {
        var first = new CountingCommandInterceptor();
        var second = new CountingCommandInterceptor();
        IInterceptor[] interceptors = [first, second];
        using var factory = new LumenApiFactory(interceptors);
        using var client = await CourseTestClient.LoginAsync(factory);
        Assert.True(first.ReaderCalls > 0);
        Assert.Equal(first.ReaderCalls, second.ReaderCalls);
    }

    [Fact]
    public async Task ConfigurationInitializer_CanBeCombinedWithIndividualInterceptors()
    {
        var interceptor = new CountingCommandInterceptor();
        var configured = false;
        using var factory = new LumenApiFactory(interceptor)
        {
            ConfigureTestServices = _ => configured = true
        };
        using var client = await CourseTestClient.LoginAsync(factory);
        Assert.True(configured);
        Assert.True(interceptor.ReaderCalls > 0);
    }

    [Fact]
    public void Constructor_NullArray_IsUnambiguousAndRejectedExplicitly() =>
        Assert.Throws<ArgumentNullException>(() => new LumenApiFactory(null!));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Constructor_NullInterceptor_IsUnambiguousAndRejectedExplicitly(bool multiple)
    {
        Assert.Throws<ArgumentException>(() => multiple
            ? new LumenApiFactory(new CountingCommandInterceptor(), null!)
            : new LumenApiFactory((IInterceptor)null!));
    }

    private sealed class CountingCommandInterceptor : DbCommandInterceptor
    {
        private int readerCalls;
        public int ReaderCalls => Volatile.Read(ref readerCalls);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref readerCalls);
            return ValueTask.FromResult(result);
        }
    }
}

