using CalorieLedger.Application.Fridge;
using CalorieLedger.Application.MealPlanning;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Tests.MealPlanning;

public sealed class MealPlanFoodDraftFactoryTests {
    private readonly MealPlanFoodDraftFactory factory = new();

    [Fact]
    public void Create_ConvertsGramTotalsToPer100GramsAndPreservesMealContext() {
        var date = new DateOnly(2026, 8, 19);
        var item = new MealPlanItem(
            Name: "Творог",
            Quantity: FoodQuantity.Grams(200m),
            FridgeItemId: null,
            Nutrition: new NutritionTotals(
                CaloriesKcal: 240m,
                ProteinG: 34m,
                FatG: 10m,
                CarbsG: 6m
            ),
            Note: "С ягодами"
        );

        var draft = factory.Create(
            item,
            date,
            MealGroupRole.Breakfast
        );

        Assert.Equal(date, draft.Date);
        Assert.Equal("Творог", draft.Name);
        Assert.Equal(MealGroupRole.Breakfast, draft.MealRole);
        Assert.Equal(200m, draft.QuantityValue);
        Assert.Equal(FoodUnit.Gram, draft.QuantityUnit);
        Assert.Equal(NutritionBasis.Per100Grams, draft.NutritionBasis);
        Assert.Equal(120m, draft.CaloriesKcal);
        Assert.Equal(17m, draft.ProteinG);
        Assert.Equal(5m, draft.FatG);
        Assert.Equal(3m, draft.CarbsG);
        Assert.Equal(FoodLogSource.Manual, draft.Source);
        Assert.Null(draft.SourceId);
        Assert.Equal("С ягодами", draft.Note);
    }

    [Fact]
    public void Create_ForExistingFridgeItem_UsesAuthoritativeFridgeNutrition() {
        var fridgeItemId = Guid.NewGuid();
        var fridgeStore = new InMemoryFridgeStore();
        fridgeStore.Save(
            new FridgeItem(
                Id: fridgeItemId,
                Name: "Йогурт из холодильника",
                Quantity: FoodQuantity.Milliliters(500m),
                Nutrition: new NutritionFacts(
                    Basis: NutritionBasis.Per100Milliliters,
                    CaloriesKcal: 60m,
                    ProteinG: 4m,
                    FatG: 2m,
                    CarbsG: 6m
                )
            )
        );
        var fridgeAwareFactory = new MealPlanFoodDraftFactory(
            new FridgeInventoryService(fridgeStore)
        );
        var item = new MealPlanItem(
            Name: "Йогурт по плану",
            Quantity: FoodQuantity.Milliliters(150m),
            FridgeItemId: fridgeItemId,
            Nutrition: new NutritionTotals(
                CaloriesKcal: 999m,
                ProteinG: 999m,
                FatG: 999m,
                CarbsG: 999m
            ),
            Note: "Добавить ягоды"
        );

        var draft = fridgeAwareFactory.Create(
            item,
            new DateOnly(2026, 8, 19),
            MealGroupRole.Snack
        );

        Assert.Equal("Йогурт по плану", draft.Name);
        Assert.Equal(150m, draft.QuantityValue);
        Assert.Equal(FoodLogSource.FridgeItem, draft.Source);
        Assert.Equal(fridgeItemId, draft.SourceId);
        Assert.Equal(NutritionBasis.Per100Milliliters, draft.NutritionBasis);
        Assert.Equal(60m, draft.CaloriesKcal);
        Assert.Equal(4m, draft.ProteinG);
        Assert.Equal(2m, draft.FatG);
        Assert.Equal(6m, draft.CarbsG);
        Assert.Equal("Добавить ягоды", draft.Note);
    }

    [Fact]
    public void Create_ForPieces_ConvertsTotalsToPerItem() {
        var item = new MealPlanItem(
            Name: "Яйца",
            Quantity: FoodQuantity.Pieces(2m),
            FridgeItemId: null,
            Nutrition: new NutritionTotals(
                CaloriesKcal: 140m,
                ProteinG: 12m,
                FatG: 10m,
                CarbsG: 1m
            )
        );

        var draft = factory.Create(
            item,
            new DateOnly(2026, 8, 19),
            MealGroupRole.Breakfast
        );

        Assert.Equal(NutritionBasis.PerItem, draft.NutritionBasis);
        Assert.Equal(70m, draft.CaloriesKcal);
        Assert.Equal(6m, draft.ProteinG);
        Assert.Equal(5m, draft.FatG);
        Assert.Equal(0.5m, draft.CarbsG);
    }

    [Fact]
    public void Create_ForPortions_KeepsTotalsAsTotalBasis() {
        var item = new MealPlanItem(
            Name: "Суп",
            Quantity: FoodQuantity.Portions(1m),
            FridgeItemId: null,
            Nutrition: new NutritionTotals(
                CaloriesKcal: 320m,
                ProteinG: null,
                FatG: 12m,
                CarbsG: null
            )
        );

        var draft = factory.Create(
            item,
            new DateOnly(2026, 8, 19),
            MealGroupRole.Lunch
        );

        Assert.Equal(NutritionBasis.Total, draft.NutritionBasis);
        Assert.Equal(320m, draft.CaloriesKcal);
        Assert.Null(draft.ProteinG);
        Assert.Equal(12m, draft.FatG);
        Assert.Null(draft.CarbsG);
    }
}
