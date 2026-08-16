using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Products;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Tests.TestDoubles;
using CalorieLedger.ViewModels;
using CalorieLedger.ViewModels.Meals;
using CalorieLedger.ViewModels.Products;

namespace CalorieLedger.Tests.ViewModels;

public sealed class MainViewModelProductCatalogTests {
    [Fact]
    public void ProductCreatedInCatalog_IsAvailableWhenAddingFood() {
        var currentDate = new DateOnly(2026, 8, 16);

        var foodDiaryStore = new InMemoryFoodDiaryStore();

        var productCatalogStore = new InMemoryProductCatalogStore();

        var profileStore = new InMemoryUserNutritionProfileStore(
            new SampleUserNutritionProfileProvider().GetCurrentProfile()
        );

        var viewModel = new MainViewModel(
            new InMemoryBodyMeasurementStore(),
            profileStore,
            foodDiaryStore,
            productCatalogStore,
            new FixedCurrentDateProvider(
                currentDate
            )
        );

        viewModel.OpenProductCatalogCommand
            .Execute(null);

        Assert.True(viewModel.IsProductCatalogOpen);

        Assert.False(viewModel.IsTodayDashboardVisible);

        var catalog = Assert.IsType<ProductCatalogManagerViewModel>(viewModel.ProductCatalogManager);

        catalog.AddProductCommand.Execute(null);

        var editor = Assert.IsType<ProductCatalogEditorViewModel>(catalog.Editor);

        editor.Name = "Кефир";

        editor.NutritionBasis = NutritionBasis.Per100Milliliters;
        editor.CaloriesKcal = 53m;
        editor.ProteinG = 3m;
        editor.FatG = 2.5m;
        editor.CarbsG = 4m;

        editor.SaveCommand.Execute(null);

        catalog.CloseCommand.Execute(null);

        Assert.False(viewModel.IsProductCatalogOpen);

        Assert.True(viewModel.IsTodayDashboardVisible);

        viewModel.Today.AddFoodCommand.Execute(null);

        var foodEditor = Assert.IsType<FoodLogEditorViewModel>(viewModel.FoodLogEditor);

        var product = Assert.Single(foodEditor.CatalogResults);

        Assert.Equal(
            "Кефир",
            product.Name
        );

        Assert.Equal(
            53m,
            product.Nutrition.CaloriesKcal
        );
    }
}
