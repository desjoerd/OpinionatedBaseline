using System.ComponentModel.DataAnnotations;
using Heroes.Api.Helpers;
using Heroes.Api.Tests.TestHelpers;

namespace Heroes.Api.Tests.Helpers;

public class ValidationTest : UnitTestBase
{
    private class TestRequired
    {
        [Required]
        public string? Name { get; set; }
    }

    public class TryValidate : ValidationTest
    {
        [Fact]
        public void ShouldReturnTrueWhenModelIsValid()
        {
            // Arrange
            var model = new TestRequired { Name = "Test" };

            // Act
            var result = Validation.TryValidate(model, out var errors);

            // Assert
            Assert.True(result);
            Assert.Null(errors);
        }

        [Fact]
        public void ShouldReturnFalseWhenModelIsInvalid()
        {
            // Arrange
            var model = new TestRequired();

            // Act
            var result = Validation.TryValidate(model, out var errors);

            // Assert
            Assert.False(result);
            Assert.NotNull(errors);
            Assert.Single(errors);
        }
    }
}