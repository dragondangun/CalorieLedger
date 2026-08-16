using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Products;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Products;
using CalorieLedger.ViewModels.Meals;

namespace CalorieLedger.Tests.ViewModels.Meals;

public sealed class FoodLogEditorViewModelTests {
    [Fact]
    public void Constructor_ExistingFoodWithQuantity_InitializesAndCalculatesPreview() {
        var date = new DateOnly(2026, 8, 15);

        var service = new FoodLogEditorService(new InMemoryFoodDiaryStore());

        var draft = new FoodLogDraft(
            Id: Guid.NewGuid(),
            Date: date,
            Name: "Творог",
            MealRole: MealGroupRole.Snack,
            QuantityValue: 200m,
            QuantityUnit: FoodUnit.Gram,
            NutritionBasis: NutritionBasis.Per100Grams,
            CaloriesKcal: 120m,
            ProteinG: 17m,
            FatG: 5m,
            CarbsG: 3m,
            Source: FoodLogSource.Manual,
            SourceId: null
        );

        var productCatalogService = new ProductCatalogService(new InMemoryProductCatalogStore());
        var viewModel = new FoodLogEditorViewModel(
            editorService: service,
            draft: draft,
            productCatalogService: productCatalogService,
            currentDate: date,
            onSaved: () => { },
            onCancelled: () => { }
        );

        Assert.Equal(
            NutritionBasis.Per100Grams,
            viewModel.NutritionBasis
        );

        Assert.Equal(
            FoodUnit.Gram,
            viewModel.QuantityUnit
        );

        Assert.Equal(
            200m,
            viewModel.QuantityValue
        );

        Assert.Equal(
            "Итого: 240 ккал · Б: 34 г · Ж: 10 г · У: 6 г",
            viewModel.NutritionPreviewSummary
        );
    }

    [Fact]
    public void SelectCatalogProduct_PopulatesNutritionAndDefaultQuantity() {
        var date = new DateOnly(2026, 8, 15);

        var product = new ProductCatalogItem(
            Id: Guid.NewGuid(),
            Name: "Творог 5%",
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 120m,
                ProteinG: 17m,
                FatG: 5m,
                CarbsG: 3m
            ),
            Brand: "Test"
        );

        var catalogStore = new InMemoryProductCatalogStore();

        catalogStore.Save(product);

        var foodEditorService = new FoodLogEditorService(new InMemoryFoodDiaryStore());

        var viewModel = new FoodLogEditorViewModel(
            editorService: foodEditorService,
            productCatalogService: new ProductCatalogService(catalogStore),
            draft: foodEditorService.CreateNew(date),
            currentDate: date,
            onSaved: () => { },
            onCancelled: () => { }
        );

        viewModel.SelectedCatalogProduct = product;

        Assert.Equal(
            "Творог 5%",
            viewModel.Name
        );

        Assert.Equal(
            FoodUnit.Gram,
            viewModel.QuantityUnit
        );

        Assert.Equal(
            100m,
            viewModel.QuantityValue
        );

        Assert.Equal(
            NutritionBasis.Per100Grams,
            viewModel.NutritionBasis
        );

        Assert.Equal(
            120m,
            viewModel.CaloriesKcal
        );

