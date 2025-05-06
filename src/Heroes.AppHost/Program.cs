var builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder.AddAzureCosmosDB("cosmosdb")
  .RunAsEmulator(emulator =>
  {
    // Keep the data
    emulator.WithDataVolume();

    // This emulator is slow, keep it alive
    emulator.WithLifetime(ContainerLifetime.Persistent);
  });

builder.AddProject<Projects.Heroes_Api>("api")
  .WithExternalHttpEndpoints()
  .WithReference(cosmos, connectionName: "HeroesDbContext")
  // Wait for cosmos to be started
  .WaitFor(cosmos);

builder.Build().Run();
