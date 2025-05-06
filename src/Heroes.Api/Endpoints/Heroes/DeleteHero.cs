using Heroes.Api.Application;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Heroes.Api.Endpoints.Heroes;

public static class DeleteHero
{
    public static string EndpointName => nameof(DeleteHero);

    public static void MapDeleteHero(this IEndpointRouteBuilder builder)
        => builder.MapDelete("{id}", Endpoint)
            .WithName(EndpointName);

    private static async Task<Results<NoContent, NotFound>> Endpoint(
        [FromServices] HeroesDbContext dbContext,
        [FromRoute] string id)
    {
        var hero = await dbContext.Heroes.FindAsync(id);
        if (hero is null)
        {
            return TypedResults.NotFound();
        }

        dbContext.Heroes.Remove(hero);
        await dbContext.SaveChangesAsync();
        return TypedResults.NoContent();
    }
}