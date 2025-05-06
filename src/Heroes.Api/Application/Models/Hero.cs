using Slugify;

namespace Heroes.Api.Application.Models;

public class Hero
{
    // Serialization constructor
    [Obsolete("For serialization purposes only", error: true)]
    protected Hero()
    {
    }

    public Hero(string name, string? realName = null, string? power = null, string? originStory = null)
    {
        Id = new SlugHelper().GenerateSlug(name);
        PartitionKey = Id;
        Name = name;
        RealName = realName;
        Power = power;
        OriginStory = originStory;
    }

    public string Id { get; private set; } = null!;

    public string PartitionKey { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string? RealName { get; private set; }

    public string? Power { get; private set; }

    public string? OriginStory { get; private set; }

    public string? Etag { get; private set; }

    public void Update(string name, string? realName, string? power, string? originStory)
    {
        Name = name;
        RealName = realName;
        Power = power;
        OriginStory = originStory;
    }
}