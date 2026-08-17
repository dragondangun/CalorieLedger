using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Fridge;
using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Products;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Tests.TestDoubles;
using CalorieLedger.ViewModels;
using CalorieLedger.ViewModels.Meals;

namespace CalorieLedger.Tests.ViewModels;

public sealed class MainViewModelCookingSessionTests {
    [Fact]
    public void CookingSession_Cook_ConsumesIngredientAndCreatesFridgeOutput() {
        var currentDate = new DateOnly(2026, 8, 18);

        var foodDiaryStore = new InMemoryFoodDiaryStore();

        var productCatalogStore = new InMemoryProductCatalogStore();

        var cookingSessionStore =
        new InMemoryCookingSessionStore();

        var fridgeStore = new InMemoryFridgeStore();

        var ingredientStock = new FridgeItem(
            Id: Guid.NewGuid(),
            Name: "Курица",
            Quantity: FoodQuantity.Grams(500m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 100m,
                ProteinG: 20m,
                FatG: 2m,
                CarbsG: 0m
            )
        );

        fridgeStore.Save(ingredientStock);

        var cooking = new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: "Готовая курица",
            Ingredients: [
                new CookingIngredient(
                    Id: Guid.NewGuid(),
                    Name: ingredientStock.Name,
                    Quantity: FoodQuantity.Grams(200m),
                    Nutrition: ingredientStock.Nutrition,
                    Source: CookingIngredientSource.FridgeItem,
                    SourceId: ingredientStock.Id
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
            fridgeStore,
            new FixedCurrentDateProvider(
                currentDate
            )
        );

        viewModel.OpenCookingSessionsCommand.Execute(null);

        var session = Assert.Single(viewModel.CookingSessionManager!.Sessions);

        session.CookCommand.Execute(null);

        Assert.Equal(
            300m,
            fridgeStore.Get(ingredientStock.Id)?.Quantity.Value
        );

        var output = Assert.Single(
            fridgeStore.GetAll(),
            item => item.Source == FridgeItemSource.CookingSession && item.SourceId == cooking.Id
        );

        Assert.Equal(
            400m,
            output.Quantity.Value
        );

        Assert.Equal(
            50m,
            output.Nutrition.CaloriesKcal
        );

        var completedSession = Assert.Single(viewModel.CookingSessionManager.Sessions);

        Assert.True(completedSession.IsCompleted);

        Assert.False(completedSession.CookCommand.CanExecute(null));

        Assert.Null(viewModel.FoodLogEditor);
    }
}
