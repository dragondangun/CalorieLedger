using CalorieLedger.Application.Fridge;
using CalorieLedger.Application.Products;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.ViewModels.Fridge;

namespace CalorieLedger.Tests.ViewModels.Fridge;

public sealed class FridgeManagerViewModelTests {
    [Fact]
    public void ExportForMealPlanning_ShowsCurrentInventory() {
        var currentDate = new DateOnly(2026, 8, 19);
        var fridgeStore = new InMemoryFridgeStore();

        fridgeStore.Save(CreateItem("Кефир"));

        var viewModel = CreateViewModel(
            fridgeStore,
            currentDate
        );

        Assert.False(viewModel.IsMealPlanningExportVisible);

        viewModel.ExportForMealPlanningCommand.Execute(null);

        Assert.True(viewModel.IsMealPlanningExportVisible);
        Assert.Contains("calorieledger.fridge.v1", viewModel.MealPlanningExportText);
        Assert.Contains("Кефир", viewModel.MealPlanningExportText);
        Assert.Contains("2026-08-19", viewModel.MealPlanningExportText);

        viewModel.HideMealPlanningExportCommand.Execute(null);

        Assert.False(viewModel.IsMealPlanningExportVisible);
    }

    [Fact]
    public void ParseMealPlanningResponse_ValidResponse_ShowsMultiDayPreview() {
        var viewModel = CreateViewModel(
            new InMemoryFridgeStore(),
            new DateOnly(2026, 8, 19)
        );
        var jsonStart = viewModel.MealPlanningResponseInstructions.IndexOf('{');

        Assert.True(jsonStart >= 0);

        viewModel.MealPlanningResponseText = viewModel.MealPlanningResponseInstructions[jsonStart..];
        viewModel.ParseMealPlanningResponseCommand.Execute(null);

        Assert.True(viewModel.IsMealPlanningPreviewVisible);
        Assert.Contains("План распознан: 2 дн.", viewModel.MealPlanningResponseStatus);
        Assert.Contains("19.08.2026", viewModel.MealPlanningPreviewText);
        Assert.Contains("20.08.2026", viewModel.MealPlanningPreviewText);
        Assert.Contains("Завтрак", viewModel.MealPlanningPreviewText);
        Assert.Contains("Поздний завтрак", viewModel.MealPlanningPreviewText);
    }

    [Fact]
    public void ParseMealPlanningResponse_InvalidResponse_HidesPreviousPreview() {
        var viewModel = CreateViewModel(
            new InMemoryFridgeStore(),
            new DateOnly(2026, 8, 19)
        );
        var jsonStart = viewModel.MealPlanningResponseInstructions.IndexOf('{');

        viewModel.MealPlanningResponseText = viewModel.MealPlanningResponseInstructions[jsonStart..];
        viewModel.ParseMealPlanningResponseCommand.Execute(null);

        Assert.True(viewModel.IsMealPlanningPreviewVisible);

        viewModel.MealPlanningResponseText = "не JSON";
        viewModel.ParseMealPlanningResponseCommand.Execute(null);

        Assert.False(viewModel.IsMealPlanningPreviewVisible);
        Assert.Empty(viewModel.MealPlanningPreviewText);
        Assert.StartsWith("Не удалось разобрать план.", viewModel.MealPlanningResponseStatus);
    }

    [Fact]
    public void RefreshItems_WhenExportIsVisible_RegeneratesExport() {
        var currentDate = new DateOnly(2026, 8, 19);
        var fridgeStore = new InMemoryFridgeStore();

        fridgeStore.Save(CreateItem("Кефир"));

        var viewModel = CreateViewModel(
            fridgeStore,
            currentDate
        );

        viewModel.ExportForMealPlanningCommand.Execute(null);

        fridgeStore.Save(CreateItem("Творог"));

        viewModel.RefreshItems();

        Assert.Contains("Кефир", viewModel.MealPlanningExportText);
        Assert.Contains("Творог", viewModel.MealPlanningExportText);
    }

    private static FridgeManagerViewModel CreateViewModel(
        InMemoryFridgeStore fridgeStore,
        DateOnly currentDate
    ) {
        return new FridgeManagerViewModel(
            fridgeInventoryService: new FridgeInventoryService(fridgeStore),
            productCatalogService: new ProductCatalogService(new InMemoryProductCatalogStore()),
            currentDate: currentDate,
            logFood: _ => { },
            onClosed: () => { }
        );
    }

    private static FridgeItem CreateItem(string name) {
        return new FridgeItem(
            Id: Guid.NewGuid(),
            Name: name,
            Quantity: FoodQuantity.Grams(500m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 100m,
                ProteinG: 10m,
                FatG: 5m,
                CarbsG: 4m
            )
        );
    }
}
