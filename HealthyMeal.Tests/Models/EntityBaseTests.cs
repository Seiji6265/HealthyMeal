using Xunit;
using HealthyMeal.Models;

namespace HealthyMeal.Tests.Models
{
    public class EntityBaseTests
    {
        private class TestEntity : EntityBase
        {
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public void EntityBase_ShouldHaveIdProperty()
        {
            // Arrange
            var entity = new TestEntity();

            // Act
            entity.Id = 123;

            // Assert
            Assert.Equal(123, entity.Id);
        }

        [Fact]
        public void EntityBase_DefaultId_ShouldBeZero()
        {
            // Arrange & Act
            var entity = new TestEntity();

            // Assert
            Assert.Equal(0, entity.Id);
        }

        [Fact]
        public void EntityBase_InheritedClasses_ShouldHaveIdProperty()
        {
            // Arrange
            var recipe = new Recipe();
            var profile = new Profile();
            var mealPlan = new MealPlan();

            // Act
            recipe.Id = 1;
            profile.Id = 2;
            mealPlan.Id = 3;

            // Assert
            Assert.Equal(1, recipe.Id);
            Assert.Equal(2, profile.Id);
            Assert.Equal(3, mealPlan.Id);
        }

        [Fact]
        public void EntityBase_InheritedClasses_ShouldBeTypeOfEntityBase()
        {
            // Arrange
            var recipe = new Recipe();
            var profile = new Profile();
            var mealPlan = new MealPlan();

            // Act & Assert
            Assert.IsAssignableFrom<EntityBase>(recipe);
            Assert.IsAssignableFrom<EntityBase>(profile);
            Assert.IsAssignableFrom<EntityBase>(mealPlan);
        }
    }
}