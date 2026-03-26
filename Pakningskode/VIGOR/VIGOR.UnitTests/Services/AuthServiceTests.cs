using Moq;
using VIGOR.Shared.DTOs;
using VIGOR.Shared.Enums;
using VIGOR.Shared.Interfaces.Services;
using VIGOR.Shared.Models;
using VIGOR.Web.Services;
using Xunit;

namespace VIGOR.UnitTests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IIdentityGateway> _identityMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _identityMock = new Mock<IIdentityGateway>();
            _authService = new AuthService(_identityMock.Object);
        }

        [Fact]
        public async Task SignInAsync_ShouldReturnSuccess_WhenCredentialsAreValid()
        {
            // Arrange
            var email = "test@vigor.dk";
            var password = "Password123!";
            var expectedRole = new Role { Name = "Leder", Level = 3 };
            
            var authResult = new AuthResult
            {
                Status = AuthStatus.Success,
                Role = expectedRole
            };

            _identityMock.Setup(x => x.AuthenticateAsync(email, password))
                .ReturnsAsync(authResult);

            // Act
            var result = await _authService.SignInAsync(email, password);

            // Assert
            Assert.Equal(AuthStatus.Success, result.Status);
            Assert.Equal(expectedRole.Name, result.Role?.Name);
            _identityMock.Verify(x => x.AuthenticateAsync(email, password), Times.Once);
        }

        [Fact]
        public async Task SignInAsync_ShouldReturnRejected_WhenCredentialsAreInvalid()
        {
            // Arrange
            var email = "invalid@vigor.dk";
            var password = "WrongPassword!";
            var expectedMessage = "Forkert email eller adgangskode";
            
            var authResult = new AuthResult
            {
                Status = AuthStatus.Rejected,
                Message = expectedMessage,
                Role = null
            };

            _identityMock.Setup(x => x.AuthenticateAsync(email, password))
                .ReturnsAsync(authResult);

            // Act
            var result = await _authService.SignInAsync(email, password);

            // Assert
            Assert.Equal(AuthStatus.Rejected, result.Status);
            Assert.Equal(expectedMessage, result.Message);
            Assert.Null(result.Role);
            _identityMock.Verify(x => x.AuthenticateAsync(email, password), Times.Once);
        }

        [Fact]
        public async Task SignInAsync_ShouldReturnDenied_WhenUserHasNoRole()
        {
            // Arrange
            var email = "norole@vigor.dk";
            var password = "CorrectPassword123!";
            var expectedMessage = "Du har ikke adgang. Kontakt administrator.";

            var authResult = new AuthResult
            {
                Status = AuthStatus.Denied,
                Message = expectedMessage,
                Role = null
            };

            _identityMock.Setup(x => x.AuthenticateAsync(email, password))
                .ReturnsAsync(authResult);

            // Act
            var result = await _authService.SignInAsync(email, password);

            // Assert
            Assert.Equal(AuthStatus.Denied, result.Status);
            Assert.Equal(expectedMessage, result.Message);
            Assert.Null(result.Role);
            _identityMock.Verify(x => x.AuthenticateAsync(email, password), Times.Once);
        }

        [Fact]
        public async Task SignInAsync_ShouldReturnRejected_WhenUserIsLockedOut()
        {
            // Arrange
            var email = "locked@vigor.dk";
            var password = "CorrectPassword123!";
            var expectedMessage = "For mange mislykkede forsøg. Prøv igen om 5 minutter.";

            var authResult = new AuthResult
            {
                Status = AuthStatus.Rejected,
                Message = expectedMessage
            };

            _identityMock.Setup(x => x.AuthenticateAsync(email, password))
                .ReturnsAsync(authResult);

            // Act
            var result = await _authService.SignInAsync(email, password);

            // Assert
            Assert.Equal(AuthStatus.Rejected, result.Status);
            Assert.Equal(expectedMessage, result.Message);
            _identityMock.Verify(x => x.AuthenticateAsync(email, password), Times.Once);
        }
    }
}
