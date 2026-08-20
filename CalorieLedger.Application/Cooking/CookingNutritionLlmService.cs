using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Cooking;

public sealed class CookingNutritionLlmService {
    public const string RequestProtocol = "calorieledger.cooking_nutrition_request.v1";
    public const string ResponseProtocol = "calorieledger.cooking_nutrition_response.v1";

    private static readonly JsonSerializerOptions SerializerOptions = new() {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions FingerprintSerializerOptions = new() {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        WriteIndented = false
    };

    public string ExportRequest(CookingSessionDraft draft) {
        ArgumentNullException.ThrowIfNull(draft);

        var content = CreateRequestContent(draft);
        var fingerprint = CreateFingerprint(content);
        var document = new RequestDocumentDto(
            Protocol: RequestProtocol,
            ResponseProtocol: ResponseProtocol,
            RequestFingerprint: fingerprint,
            SessionId: content.SessionId,
            Dish: content.Dish,
            Ingredients: content.Ingredients
        );

        return JsonSerializer.Serialize(document, SerializerOptions);
    }

    public string CreateResponseInstructions(CookingSessionDraft draft) {
        ArgumentNullException.ThrowIfNull(draft);

        var content = CreateRequestContent(draft);
        var fingerprint = CreateFingerprint(content);
        var example = new ResponseDocumentDto(
            Protocol: ResponseProtocol,
            SessionId: draft.Id,
            RequestFingerprint: fingerprint,
            NutritionPer100Grams: new NutritionDto(
                CaloriesKcal: 150m,
                ProteinG: 10m,
                FatG: 6m,
                CarbsG: 15m
            ),
            Note: "Краткое пояснение оценки, если нужно"
        );
        var json = JsonSerializer.Serialize(example, SerializerOptions);

        return $"""
Верни только JSON без Markdown и дополнительных пояснений.
Протокол ответа: {ResponseProtocol}.
sessionId и requestFingerprint скопируй из запроса без изменений.
nutritionPer100Grams — оценка КБЖУ готового блюда на 100 г с учётом состава, количества ингредиентов и указанного веса готового блюда.
Все четыре значения обязательны, должны быть числами не меньше 0.
note необязателен и может кратко описывать допущения расчёта.

Пример корректного ответа для текущего запроса:
{json}
""";
    }

    public CookingNutritionLlmParseResult ParseResponse(
        string? text,
        CookingSessionDraft currentDraft
    ) {
        ArgumentNullException.ThrowIfNull(currentDraft);

        if(string.IsNullOrWhiteSpace(text)) {
            return Failure(
                new CookingNutritionLlmParseError(
                    CookingNutritionLlmParseErrorCode.InvalidJson,
                    "$"
                )
            );
        }

        ResponseDocumentDto? document;

        try {
            document = JsonSerializer.Deserialize<ResponseDocumentDto>(
                text,
                SerializerOptions
            );
        }
        catch(JsonException) {
            return Failure(
                new CookingNutritionLlmParseError(
                    CookingNutritionLlmParseErrorCode.InvalidJson,
                    "$"
                )
            );
        }

        if(document is null) {
            return Failure(
                new CookingNutritionLlmParseError(
                    CookingNutritionLlmParseErrorCode.InvalidJson,
                    "$"
                )
            );
        }

        var errors = new List<CookingNutritionLlmParseError>();

        if(!string.Equals(document.Protocol, ResponseProtocol, StringComparison.Ordinal)) {
            errors.Add(
                new CookingNutritionLlmParseError(
                    CookingNutritionLlmParseErrorCode.UnsupportedProtocol,
                    "protocol"
                )
            );
        }

        if(document.SessionId != currentDraft.Id) {
            errors.Add(
                new CookingNutritionLlmParseError(
                    CookingNutritionLlmParseErrorCode.SessionMismatch,
                    "sessionId"
                )
            );
        }

        var expectedFingerprint = CreateFingerprint(CreateRequestContent(currentDraft));

        if(!string.Equals(
                document.RequestFingerprint,
                expectedFingerprint,
                StringComparison.Ordinal
            )
        ) {
            errors.Add(
                new CookingNutritionLlmParseError(
                    CookingNutritionLlmParseErrorCode.RequestMismatch,
                    "requestFingerprint"
                )
            );
        }

        if(document.NutritionPer100Grams is null) {
            errors.Add(
                new CookingNutritionLlmParseError(
                    CookingNutritionLlmParseErrorCode.MissingNutrition,
                    "nutritionPer100Grams"
                )
            );
        }
        else if(!IsValidNutrition(document.NutritionPer100Grams)) {
            errors.Add(
                new CookingNutritionLlmParseError(
                    CookingNutritionLlmParseErrorCode.InvalidNutrition,
                    "nutritionPer100Grams"
                )
            );
        }

        if(errors.Count > 0) {
            return Failure(errors);
        }

        var nutrition = document.NutritionPer100Grams!;

        return new CookingNutritionLlmParseResult(
            IsSuccess: true,
            NutritionPer100Grams: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: nutrition.CaloriesKcal,
                ProteinG: nutrition.ProteinG,
                FatG: nutrition.FatG,
                CarbsG: nutrition.CarbsG
            ),
            Note: NormalizeOptionalText(document.Note),
            Errors: []
        );
    }

