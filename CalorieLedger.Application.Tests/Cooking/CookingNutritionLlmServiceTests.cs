using System.Text.Json;
using CalorieLedger.Application.Cooking;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Tests.Cooking;

public sealed class CookingNutritionLlmServiceTests {
    [Fact]
    public void ExportRequest_IncludesDishIngredientsAndProtocols() {
        var service = new CookingNutritionLlmService();
        var draft = CreateDraft();

        using var document = JsonDocument.Parse(service.ExportRequest(draft));
        var root = document.RootElement;

        Assert.Equal(
            CookingNutritionLlmService.RequestProtocol,
            root.GetProperty("protocol").GetString()
        );
        Assert.Equal(
            CookingNutritionLlmService.ResponseProtocol,
            root.GetProperty("responseProtocol").GetString()
        );
        Assert.Equal(
            draft.Id,
            root.GetProperty("sessionId").GetGuid()
        );
        Assert.Equal(
            draft.Name,
            root.GetProperty("dish").GetProperty("name").GetString()
        );
        Assert.Equal(
            draft.OutputWeightG,
            root.GetProperty("dish").GetProperty("outputWeightG").GetDecimal()
        );

        var ingredients = root.GetProperty("ingredients").EnumerateArray().ToArray();
        var ingredient = Assert.Single(ingredients);
        Assert.Equal("Картофель", ingredient.GetProperty("name").GetString());
        Assert.Equal(300m, ingredient.GetProperty("quantity").GetProperty("value").GetDecimal());
        Assert.Equal("g", ingredient.GetProperty("quantity").GetProperty("unit").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("requestFingerprint").GetString()));
    }

    [Fact]
    public void ParseResponse_ValidCurrentRequest_ReturnsPer100GramNutrition() {
        var service = new CookingNutritionLlmService();
        var draft = CreateDraft();
        var response = CreateResponse(service, draft, 175m, 8m, 7m, 20m);

        var result = service.ParseResponse(response, draft);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        var nutrition = Assert.IsType<NutritionFacts>(result.NutritionPer100Grams);
        Assert.Equal(NutritionBasis.Per100Grams, nutrition.Basis);
        Assert.Equal(175m, nutrition.CaloriesKcal);
        Assert.Equal(8m, nutrition.ProteinG);
        Assert.Equal(7m, nutrition.FatG);
        Assert.Equal(20m, nutrition.CarbsG);
    }

    [Fact]
    public void ParseResponse_DraftChangedAfterExport_ReturnsRequestMismatch() {
        var service = new CookingNutritionLlmService();
        var draft = CreateDraft();
        var response = CreateResponse(service, draft, 175m, 8m, 7m, 20m);
        var changedDraft = draft with {
            OutputWeightG = 450m,
        };

        var result = service.ParseResponse(response, changedDraft);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            error => error.Code == CookingNutritionLlmParseErrorCode.RequestMismatch
        );
    }

    [Fact]
    public void ParseResponse_NegativeNutrition_ReturnsInvalidNutrition() {
        var service = new CookingNutritionLlmService();
        var draft = CreateDraft();
        var response = CreateResponse(service, draft, -1m, 8m, 7m, 20m);

        var result = service.ParseResponse(response, draft);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            error => error.Code == CookingNutritionLlmParseErrorCode.InvalidNutrition
        );
    }

    private static CookingSessionDraft CreateDraft() {
        return new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: "Картофельное пюре",
            Ingredients: [
                new CookingIngredient(
                    Id: Guid.NewGuid(),
                    Name: "Картофель",
                    Quantity: FoodQuantity.Grams(300m),
                    Nutrition: new NutritionFacts(
                        Basis: NutritionBasis.Per100Grams,
                        CaloriesKcal: 80m,
                        ProteinG: 2m,
                        FatG: 0.4m,
                        CarbsG: 17m
                    )
                ),
            ],
            OutputWeightG: 400m,
            Note: "С молоком"
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
  "note": "Оценка"
}
""";
    }
}
