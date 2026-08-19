using System.Text.Encodings.Web;
using System.Text.Json;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Fridge;

public sealed class FridgeMealPlanningExportService {
    public const string Protocol = "calorieledger.fridge.v1";

    private static readonly JsonSerializerOptions SerializerOptions = new() {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly FridgeInventoryService fridgeInventoryService;

    public FridgeMealPlanningExportService(FridgeInventoryService fridgeInventoryService) {
        ArgumentNullException.ThrowIfNull(fridgeInventoryService);

        this.fridgeInventoryService = fridgeInventoryService;
    }

    public string Export(DateOnly asOfDate) {
        var items = fridgeInventoryService
            .Search(null)
            .OrderBy(item => item.ExpirationDate is null)
            .ThenBy(item => item.ExpirationDate)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id)
            .Select(item => CreateExportItem(item, asOfDate))
            .ToArray();

        var document = new FridgeExportDocument(
            Protocol: Protocol,
            AsOfDate: asOfDate,
            Items: items
        );

        return JsonSerializer.Serialize(
            document,
            SerializerOptions
        );
    }

    private static FridgeExportItem CreateExportItem(
        FridgeItem item,
        DateOnly asOfDate
    ) {
        return new FridgeExportItem(
            Id: item.Id,
            Name: item.Name,
            Quantity: new FridgeExportQuantity(
                Value: item.Quantity.Value,
                Unit: FormatUnit(item.Quantity.Unit)
            ),
            Nutrition: new FridgeExportNutrition(
                Basis: FormatNutritionBasis(item.Nutrition.Basis),
                CaloriesKcal: item.Nutrition.CaloriesKcal,
                ProteinG: item.Nutrition.ProteinG,
                FatG: item.Nutrition.FatG,
                CarbsG: item.Nutrition.CarbsG
            ),
            ExpirationDate: item.ExpirationDate,
            DaysUntilExpiration: item.ExpirationDate?.DayNumber - asOfDate.DayNumber,
            Note: item.Note,
            Source: FormatSource(item.Source)
        );
    }

    private static string FormatUnit(FoodUnit unit) {
        return unit switch {
            FoodUnit.Gram => "g",
            FoodUnit.Milliliter => "ml",
            FoodUnit.Piece => "piece",
            FoodUnit.Portion => "portion",
            _ => throw new ArgumentOutOfRangeException(
                nameof(unit),
                unit,
                null
            )
        };
    }

    private static string FormatNutritionBasis(NutritionBasis basis) {
        return basis switch {
            NutritionBasis.Per100Grams => "per_100_g",
            NutritionBasis.Per100Milliliters => "per_100_ml",
            NutritionBasis.PerItem => "per_item",
            NutritionBasis.Total => "total",
            _ => throw new ArgumentOutOfRangeException(
                nameof(basis),
                basis,
                null
            )
        };
    }

    private static string FormatSource(FridgeItemSource source) {
        return source switch {
            FridgeItemSource.Manual => "manual",
            FridgeItemSource.CatalogProduct => "catalog_product",
            FridgeItemSource.CookingSession => "cooking_session",
            _ => throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                null
            )
        };
    }

    private sealed record FridgeExportDocument(
        string Protocol,
        DateOnly AsOfDate,
        IReadOnlyList<FridgeExportItem> Items
    );

    private sealed record FridgeExportItem(
        Guid Id,
        string Name,
        FridgeExportQuantity Quantity,
        FridgeExportNutrition Nutrition,
        DateOnly? ExpirationDate,
        int? DaysUntilExpiration,
        string? Note,
        string Source
    );

    private sealed record FridgeExportQuantity(
        decimal Value,
        string Unit
    );

    private sealed record FridgeExportNutrition(
        string Basis,
        decimal? CaloriesKcal,
        decimal? ProteinG,
        decimal? FatG,
        decimal? CarbsG
    );
}
