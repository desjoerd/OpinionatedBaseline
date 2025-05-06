using Heroes.Api.Application;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Heroes.Api.Endpoints.Heroes;

public static class GetHero
{
    public static string EndpointName => nameof(GetHero);

    public static void MapGetHero(this IEndpointRouteBuilder builder)
        => builder.MapGet("{id}", Endpoint)
            .WithName(EndpointName);

    private static async Task<Results<Ok<HeroResponse>, NotFound>> Endpoint(
        [FromServices] HeroesDbContext dbContext,
        [FromRoute] string id)
    {
        return await dbContext.Heroes.FindAsync(id) switch
        {
            null => TypedResults.NotFound(),
            var hero => TypedResults.Ok(
                new HeroResponse(
                    hero.Id,
                    hero.Name,
                    hero.RealName,
                    hero.Power,
                    hero.OriginStory))
        };
    }
}