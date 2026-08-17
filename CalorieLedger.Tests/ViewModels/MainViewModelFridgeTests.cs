using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Fridge;
using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Products;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Tests.TestDoubles;
using CalorieLedger.ViewModels;
using CalorieLedger.ViewModels.Meals;

namespace CalorieLedger.Tests.ViewModels;

public sealed class MainViewModelFridgeTests {
    [Fact]
    public void Fridge_LogFood_Save_DeductsStockAndRefreshesToday() {
        var currentDate = new DateOnly(2026, 8, 17);

        var foodDiaryStore = new InMemoryFoodDiaryStore();

        var productCatalogStore = new InMemoryProductCatalogStore();

        var cookingSessionStore = new InMemoryCookingSessionStore();

        var fridgeStore = new InMemoryFridgeStore();

        var fridgeItem = new FridgeItem(
            Id: Guid.NewGuid(),
            Name: "Творог",
            Quantity: FoodQuantity.Grams(500m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 120m,
                ProteinG: 17m,
                FatG: 5m,
                CarbsG: 3m
            )
        );

        fridgeStore.Save(fridgeItem);

        var profileStore = new InMemoryUserNutritionProfileStore(
            new SampleUserNutritionProfileProvider().GetCurrentProfile()
        );

        var viewModel = new MainViewModel(
            new InMemoryBodyMeasurementStore(),
            profileStore,
            foodDiaryStore,
            productCatalogStore,
            cookingSessionStore,
            fridgeStore,
            new FixedCurrentDateProvider(currentDate)
        );

        viewModel.OpenFridgeCommand.Execute(null);

        var fridgeItemViewModel = Assert.Single(viewModel.FridgeManager!.Items);

        fridgeItemViewModel.LogFoodCommand.Execute(null);

        var editor = Assert.IsType<FoodLogEditorViewModel>(viewModel.FoodLogEditor);

        editor.QuantityValue = 250m;

        editor.SaveCommand.Execute(null);

        Assert.Equal(
            250m,
            fridgeStore.Get(fridgeItem.Id)?.Quantity.Value
        );

        Assert.Equal(
            300m,
            viewModel.Today.ConsumedCaloriesKcal
        );

        Assert.NotNull(viewModel.FridgeManager);

        var refreshedItem = Assert.Single(viewModel.FridgeManager.Items);

        Assert.Contains(
            "250",
            refreshedItem.QuantitySummary
        );
    }
}
