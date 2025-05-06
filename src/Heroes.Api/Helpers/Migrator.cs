using Heroes.Api.Application;

namespace Heroes.Api.Helpers;

public class Migrator(IServiceProvider serviceProvider) : IHostedService
{
  public async Task StartAsync(CancellationToken cancellationToken)
  {
    using var serviceScope = serviceProvider.CreateAsyncScope();

    var dbContext = serviceScope.ServiceProvider.GetRequiredService<HeroesDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    throw new NotImplementedException();
  }
}
