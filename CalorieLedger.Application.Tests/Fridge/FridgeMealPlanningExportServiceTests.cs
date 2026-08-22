using System.Text.Json;
using CalorieLedger.Application.Fridge;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.Tests.Fridge;

public sealed class FridgeMealPlanningExportServiceTests {
    [Fact]
    public void Export_ProducesVersionedStructuredInventory() {
        var currentDate = new DateOnly(2026, 8, 19);
        var store = new InMemoryFridgeStore();

        var item = new FridgeItem(
            Id: Guid.NewGuid(),
            Name: "Творог",
            Quantity: FoodQuantity.Grams(450m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Per100Grams,
                CaloriesKcal: 121m,
                ProteinG: 17m,
                FatG: 5m,
                CarbsG: 3m
            ),
            ExpirationDate: currentDate.AddDays(2),
            Note: "открыт",
            Source: FridgeItemSource.CatalogProduct
        );

        store.Save(item);

        var service = new FridgeMealPlanningExportService(
            new FridgeInventoryService(store)
        );

        using var document = JsonDocument.Parse(service.Export(currentDate));
        var root = document.RootElement;

        Assert.Equal(
            FridgeMealPlanningExportService.Protocol,
            root.GetProperty("protocol").GetString()
        );

        Assert.Equal(
            "2026-08-19",
            root.GetProperty("asOfDate").GetString()
        );

        var exportedItem = Assert.Single(root.GetProperty("items").EnumerateArray());

        Assert.Equal("Творог", exportedItem.GetProperty("name").GetString());
        Assert.Equal(450m, exportedItem.GetProperty("quantity").GetProperty("value").GetDecimal());
        Assert.Equal("g", exportedItem.GetProperty("quantity").GetProperty("unit").GetString());
        Assert.Equal("per_100_g", exportedItem.GetProperty("nutrition").GetProperty("basis").GetString());
        Assert.Equal(121m, exportedItem.GetProperty("nutrition").GetProperty("caloriesKcal").GetDecimal());
        Assert.Equal("2026-08-21", exportedItem.GetProperty("expirationDate").GetString());
        Assert.Equal(2, exportedItem.GetProperty("daysUntilExpiration").GetInt32());
        Assert.Equal("открыт", exportedItem.GetProperty("note").GetString());
        Assert.Equal("catalog_product", exportedItem.GetProperty("source").GetString());
    }

    [Fact]
    public void Export_PreservesUnknownNutritionAsNull() {
        var store = new InMemoryFridgeStore();

        store.Save(
            new FridgeItem(
                Id: Guid.NewGuid(),
                Name: "Домашний соус",
                Quantity: FoodQuantity.Grams(200m),
                Nutrition: NutritionFacts.Empty(NutritionBasis.Per100Grams)
            )
        );

        var service = new FridgeMealPlanningExportService(
            new FridgeInventoryService(store)
        );

        using var document = JsonDocument.Parse(
            service.Export(new DateOnly(2026, 8, 19))
        );

        var item = Assert.Single(document.RootElement.GetProperty("items").EnumerateArray());
        var nutrition = item.GetProperty("nutrition");

        Assert.Equal(JsonValueKind.Null, nutrition.GetProperty("caloriesKcal").ValueKind);
        Assert.Equal(JsonValueKind.Null, nutrition.GetProperty("proteinG").ValueKind);
        Assert.Equal(JsonValueKind.Null, nutrition.GetProperty("fatG").ValueKind);
        Assert.Equal(JsonValueKind.Null, nutrition.GetProperty("carbsG").ValueKind);
    }

    [Fact]
    public void Export_OrdersExpiringItemsFirst() {
        var currentDate = new DateOnly(2026, 8, 19);
        var store = new InMemoryFridgeStore();

        store.Save(CreateItem("Без срока", null));
        store.Save(CreateItem("Позже", currentDate.AddDays(5)));
        store.Save(CreateItem("Скорее", currentDate.AddDays(1)));

        var service = new FridgeMealPlanningExportService(
            new FridgeInventoryService(store)
        );

        using var document = JsonDocument.Parse(service.Export(currentDate));

        var names = document.RootElement
            .GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("name").GetString())
            .ToArray();

        Assert.Equal(
            ["Скорее", "Позже", "Без срока"],
            names.Select(x => x!)
        );
    }

    private static FridgeItem CreateItem(
        string name,
        DateOnly? expirationDate
    ) {
        return new FridgeItem(
            Id: Guid.NewGuid(),
            Name: name,
            Quantity: FoodQuantity.Pieces(1m),
            Nutrition: NutritionFacts.Empty(NutritionBasis.PerItem),
            ExpirationDate: expirationDate
        );
    }
}
