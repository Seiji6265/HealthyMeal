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

        public NavigationServiceTests()
        {
            _mockServiceProvider = new Mock<IServiceProvider>();
        }

        [Fact(Skip = "Requires WPF UI components")]
        public void Constructor_ShouldNotThrow()
        {
            // Act & Assert
            var exception = Record.Exception(() => new NavigationService(_mockServiceProvider.Object));
            Assert.Null(exception);
        }

        [Fact(Skip = "Requires WPF UI components")]
        public void NavigateTo_WithInvalidViewName_ShouldThrowArgumentException()
        {
            // Arrange
            var invalidViewName = "NonExistentView";
            var navigationService = new NavigationService(_mockServiceProvider.Object);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => navigationService.NavigateTo(invalidViewName));
        }

        [Fact(Skip = "Requires WPF UI components")]
        public void NavigationService_HasViewTypes_ShouldInitializeCorrectly()
        {
            // This test only verifies the constructor works and the service initializes
            // Real navigation testing would require UI components
            var navigationService = new NavigationService(_mockServiceProvider.Object);
            Assert.NotNull(navigationService);
        }
    }
}
