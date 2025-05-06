using Heroes.Api.Endpoints;
using Heroes.Api.Endpoints.Heroes;
using Heroes.Api.Tests.TestHelpers;

namespace Heroes.Api.Tests.Endpoints.Heroes;

public class CreateHeroTest : IntegrationTestBase
{
    public CreateHeroTest(IntegrationTestFixture testFixture) : base(testFixture)
    {
    }

    [Fact]
    public async Task CreateHero_WithValidData_ReturnsCreatedHero()
    {
        // Arrange
        var client = CreateClient();
        var hero = new CreateHero.CreateHeroRequest
        {
            Name = "Superman",
            Power = "Flying",
        };

        // Act
        var response = await client.PostAsJsonAsync("/heroes", hero);

        // Assert
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadFromJsonAsync<IdResponse>();
        Assert.NotNull(responseContent);
        Assert.NotEmpty(responseContent.Id);
    }
}