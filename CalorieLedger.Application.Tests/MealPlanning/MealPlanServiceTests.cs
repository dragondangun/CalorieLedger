using CalorieLedger.Application.MealPlanning;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Tests.MealPlanning;

public sealed class MealPlanServiceTests {
    [Fact]
    public void Save_ReplacesOnlyDatesPresentInNewPlan() {
        var store = new InMemoryMealPlanStore();
        var service = new MealPlanService(store);

        service.Save(
            new MealPlan([
                CreateDay(new DateOnly(2026, 8, 19), "Старый 19"),
                CreateDay(new DateOnly(2026, 8, 20), "Старый 20"),
                CreateDay(new DateOnly(2026, 8, 21), "Старый 21"),
                CreateDay(new DateOnly(2026, 8, 23), "Старый 23"),
            ])
        );

        service.Save(
            new MealPlan([
                CreateDay(new DateOnly(2026, 8, 20), "Новый 20"),
                CreateDay(new DateOnly(2026, 8, 22), "Новый 22"),
            ])
        );

        var days = service.GetAll();

        Assert.Equal(
            new[] {
                new DateOnly(2026, 8, 19),
                new DateOnly(2026, 8, 20),
                new DateOnly(2026, 8, 21),
                new DateOnly(2026, 8, 22),
                new DateOnly(2026, 8, 23),
            },
            days.Select(day => day.Date).ToArray()
        );
        Assert.Equal("Старый 19", days[0].Meals[0].Name);
        Assert.Equal("Новый 20", days[1].Meals[0].Name);
        Assert.Equal("Старый 21", days[2].Meals[0].Name);
        Assert.Equal("Новый 22", days[3].Meals[0].Name);
        Assert.Equal("Старый 23", days[4].Meals[0].Name);
    }

    [Fact]
    public void Save_DuplicateDates_Throws() {
        var service = new MealPlanService(new InMemoryMealPlanStore());
        var date = new DateOnly(2026, 8, 19);

        Assert.Throws<ArgumentException>(
            () => service.Save(
                new MealPlan([
                    CreateDay(date, "Первый"),
                    CreateDay(date, "Второй"),
                ])
            )
        );
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
                            Quantity: FoodQuantity.Grams(100m),
                            FridgeItemId: null,
                            Nutrition: NutritionTotals.Empty
                        ),
                    ]
                ),
            ]
        );
    }
}