        Assert.Equal(
            "Итого: 120 ккал · Б: 17 г · Ж: 5 г · У: 3 г",
            viewModel.NutritionPreviewSummary
        );
    }

    [Fact]
    public void Constructor_ExistingCatalogFood_PreservesLoggedQuantity() {
        var date = new DateOnly(2026, 8, 15);

        var product = new ProductCatalogItem(
            Id: Guid.NewGuid(),
            Name: "Творог",
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 120m,
                ProteinG: 17m,
                FatG: 5m,
                CarbsG: 3m
            )
        );

        var catalogStore = new InMemoryProductCatalogStore();

        catalogStore.Save(product);

        var draft = new FoodLogDraft(
            Id: Guid.NewGuid(),
            Date: date,
            Name: product.Name,
            MealRole: MealGroupRole.Snack,
            QuantityValue: 250m,
            QuantityUnit: FoodUnit.Gram,
            NutritionBasis: NutritionBasis.Per100Grams,
            CaloriesKcal: 120m,
            ProteinG: 17m,
            FatG: 5m,
            CarbsG: 3m,
            Source: FoodLogSource.CatalogProduct,
            SourceId: product.Id
        );

        var viewModel = new FoodLogEditorViewModel(
            editorService: new FoodLogEditorService(new InMemoryFoodDiaryStore()),
            productCatalogService: new ProductCatalogService(catalogStore),
            draft: draft,
            currentDate: date,
            onSaved: () => { },
            onCancelled: () => { }
        );

        Assert.Equal(
            product,
            viewModel.SelectedCatalogProduct
        );

        Assert.Equal(
            250m,
            viewModel.QuantityValue
        );

        Assert.Equal(
            "Итого: 300 ккал · Б: 42,5 г · Ж: 12,5 г · У: 7,5 г",
            viewModel.NutritionPreviewSummary
        );
    }

    [Fact]
    public void SaveCurrentProductToCatalog_CreatesReusableCatalogProduct() {
        var date = new DateOnly(2026, 8, 15);

        var catalogStore = new InMemoryProductCatalogStore();

        var foodEditorService = new FoodLogEditorService(new InMemoryFoodDiaryStore());

        var viewModel = new FoodLogEditorViewModel(
            editorService: foodEditorService,
            productCatalogService: new ProductCatalogService(catalogStore),
            draft: foodEditorService.CreateNew(date),
            currentDate: date,
            onSaved: () => { },
            onCancelled: () => { }
        );

        viewModel.Name = "Кефир";
        viewModel.QuantityValue = 300m;
        viewModel.QuantityUnit = FoodUnit.Milliliter;
        viewModel.NutritionBasis = NutritionBasis.Per100Milliliters;

        viewModel.CaloriesKcal = 53m;
        viewModel.ProteinG = 3m;
        viewModel.FatG = 2.5m;
        viewModel.CarbsG = 4m;

        viewModel.SaveCurrentProductToCatalogCommand.Execute(null);

        var saved = Assert.Single(catalogStore.GetAll());

        Assert.Equal(
            "Кефир",
            saved.Name
        );

        Assert.Equal(
            NutritionBasis.Per100Milliliters,
            saved.Nutrition.Basis
        );

        Assert.Equal(
            saved,
            viewModel.SelectedCatalogProduct
        );

        Assert.Contains(
            "сохранён в каталог",
            viewModel.CatalogActionSummary
        );
    }

    [Fact]
    public void SaveQuickApproximation_WithoutCalories_ShowsValidationAndDoesNotPersist() {
        var date = new DateOnly(2026, 8, 16);

        var foodDiaryStore = new InMemoryFoodDiaryStore();

        var editorService = new FoodLogEditorService(foodDiaryStore);

        var viewModel = new FoodLogEditorViewModel(
            editorService: editorService,
            productCatalogService: new ProductCatalogService(new InMemoryProductCatalogStore()),
            draft: editorService.CreateNewApproximation(date),
            currentDate: date,
            onSaved: () => { },
            onCancelled: () => { },
            isQuickApproximation: true
        );

        viewModel.Name = "Ужин в ресторане";

        viewModel.SaveCommand.Execute(null);

        Assert.True(viewModel.HasValidationErrors);

        Assert.Contains(
            "Введите примерную калорийность.",
            viewModel.ValidationMessages
        );

        Assert.Empty(foodDiaryStore.GetMeals(date, date));
    }

    [Fact]
    public void SaveQuickApproximation_WithCalories_PersistsNormalizedApproximation() {
        var date = new DateOnly(2026, 8, 16);

        var foodDiaryStore = new InMemoryFoodDiaryStore();

        foodDiaryStore.SetDateComplete(date, true);

        var editorService = new FoodLogEditorService(foodDiaryStore);

        var saved = false;

        var viewModel = new FoodLogEditorViewModel(
            editorService: editorService,
            productCatalogService: new ProductCatalogService(new InMemoryProductCatalogStore()),
            draft: editorService.CreateNewApproximation(date),
            currentDate: date,
            onSaved: () => saved = true,
            onCancelled: () => { },
            isQuickApproximation: true
        );

        viewModel.Name = "Ужин в ресторане";

        viewModel.MealRole = MealGroupRole.Dinner;

        viewModel.CaloriesKcal = 1350m;

        viewModel.Note = "Очень приблизительно";

        viewModel.SaveCommand.Execute(null);

        Assert.True(saved);

        var meal = Assert.Single(foodDiaryStore.GetMeals(date, date));

        Assert.Equal(
            MealGroupRole.Dinner,
            meal.Role
        );

        var food = Assert.Single(
            foodDiaryStore.GetFoodEntries(
                [meal.Id]
            )
        );

        Assert.Equal(
            "Ужин в ресторане",
            food.Name
        );

        Assert.Equal(
            FoodLogSource.Approximation,
            food.Source
        );

        Assert.True(food.IsApproximate);

        Assert.Null(food.SourceId);

        Assert.Equal(
            1m,
            food.Quantity.Value
        );

        Assert.Equal(
            FoodUnit.Portion,
            food.Quantity.Unit
        );

        Assert.Equal(
            NutritionBasis.Total,
            food.Nutrition.Basis
        );

        Assert.Equal(
            1350m,
            food.Nutrition.CaloriesKcal
        );

        Assert.Null(food.Nutrition.ProteinG);
        Assert.Null(food.Nutrition.FatG);
        Assert.Null(food.Nutrition.CarbsG);

        Assert.Equal(
            "Очень приблизительно",
            food.Note
        );

        Assert.Empty(foodDiaryStore.GetCompletedDates(date, date));
    }
}
