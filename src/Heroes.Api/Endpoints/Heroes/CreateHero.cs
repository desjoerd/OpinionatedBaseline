using System.ComponentModel.DataAnnotations;
using Heroes.Api.Application;
using Heroes.Api.Application.Models;
using Heroes.Api.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Heroes.Api.Endpoints.Heroes;

public static class CreateHero
{
    public static string EndpointName => nameof(CreateHero);

    public static void MapCreateHero(this IEndpointRouteBuilder builder)
        => builder.MapPost("", Endpoint)
            .WithName(EndpointName)
            .WithOpenApi()
            .RequireRateLimiting("fixed-slow");

    public record CreateHeroRequest
    {
        [Required] [MaxLength(50)] public required string Name { get; init; }

        [MaxLength(50)] public string? RealName { get; init; }

        [MaxLength(20)] public string? Power { get; init; }

        [MaxLength(1000)] public string? OriginStory { get; init; }
    }

    private static async Task<Results<Created<IdResponse>, ProblemHttpResult, ValidationProblem>> Endpoint(
        [FromServices] HeroesDbContext dbContext,
        [FromServices] LinkGenerator link,
        [FromBody] CreateHeroRequest request)
    {
        if (!Validation.TryValidate(request, out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        var hero = new Hero(request.Name, request.RealName, request.Power, request.OriginStory);

        if (await dbContext.Heroes.FindAsync(hero.Id) is not null)
        {
            return TypedResults.Problem("A hero with the same name already exists.", statusCode: 400);
        }

        dbContext.Heroes.Add(hero);

        await dbContext.SaveChangesAsync();

        var result = new IdResponse(hero.Id.ToString());
        var location = link.GetPathByName(GetHero.EndpointName, new { id = result.Id });
        return TypedResults.Created(location, result);
    }
}