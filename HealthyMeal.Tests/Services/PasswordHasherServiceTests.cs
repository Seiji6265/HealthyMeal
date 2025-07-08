using Xunit;
using HealthyMeal.Services;

namespace HealthyMeal.Tests.Services
{
    public class PasswordHasherServiceTests
    {
        private readonly PasswordHasherService _passwordHasher;

        public PasswordHasherServiceTests()
        {
            _passwordHasher = new PasswordHasherService();
        }

        [Fact]
        public void HashPassword_ShouldReturnValidHash()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash = _passwordHasher.HashPassword(password);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
            Assert.True(hash.Length > 50); // BCrypt hash should be longer than 50 chars
        }

        [Fact]
        public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
        {
            // Arrange
            var password = "TestPassword123!";
            var hash = _passwordHasher.HashPassword(password);

            // Act
            var result = _passwordHasher.VerifyPassword(password, hash);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
        {
            // Arrange
            var password = "TestPassword123!";
            var wrongPassword = "WrongPassword123!";
            var hash = _passwordHasher.HashPassword(password);

            // Act
            var result = _passwordHasher.VerifyPassword(wrongPassword, hash);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void HashPassword_SamePlaintext_ShouldProduceDifferentHashes()
        {
            // Arrange
            var password = "TestPassword123!";

            // Act
            var hash1 = _passwordHasher.HashPassword(password);
            var hash2 = _passwordHasher.HashPassword(password);

            // Assert
            Assert.NotEqual(hash1, hash2); // BCrypt should produce different hashes due to salt
        }

        [Fact]
        public void HashPassword_WithEmptyPassword_ShouldReturnHash()
        {
            // Arrange
            var emptyPassword = string.Empty;

            // Act
            var hash = _passwordHasher.HashPassword(emptyPassword);

            // Assert
            Assert.NotNull(hash);
            Assert.NotEmpty(hash);
        }

        [Fact]
        public void VerifyPassword_WithEmptyPassword_ShouldReturnFalse()
        {
            // Arrange
            var hash = _passwordHasher.HashPassword("ValidPassword123!");

            // Act
            var result = _passwordHasher.VerifyPassword(string.Empty, hash);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void VerifyPassword_WithNullHash_ShouldReturnFalse()
        {
            // Arrange
            var password = "ValidPassword123!";

            // Act
            var result = _passwordHasher.VerifyPassword(password, null!);

            // Assert
            Assert.False(result);
        }
    }
}