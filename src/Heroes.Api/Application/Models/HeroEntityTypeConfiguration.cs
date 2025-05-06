using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heroes.Api.Application.Models;

public class HeroEntityTypeConfiguration : IEntityTypeConfiguration<Hero>
{
    public void Configure(EntityTypeBuilder<Hero> builder)
    {
        builder
            .ToContainer("heroes")
            .HasNoDiscriminator();

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever()
            .HasMaxLength(50);

        builder.Property(e => e.PartitionKey)
            .ToJsonProperty("__partitionKey");

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.RealName)
            .HasMaxLength(50);

        builder.Property(e => e.Power)
            .HasMaxLength(20);

        builder.Property(e => e.OriginStory)
            .HasMaxLength(1000);

        builder.Property(e => e.Etag)
            .IsETagConcurrency();
    }
}