    private static RequestContentDto CreateRequestContent(CookingSessionDraft draft) {
        return new RequestContentDto(
            SessionId: draft.Id,
            Dish: new DishDto(
                Name: draft.Name,
                OutputWeightG: draft.OutputWeightG,
                Note: draft.Note
            ),
            Ingredients: [
                .. draft.Ingredients.Select(
                    ingredient => new IngredientDto(
                        Id: ingredient.Id,
                        Name: ingredient.Name,
                        Quantity: new QuantityDto(
                            Value: ingredient.Quantity.Value,
                            Unit: FormatUnit(ingredient.Quantity.Unit)
                        ),
                        Nutrition: new IngredientNutritionDto(
                            Basis: FormatNutritionBasis(ingredient.Nutrition.Basis),
                            CaloriesKcal: ingredient.Nutrition.CaloriesKcal,
                            ProteinG: ingredient.Nutrition.ProteinG,
                            FatG: ingredient.Nutrition.FatG,
                            CarbsG: ingredient.Nutrition.CarbsG
                        ),
                        Source: FormatSource(ingredient.Source),
                        Note: ingredient.Note
                    )
                ),
            ]
        );
    }

    private static string CreateFingerprint(RequestContentDto content) {
        var json = JsonSerializer.Serialize(content, FingerprintSerializerOptions);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool IsValidNutrition(NutritionDto nutrition) {
        return nutrition.CaloriesKcal is >= 0m
            && nutrition.ProteinG is >= 0m
            && nutrition.FatG is >= 0m
            && nutrition.CarbsG is >= 0m;
    }

    private static string FormatUnit(FoodUnit unit) {
        return unit switch {
            FoodUnit.Gram => "g",
            FoodUnit.Milliliter => "ml",
            FoodUnit.Piece => "piece",
            FoodUnit.Portion => "portion",
            _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, null)
        };
    }

    private static string FormatNutritionBasis(NutritionBasis basis) {
        return basis switch {
            NutritionBasis.Per100Grams => "per_100_g",
            NutritionBasis.Per100Milliliters => "per_100_ml",
            NutritionBasis.PerItem => "per_item",
            NutritionBasis.Total => "total",
            _ => throw new ArgumentOutOfRangeException(nameof(basis), basis, null)
        };
    }

    private static string FormatSource(CookingIngredientSource source) {
        return source switch {
            CookingIngredientSource.Manual => "manual",
            CookingIngredientSource.ProductCatalog => "product_catalog",
            CookingIngredientSource.FridgeItem => "fridge_item",
            CookingIngredientSource.Recipe => "recipe",
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null)
        };
    }

    private static string? NormalizeOptionalText(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static CookingNutritionLlmParseResult Failure(
        params CookingNutritionLlmParseError[] errors
    ) {
        return Failure((IReadOnlyList<CookingNutritionLlmParseError>)errors);
    }

    private static CookingNutritionLlmParseResult Failure(
        IReadOnlyList<CookingNutritionLlmParseError> errors
    ) {
        return new CookingNutritionLlmParseResult(
            IsSuccess: false,
            NutritionPer100Grams: null,
            Note: null,
            Errors: errors
        );
    }

    private sealed record RequestDocumentDto(
        string Protocol,
        string ResponseProtocol,
        string RequestFingerprint,
        Guid SessionId,
        DishDto Dish,
        IReadOnlyList<IngredientDto> Ingredients
    );

    private sealed record RequestContentDto(
        Guid SessionId,
        DishDto Dish,
        IReadOnlyList<IngredientDto> Ingredients
    );

    private sealed record DishDto(
        string Name,
        decimal OutputWeightG,
        string? Note
    );

    private sealed record IngredientDto(
        Guid Id,
        string Name,
        QuantityDto Quantity,
        IngredientNutritionDto Nutrition,
        string Source,
        string? Note
    );

    private sealed record QuantityDto(
        decimal Value,
        string Unit
    );

    private sealed record IngredientNutritionDto(
        string Basis,
        decimal? CaloriesKcal,
        decimal? ProteinG,
        decimal? FatG,
        decimal? CarbsG
    );

    private sealed record ResponseDocumentDto(
        string? Protocol,
        Guid SessionId,
        string? RequestFingerprint,
        NutritionDto? NutritionPer100Grams,
        string? Note
    );

    private sealed record NutritionDto(
        decimal? CaloriesKcal,
        decimal? ProteinG,
        decimal? FatG,
        decimal? CarbsG
    );
}
