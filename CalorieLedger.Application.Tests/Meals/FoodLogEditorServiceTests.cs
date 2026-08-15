using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Tests.Meals;

public sealed class FoodLogEditorServiceTests {
    [Fact]
    public void Save_ValidFood_CreatesMealAndFoodEntry() {
        var currentDate = new DateOnly(2026, 8, 15);

        var store = new InMemoryFoodDiaryStore();

        store.SetDateComplete(
            currentDate,
            true
        );

        var service = new FoodLogEditorService(store);

        var draft = service.CreateNew(currentDate) with {
            Name = "Творог",
            QuantityValue = 250m,
            QuantityUnit = FoodUnit.Gram,
            NutritionBasis = NutritionBasis.Per100Grams,
            CaloriesKcal = 120m,
            ProteinG = 17m,
            FatG = 5m,
            CarbsG = 3m,
        };

        var result = service.Save(
            draft,
            currentDate
        );

        Assert.True(result.IsSuccess);

        var meal = Assert.Single(
            store.GetMeals(
                currentDate,
                currentDate
            )
        );

        Assert.Equal(
            MealGroupRole.Snack,
            meal.Role
        );

        var food = Assert.Single(
            store.GetFoodEntries(
                [meal.Id]
            )
        );

        Assert.Equal(
            "Творог",
            food.Name
        );

        Assert.Empty(
            store.GetCompletedDates(
                currentDate,
                currentDate
            )
        );
    }

    [Fact]
    public void Save_IncompatibleBasisAndUnit_ReturnsValidationError() {
        var currentDate = new DateOnly(2026, 8, 15);

        var service = new FoodLogEditorService(
            new InMemoryFoodDiaryStore()
        );

        var draft = service.CreateNew(currentDate) with {
            Name = "Продукт",
            QuantityValue = 2m,
            QuantityUnit = FoodUnit.Piece,
            NutritionBasis = NutritionBasis.Per100Grams,
            CaloriesKcal = 100m,
        };

        var result = service.Save(draft, currentDate);

        Assert.False(result.IsSuccess);

        Assert.Contains(
            FoodLogValidationError.IncompatibleNutritionBasis,
            result.Errors
        );
    }

    [Fact]
    public void CalculatePreview_Per100Grams_ReturnsScaledNutrition() {
        var currentDate = new DateOnly(2026, 8, 15);

        var service = new FoodLogEditorService(
            new InMemoryFoodDiaryStore()
        );

        var draft = service.CreateNew(currentDate) with {
            Name = "Творог",
            QuantityValue = 250m,
            CaloriesKcal = 120m,
            ProteinG = 17m,
            FatG = 5m,
            CarbsG = 3m,
        };

        var result = Assert.IsType<NutritionTotals>(
            service.CalculatePreview(draft)
        );

        Assert.Equal(
            300m,
            result.CaloriesKcal
        );

        Assert.Equal(
            42.5m,
            result.ProteinG
        );
    }
}
