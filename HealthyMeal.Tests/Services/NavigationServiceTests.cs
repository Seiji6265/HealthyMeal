using Xunit;
using Moq;
using HealthyMeal.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace HealthyMeal.Tests.Services
{
    public class NavigationServiceTests
    {
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly NavigationService _navigationService;

        public NavigationServiceTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
            _navigationService = new NavigationService(_mockServiceProvider.Object);
        }

        [Fact]
        public void Constructor_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => new NavigationService(_mockServiceProvider.Object));
            Assert.Null(exception);
        }

        [Fact(Skip = "Requires WPF UI components - not supported in GitHub Actions")]
        public void NavigateTo_WithInvalidViewName_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidViewName = "NonExistentView";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _navigationService.NavigateTo(invalidViewName));
        }

        [Fact]
        public void NavigationService_HasViewTypes_ShouldInitializeCorrectly()
        {
            // This test only verifies the constructor works and the service initializes
            // Real navigation testing would require UI components
            Assert.NotNull(_navigationService);
        }
    }
}
