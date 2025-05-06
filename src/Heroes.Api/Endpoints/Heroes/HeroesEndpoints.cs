namespace Heroes.Api.Endpoints.Heroes;

public static class HeroesEndpoints
{
    public static void MapHeroesEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/heroes")
            .WithTags("Heroes");

        group.MapGetHeroes();
        group.MapGetHero();
        group.MapCreateHero();
        group.MapUpdateHero();
        group.MapDeleteHero();
    }
}