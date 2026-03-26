using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Interfaces.Services;
using VIGOR.Shared.Models;
using VIGOR.Web.Controllers.Api;
using Xunit;

namespace VIGOR.UnitTests.Controllers.Api
{
    public class AuthApiControllerTests
    {
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<IConfiguration> _configurationMock;
        private readonly AuthApiController _controller;

        public AuthApiControllerTests()
        {
            _authServiceMock = new Mock<IAuthService>();
            _configurationMock = new Mock<IConfiguration>();

            // Mock configuration for JWT
            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(s => s["Secret"]).Returns("ThisIsAVerySecretKeyForTestingPurposesOnly!");
            jwtSection.Setup(s => s["Issuer"]).Returns("VIGOR");
            jwtSection.Setup(s => s["Audience"]).Returns("VIGOR_CLIENT");
            jwtSection.Setup(s => s["ExpiryMinutes"]).Returns("60");
            
            _configurationMock.Setup(c => c.GetSection("JwtSettings")).Returns(jwtSection.Object);

            _controller = new AuthApiController(_authServiceMock.Object, _configurationMock.Object);
        }

        [Fact]
        public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
        {
            // Arrange
            var request = new LoginRequest { Email = "leder@vigor.dk", Password = "Password123!" };
            var authResult = new AuthResult
            {
                Status = AuthStatus.Success,
                Role = new Role { Name = "Leder", Level = 3 }
            };

            _authServiceMock.Setup(s => s.SignInAsync(request.Email, request.Password))
                .ReturnsAsync(authResult);

            // Act
            var actionResult = await _controller.Login(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            var response = Assert.IsType<AuthResponse>(okResult.Value);

            Assert.NotEmpty(response.Token);
            Assert.Equal(AuthStatus.Success, response.Result.Status);
            Assert.Equal("Leder", response.Result.Role?.Name);
            
            _authServiceMock.Verify(s => s.SignInAsync(request.Email, request.Password), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenCredentialsAreRejected()
        {
            // Arrange
            var request = new LoginRequest { Email = "wrong@vigor.dk", Password = "WrongPassword" };
            var authResult = new AuthResult
            {
                Status = AuthStatus.Rejected,
                Message = "Forkert email eller adgangskode"
            };

            _authServiceMock.Setup(s => s.SignInAsync(request.Email, request.Password))
                .ReturnsAsync(authResult);

            // Act
            var actionResult = await _controller.Login(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult);
            var response = Assert.IsType<AuthResponse>(unauthorizedResult.Value);

            Assert.Empty(response.Token);
            Assert.Equal(AuthStatus.Rejected, response.Result.Status);
            Assert.Equal("Forkert email eller adgangskode", response.Result.Message);

            _authServiceMock.Verify(s => s.SignInAsync(request.Email, request.Password), Times.Once);
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenUserIsDenied()
        {
            // Arrange
            var request = new LoginRequest { Email = "norole@vigor.dk", Password = "CorrectPassword" };
            var authResult = new AuthResult
            {
                Status = AuthStatus.Denied,
                Message = "Du har ikke adgang. Kontakt administrator."
            };

            _authServiceMock.Setup(s => s.SignInAsync(request.Email, request.Password))
                .ReturnsAsync(authResult);

            // Act
            var actionResult = await _controller.Login(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult);
            var response = Assert.IsType<AuthResponse>(unauthorizedResult.Value);

            Assert.Empty(response.Token);
            Assert.Equal(AuthStatus.Denied, response.Result.Status);
            Assert.Equal("Du har ikke adgang. Kontakt administrator.", response.Result.Message);

            _authServiceMock.Verify(s => s.SignInAsync(request.Email, request.Password), Times.Once);
        }
    }
}
