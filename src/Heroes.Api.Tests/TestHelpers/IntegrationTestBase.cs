using Microsoft.Extensions.DependencyInjection;

namespace Heroes.Api.Tests.TestHelpers;

[Collection(nameof(IntegrationCollectionFixture))]
[Trait("Category", "Integration")]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly IntegrationTestFixture testFixture;
    private readonly IServiceScope defaultServiceScope;

    protected IntegrationTestBase(IntegrationTestFixture testFixture)
    {
        this.testFixture = testFixture;

        defaultServiceScope = testFixture.Services.CreateScope();
    }

    protected IServiceProvider TestServiceProvider => defaultServiceScope.ServiceProvider;

    public async Task InitializeAsync() =>
        await testFixture.ResetDatabaseAsync()
            .ConfigureAwait(false);

    public async Task DisposeAsync()
    {
        if (defaultServiceScope is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync()
                .ConfigureAwait(false);
        }
        else
        {
            defaultServiceScope.Dispose();
        }
    }

    protected HttpClient CreateClient() => testFixture.WebApplicationFactory.CreateClient();
}
