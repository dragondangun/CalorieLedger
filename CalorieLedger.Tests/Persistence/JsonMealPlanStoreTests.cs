using CalorieLedger.Application.MealPlanning;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Persistence;

namespace CalorieLedger.Tests.Persistence;

public sealed class JsonMealPlanStoreTests:IDisposable {
    private readonly string directoryPath;
    private readonly string filePath;

    public JsonMealPlanStoreTests() {
        directoryPath = Path.Combine(
            Path.GetTempPath(),
            "CalorieLedger.Tests",
            Guid.NewGuid().ToString("N")
        );
        filePath = Path.Combine(directoryPath, "meal-plans.json");
    }

    [Fact]
    public void Save_Plan_PersistsAcrossStoreInstances() {
        var fridgeItemId = Guid.NewGuid();
        var day = new MealPlanDay(
            Date: new DateOnly(2026, 8, 21),
            Meals: [
                new MealPlanMeal(
                    Name: "Обед",
                    Role: MealGroupRole.Lunch,
                    Time: new TimeOnly(13, 30),
                    Items: [
                        new MealPlanItem(
                            Name: "Суп",
                            Quantity: FoodQuantity.Milliliters(350m),
                            FridgeItemId: fridgeItemId,
                            Nutrition: new NutritionTotals(
                                CaloriesKcal: 280m,
                                ProteinG: 12m,
                                FatG: 8m,
                                CarbsG: 35m
                            ),
                            Note: "Из холодильника"
                        ),
                    ],
                    Note: "Рабочий день"
                ),
            ]
        );

        new JsonMealPlanStore(filePath).Save(new MealPlan([day]));

        var saved = Assert.Single(new JsonMealPlanStore(filePath).GetAll());
        var savedMeal = Assert.Single(saved.Meals);
        var savedItem = Assert.Single(savedMeal.Items);

        Assert.Equal(day.Date, saved.Date);
        Assert.Equal("Обед", savedMeal.Name);
        Assert.Equal(MealGroupRole.Lunch, savedMeal.Role);
        Assert.Equal(new TimeOnly(13, 30), savedMeal.Time);
        Assert.Equal("Рабочий день", savedMeal.Note);
        Assert.Equal("Суп", savedItem.Name);
        Assert.Equal(FoodQuantity.Milliliters(350m), savedItem.Quantity);
        Assert.Equal(fridgeItemId, savedItem.FridgeItemId);
        Assert.Equal(280m, savedItem.Nutrition.CaloriesKcal);
        Assert.Equal("Из холодильника", savedItem.Note);
    }

    [Fact]
    public void Save_NewPlan_ReplacesOnlyMatchingDates() {
        var store = new JsonMealPlanStore(filePath);

        store.Save(
            new MealPlan([
                CreateDay(new DateOnly(2026, 8, 19), "19"),
                CreateDay(new DateOnly(2026, 8, 20), "old-20"),
                CreateDay(new DateOnly(2026, 8, 21), "old-21"),
                CreateDay(new DateOnly(2026, 8, 22), "22"),
            ])
        );

        store.Save(
            new MealPlan([
                CreateDay(new DateOnly(2026, 8, 20), "new-20"),
                CreateDay(new DateOnly(2026, 8, 22), "new-22"),
            ])
        );

        var reopened = new JsonMealPlanStore(filePath).GetAll();

        Assert.Equal(4, reopened.Count);
        Assert.Equal("19", reopened[0].Meals[0].Name);
        Assert.Equal("new-20", reopened[1].Meals[0].Name);
        Assert.Equal("old-21", reopened[2].Meals[0].Name);
        Assert.Equal("new-22", reopened[3].Meals[0].Name);
    }

    public void Dispose() {
        if(Directory.Exists(directoryPath)) {
            Directory.Delete(directoryPath, recursive: true);
        }
    }

    private static MealPlanDay CreateDay(DateOnly date, string mealName) {
        return new MealPlanDay(
            Date: date,
            Meals: [
                new MealPlanMeal(
                    Name: mealName,
                    Role: MealGroupRole.Custom,
                    Time: null,
                    Items: [
                        new MealPlanItem(
                            Name: "Продукт",
                            Quantity: FoodQuantity.Pieces(1m),
                            FridgeItemId: null,
                            Nutrition: NutritionTotals.Empty
                        ),
                    ]
                ),
            ]
        );
    }
}
