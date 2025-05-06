namespace Heroes.Api.Endpoints.Heroes;

public record HeroResponse(
    string Id,
    string Name,
    string? RealName,
    string? Power,
    string? OriginStory);