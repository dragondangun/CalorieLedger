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

    [Fact]
    public void Load_ExistingFood_ReturnsEditableDraft() {
        var date = new DateOnly(2026, 8, 15);

        var store = new InMemoryFoodDiaryStore();

        var meal = new MealEntry(
            Id: Guid.NewGuid(),
            Date: date,
            Name: "Обед",
            Role: MealGroupRole.Lunch
        );

        var food = new FoodLogEntry(
            Id: Guid.NewGuid(),
            MealEntryId: meal.Id,
            Name: "Гречка",
            Quantity: FoodQuantity.Grams(200m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 110m,
                ProteinG: 4m,
                FatG: 1m,
                CarbsG: 21m
            ),
            Source: FoodLogSource.CatalogProduct,
            SourceId: Guid.NewGuid(),
            Note: "До тренировки"
        );

        store.SaveMeal(meal);

        store.SaveFoodEntry(food);

        var service = new FoodLogEditorService(store);

        var draft = Assert.IsType<FoodLogDraft>(service.Load(food.Id));

        Assert.Equal(
            food.Id,
            draft.Id
        );

        Assert.Equal(
            date,
            draft.Date
        );

        Assert.Equal(
            MealGroupRole.Lunch,
            draft.MealRole
        );

        Assert.Equal(
            FoodLogSource.CatalogProduct,
            draft.Source
        );

        Assert.Equal(
            food.SourceId,
            draft.SourceId
        );
    }

    [Fact]
    public void Save_ExistingFood_UpdatesEntryAndMovesBetweenMeals() {
        var date = new DateOnly(2026, 8, 15);

        var store = new InMemoryFoodDiaryStore();

        var oldMeal = new MealEntry(
            Id: Guid.NewGuid(),
            Date: date,
            Name: "Обед",
            Role: MealGroupRole.Lunch
        );

        var food = new FoodLogEntry(
            Id: Guid.NewGuid(),
            MealEntryId: oldMeal.Id,
            Name: "Творог",
            Quantity: FoodQuantity.Grams(200m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 120m,
                ProteinG: 17m,
                FatG: 5m,
                CarbsG: 3m
            ),
            Source: FoodLogSource.Manual
        );

        store.SaveMeal(oldMeal);

        store.SaveFoodEntry(food);

        store.SetDateComplete(date, true);

        var service = new FoodLogEditorService(store);

        var draft = Assert.IsType<FoodLogDraft>(service.Load(food.Id)) with {
            Name = "Творог 5%",
            MealRole = MealGroupRole.Snack,
            QuantityValue = 250m,
        };

        var result = service.Save(draft, date);

        Assert.True(result.IsSuccess);

        var savedFood = Assert.IsType<FoodLogEntry>(
            store.GetFoodEntry(food.Id)
        );

        Assert.Equal(
            "Творог 5%",
            savedFood.Name
        );

        Assert.Equal(
            250m,
            savedFood.Quantity.Value
        );

        var newMeal = Assert.IsType<MealEntry>(store.GetMeal(savedFood.MealEntryId));

        Assert.Equal(MealGroupRole.Snack, newMeal.Role
        );

        Assert.Null(store.GetMeal(oldMeal.Id));

        Assert.Empty(store.GetCompletedDates(date, date));
    }

    [Fact]
    public void Delete_LastFoodInMeal_RemovesMealAndReopensDay() {
        var date = new DateOnly(2026, 8, 15);

        var store = new InMemoryFoodDiaryStore();

        var meal = new MealEntry(
            Id: Guid.NewGuid(),
            Date: date,
            Name: "Перекусы",
            Role: MealGroupRole.Snack
        );

        var food = new FoodLogEntry(
            Id: Guid.NewGuid(),
            MealEntryId: meal.Id,
            Name: "Яблоко",
            Quantity: FoodQuantity.Grams(150m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 52m,
                ProteinG: 0.3m,
                FatG: 0.2m,
                CarbsG: 14m
            ),
            Source: FoodLogSource.Manual
        );

        store.SaveMeal(meal);

        store.SaveFoodEntry(food);

        store.SetDateComplete(date, true);

        var service = new FoodLogEditorService(store);

        var deleted = service.Delete(food.Id);
        Assert.True(deleted);

        Assert.Null(store.GetFoodEntry(food.Id));

        Assert.Null(store.GetMeal(meal.Id));

        Assert.Empty(store.GetCompletedDates(date, date));
    }

    [Fact]
    public void CreateNewApproximation_ReturnsTotalApproximateDraft() {
        var date = new DateOnly(2026, 8, 16);

        var service = new FoodLogEditorService(new InMemoryFoodDiaryStore());

        var draft = service.CreateNewApproximation(date);

        Assert.Equal(
            date,
            draft.Date
        );

        Assert.Equal(
            MealGroupRole.Custom,
            draft.MealRole
        );

        Assert.Equal(
            1m,
            draft.QuantityValue
        );

        Assert.Equal(
            FoodUnit.Portion,
            draft.QuantityUnit
        );

        Assert.Equal(
            NutritionBasis.Total,
            draft.NutritionBasis
        );

        Assert.Equal(
            FoodLogSource.Approximation,
            draft.Source
        );

        Assert.True(draft.IsApproximate);

        Assert.Null(draft.CaloriesKcal);
    }
}
