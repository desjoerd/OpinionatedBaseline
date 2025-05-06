using Heroes.Api.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Testcontainers.CosmosDb;

namespace Heroes.Api.Tests.TestHelpers;

public sealed class IntegrationTestFixture : IAsyncLifetime
{
     private readonly CosmosDbContainer cosmosDbDockerContainer = new CosmosDbBuilder()
         .WithEnvironment("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "3")
         .WithReuse(true)
         .Build();

    internal WebApplicationFactory<Program> WebApplicationFactory { get; private set; } = null!;

    public IServiceProvider Services => this.WebApplicationFactory.Services;

    public async Task ResetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        using var scope = this.Services.CreateScope();
        var scopeServies = scope.ServiceProvider;

        var dbContext = scopeServies.GetRequiredService<HeroesDbContext>();
        dbContext.Heroes.RemoveRange(await dbContext.Heroes.ToListAsync(cancellationToken: cancellationToken));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task InitializeAsync()
    {
        await this.cosmosDbDockerContainer.StartAsync();

        WebApplicationFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(ConfigureHost);

        // Wait for successful reset
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var successful = false;
        while (!successful)
        {
            try
            {
                await ResetDatabaseAsync(timeout.Token);
                successful = true;
            }
            catch (CosmosException)
            {
                await Task.Delay(1000, timeout.Token);
            }
        }
    }

    private void ConfigureHost(IWebHostBuilder host)
    {
        host.UseEnvironment("Testing");
        host.ConfigureServices(services =>
        {
            // Replace the database with a test database
            var contextOptions = new DbContextOptionsBuilder<HeroesDbContext>()
                .UseCosmos(this.cosmosDbDockerContainer.GetConnectionString(), "cosmos-test-heroes", builder =>
                {
                    builder.HttpClientFactory(() => cosmosDbDockerContainer.HttpClient);
                    builder.ConnectionMode(ConnectionMode.Gateway);
                })
                //.ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            services.AddSingleton(contextOptions);
        });
    }

    public async Task DisposeAsync()
    {
        // Disposing the container stops the container.
        // Starting the container takes a long time so in debug we don't want to stop it.
        //await this.cosmosDbDockerContainer.StopAsync();

        // awaiting the completed task to avoid warning (in debug)
        await Task.CompletedTask;
    }
}