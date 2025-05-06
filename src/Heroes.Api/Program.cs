using System.Diagnostics;
using System.Reflection;
using System.Threading.RateLimiting;

using Heroes.Api.Application;
using Heroes.Api.Endpoints.Heroes;
using Heroes.Api.Helpers;

using Microsoft.AspNetCore.RateLimiting;

using OpenTelemetry.Resources;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHostedService<Migrator>();

builder.Services.AddOpenTelemetry()
    .WithTracing(x => x.AddSource("Heroes"))
    .ConfigureResource(x => x
        .AddService(
            "Heroes.Api",
            serviceVersion: typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion));

builder.Services.AddSingleton<ActivitySource>(new ActivitySource("Heroes"));

// Add services to the container.
builder.AddCosmosDbContext<HeroesDbContext>("HeroesDbContext", "cosmos-heroes");

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRateLimiter(x => x
    .AddTokenBucketLimiter("fixed-slow", options =>
    {
        options.TokenLimit = 15;
        options.TokensPerPeriod = 1;
        options.ReplenishmentPeriod = TimeSpan.FromSeconds(10);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 1;
    }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRateLimiter();

app.MapDefaultEndpoints();

app.MapGet("", () => typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unknown")
    .ExcludeFromDescription();

var apiRoot = app.MapGroup("");
apiRoot.AddEndpointFilter(async (context, next) =>
{
    var endpoint = context.HttpContext.GetEndpoint();
    var acitivitySource = context.HttpContext.RequestServices.GetService<ActivitySource>();
    using var activity = acitivitySource?.StartActivity(endpoint?.DisplayName ?? "Unknown");

    var result = await next(context);
    result = result is INestedHttpResult nestedResult ? nestedResult.Result : result;

    if (result is IStatusCodeHttpResult statusCodeResult)
    {
        var status = statusCodeResult.StatusCode >= 400 ? ActivityStatusCode.Error : ActivityStatusCode.Ok;
        activity?.SetStatus(status, statusCodeResult.StatusCode.ToString());
    }

    return result;
});
apiRoot.MapHeroesEndpoints();

app.Run();
