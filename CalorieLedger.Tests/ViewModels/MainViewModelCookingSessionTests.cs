using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Products;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Tests.TestDoubles;
using CalorieLedger.ViewModels;
using CalorieLedger.ViewModels.Meals;

namespace CalorieLedger.Tests.ViewModels;

public sealed class MainViewModelCookingSessionTests {
    [Fact]
    public void CookingSession_LogFood_OpensFoodEditorAndPersistsCalculatedServing() {
        var currentDate = new DateOnly(2026, 8, 17);

        var foodDiaryStore = new InMemoryFoodDiaryStore();

        var productCatalogStore = new InMemoryProductCatalogStore();

        var cookingSessionStore = new InMemoryCookingSessionStore();

        var cooking = new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: "Курица",
            Ingredients: [
                new CookingIngredient(
                    Id: Guid.NewGuid(),
                    Name: "Куриная грудка",
                    Quantity: FoodQuantity.Grams(500m),
                    Nutrition: new NutritionFacts(
                        Basis: NutritionBasis.Per100Grams,
                        CaloriesKcal: 100m,
                        ProteinG: 20m,
                        FatG: 2m,
                        CarbsG: 0m
                    )
                ),
            ],
            OutputWeightG: 400m
        );

        cookingSessionStore.Save(cooking);

        var profileStore = new InMemoryUserNutritionProfileStore(new SampleUserNutritionProfileProvider().GetCurrentProfile());

        var viewModel = new MainViewModel(
            new InMemoryBodyMeasurementStore(),
            profileStore,
            foodDiaryStore,
            productCatalogStore,
            cookingSessionStore,
            new FixedCurrentDateProvider(
                currentDate
            )
        );

        viewModel.OpenCookingSessionsCommand.Execute(null);

        var session = Assert.Single(viewModel.CookingSessionManager!.Sessions);

        session.LogFoodCommand.Execute(null);

        var editor = Assert.IsType<FoodLogEditorViewModel>(viewModel.FoodLogEditor);

        Assert.Equal(
            "Курица",
            editor.Name
        );

        Assert.Equal(
            125m,
            editor.CaloriesKcal
        );

        editor.QuantityValue = 200m;

        editor.SaveCommand.Execute(null);

        var meal = Assert.Single(foodDiaryStore.GetMeals(currentDate, currentDate));

        var food = Assert.Single(foodDiaryStore.GetFoodEntries([meal.Id]));

        Assert.Equal(
            FoodLogSource.CookingSession,
            food.Source
        );

        Assert.Equal(
            cooking.Id,
            food.SourceId
        );

        Assert.Equal(
            250m,
            viewModel.Today.ConsumedCaloriesKcal
        );

        Assert.NotNull(viewModel.CookingSessionManager);
    }
}
