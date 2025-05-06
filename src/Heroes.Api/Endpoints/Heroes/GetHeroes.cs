using System.ComponentModel.DataAnnotations;
using Heroes.Api.Application;
using Heroes.Api.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Heroes.Api.Endpoints.Heroes;

public static class GetHeroes
{
    public static string EndpointName => nameof(GetHeroes);

    public static void MapGetHeroes(this IEndpointRouteBuilder builder)
        => builder.MapGet("", Endpoint)
            .WithName(EndpointName);

    public record GetHeroesRequest
    {
        [FromQuery] [Range(0, int.MaxValue)] public int? Skip { get; init; } = 0;

        [FromQuery] [Range(1, 100)] public int? Take { get; init; } = 10;

        public GetHeroesRequest Next() => this with { Skip = Skip + Take };
    }

    public record HeroesResponse(IEnumerable<HeroResponse> Items, int Total, Links Links);

    public record Links(string? Next);

    public static async Task<Results<Ok<HeroesResponse>, ValidationProblem>> Endpoint(
        [FromServices] HeroesDbContext dbContext,
        [FromServices] LinkGenerator link,
        [AsParameters] GetHeroesRequest request)
    {
        if (!Validation.TryValidate(request, out var errors))
        {
            return TypedResults.ValidationProblem(errors);
        }

        var count = await dbContext.Heroes.CountAsync();
        var query = from hero in dbContext.Heroes.AsNoTracking()
            orderby hero.Id
            select new HeroResponse(
                hero.Id.ToString(),
                hero.Name,
                hero.RealName,
                hero.Power,
                hero.OriginStory);

        var heroes = await query.Skip(request.Skip ?? 0).Take(request.Take ?? 10).ToListAsync();

        var next = request.Next();
        var nextLink = count > next.Skip
            ? link.GetPathByName(EndpointName, next)
            : null;

        return TypedResults.Ok(
            new HeroesResponse(
                heroes,
                count,
                new Links(nextLink)));
    }
}