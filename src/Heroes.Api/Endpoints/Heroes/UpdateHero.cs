using System.ComponentModel.DataAnnotations;
using Heroes.Api.Application;
using Heroes.Api.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Heroes.Api.Endpoints.Heroes;

public static class UpdateHero
{
    public static string EndpointName => nameof(UpdateHero);

    public static void MapUpdateHero(this IEndpointRouteBuilder builder)
        => builder.MapPut("{id}", Endpoint)
            .WithName(EndpointName)
            .RequireRateLimiting("fixed-slow");

    public record UpdateHeroRequest
    {
        [MaxLength(50)] public string? RealName { get; init; }

        [MaxLength(20)] public string? Power { get; init; }

        [MaxLength(1000)] public string? OriginStory { get; init; }
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound>> Endpoint(
        [FromServices] HeroesDbContext dbContext,
        [FromRoute] string id,
        [FromBody] UpdateHeroRequest request)
    {
        if(!Validation.TryValidate(request, out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        var hero = await dbContext.Heroes.FindAsync(id);
        if (hero is null)
        {
            return TypedResults.NotFound();
        }

        var name = hero.Name; // we don't want to update the name
        hero.Update(name, request.RealName, request.Power, request.OriginStory);

        await dbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }

}