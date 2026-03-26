using VIGOR.Web.Services;
using VIGOR.Shared.Models;
using Xunit;

namespace VIGOR.UnitTests.Navigation
{
    public class StartPageResolverTests
    {
        private readonly StartPageResolver _resolver;

        public StartPageResolverTests()
        {
            _resolver = new StartPageResolver();
        }

        [Theory]
        [InlineData("Leder", "/admin")]
        [InlineData("Vagtansvarlig", "/schedule")]
        [InlineData("Personale", "/home")]
        public void ResolveStartRoute_ShouldReturnCorrectRoute_ForKnownRoles(string roleName, string expectedRoute)
        {
            // Arrange
            var role = new Role { Name = roleName };

            // Act
            var result = _resolver.ResolveStartRoute(role);

            // Assert
            Assert.Equal(expectedRoute, result);
        }

        [Fact]
        public void ResolveStartRoute_ShouldReturnHome_ForUnknownRole()
        {
            // Arrange
            var role = new Role { Name = "Ukendt" };

            // Act
            var result = _resolver.ResolveStartRoute(role);

            // Assert
            Assert.Equal("/home", result);
        }

        [Fact]
        public void ResolveStartRoute_ShouldReturnHome_ForNullRoleName()
        {
            // Arrange
            var role = new Role { Name = null! };

            // Act
            var result = _resolver.ResolveStartRoute(role);

            // Assert
            Assert.Equal("/home", result);
        }
    }
}
