using System.Text.Json;
using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Fridge;
using CalorieLedger.Application.Products;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.ViewModels.Cooking;

namespace CalorieLedger.Tests.ViewModels.Cooking;

public sealed class CookingSessionEditorViewModelLlmTests {
    [Fact]
    public void ApplyLlmNutrition_ValidResponse_SavesOverride() {
        var store = new InMemoryCookingSessionStore();
        var llmService = new CookingNutritionLlmService();
        var draft = CreateDraft();
        var saved = false;
        var viewModel = CreateViewModel(store, llmService, draft, () => saved = true);

        viewModel.PrepareLlmNutritionCommand.Execute(null);
        viewModel.LlmResponseText = CreateResponse(llmService, draft, 180m, 9m, 8m, 18m);
        viewModel.ApplyLlmNutritionCommand.Execute(null);

        Assert.True(viewModel.HasNutritionOverride);
        Assert.Contains("180", viewModel.NutritionOverrideSummary);

        viewModel.SaveCommand.Execute(null);

        Assert.True(saved);
        var stored = Assert.IsType<CookingSessionDraft>(store.Get(draft.Id));
        var nutrition = Assert.IsType<NutritionFacts>(stored.NutritionPer100GramsOverride);
        Assert.Equal(180m, nutrition.CaloriesKcal);
        Assert.Equal(9m, nutrition.ProteinG);
    }

    [Fact]
    public void ChangeOutputWeight_AfterLlmRequest_InvalidatesExchangeAndOverride() {
        var store = new InMemoryCookingSessionStore();
        var llmService = new CookingNutritionLlmService();
        var draft = CreateDraft();
        var viewModel = CreateViewModel(store, llmService, draft, () => { });

        viewModel.PrepareLlmNutritionCommand.Execute(null);
        viewModel.LlmResponseText = CreateResponse(llmService, draft, 180m, 9m, 8m, 18m);
        viewModel.ApplyLlmNutritionCommand.Execute(null);
        Assert.True(viewModel.HasNutritionOverride);

        viewModel.OutputWeightG = 450m;

        Assert.False(viewModel.HasNutritionOverride);
        Assert.Equal(string.Empty, viewModel.LlmRequestText);
        Assert.Equal(string.Empty, viewModel.LlmResponseText);
        Assert.Contains("Подготовьте новый запрос", viewModel.LlmActionSummary);
    }

    private static CookingSessionEditorViewModel CreateViewModel(
        InMemoryCookingSessionStore store,
        CookingNutritionLlmService llmService,
        CookingSessionDraft draft,
        Action onSaved
    ) {
        return new CookingSessionEditorViewModel(
            cookingSessionService: new CookingSessionService(store),
            cookingNutritionLlmService: llmService,
            productCatalogService: new ProductCatalogService(new InMemoryProductCatalogStore()),
            fridgeInventoryService: new FridgeInventoryService(new InMemoryFridgeStore()),
            draft: draft,
            isNew: false,
            onSaved: onSaved,
            onCancelled: () => { }
        );
    }

    private static CookingSessionDraft CreateDraft() {
        return new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: "Запеканка",
            Ingredients: [
                new CookingIngredient(
                    Id: Guid.NewGuid(),
                    Name: "Творог",
                    Quantity: FoodQuantity.Grams(300m),
                    Nutrition: new NutritionFacts(
                        Basis: NutritionBasis.Per100Grams,
                        CaloriesKcal: 120m,
                        ProteinG: 16m,
                        FatG: 5m,
                        CarbsG: 3m
                    )
                ),
            ],
            OutputWeightG: 400m
        );
    }

    private static string CreateResponse(
        CookingNutritionLlmService service,
        CookingSessionDraft draft,
        decimal calories,
        decimal protein,
        decimal fat,
        decimal carbs
    ) {
        using var request = JsonDocument.Parse(service.ExportRequest(draft));
        var fingerprint = request.RootElement.GetProperty("requestFingerprint").GetString();

        return $$"""
{
  "protocol": "{{CookingNutritionLlmService.ResponseProtocol}}",
  "sessionId": "{{draft.Id}}",
  "requestFingerprint": "{{fingerprint}}",
  "nutritionPer100Grams": {
    "caloriesKcal": {{calories.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
    "proteinG": {{protein.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
    "fatG": {{fat.ToString(System.Globalization.CultureInfo.InvariantCulture)}},
    "carbsG": {{carbs.ToString(System.Globalization.CultureInfo.InvariantCulture)}}
  },
  "note": null
}
""";
    }
}
