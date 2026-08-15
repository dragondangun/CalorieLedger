using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.ViewModels.Meals;

namespace CalorieLedger.Tests.ViewModels.Meals;

public sealed class FoodLogEditorViewModelTests {
    [Fact]
    public void Constructor_ExistingFoodWithQuantity_InitializesAndCalculatesPreview() {
        var date =
            new DateOnly(2026, 8, 15);

        var service =
            new FoodLogEditorService(
                new InMemoryFoodDiaryStore()
            );

        var draft =
            new FoodLogDraft(
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

        var viewModel =
            new FoodLogEditorViewModel(
                editorService: service,
                draft: draft,
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
}
