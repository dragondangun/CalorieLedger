using CalorieLedger.Application.Fridge;
using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using System;

namespace CalorieLedger.Application.MealPlanning;

public sealed class MealPlanFoodDraftFactory {
    private readonly FridgeInventoryService? fridgeInventoryService;

    public MealPlanFoodDraftFactory(
        FridgeInventoryService? fridgeInventoryService = null
    ) {
        this.fridgeInventoryService = fridgeInventoryService;
    }

    public FoodLogDraft Create(
        MealPlanItem item,
        DateOnly date,
        MealGroupRole mealRole
    ) {
        ArgumentNullException.ThrowIfNull(item);

        if(item.Quantity.Value <= 0m) {
            throw new ArgumentOutOfRangeException(
                nameof(item),
                "Meal plan item quantity must be greater than zero."
            );
        }

        var fridgeDraft = TryCreateFridgeDraft(
            item,
            date,
            mealRole
        );

        if(fridgeDraft is not null) {
            return fridgeDraft;
        }

        var nutrition = ConvertNutrition(
            item.Quantity,
            item.Nutrition
        );

        return new FoodLogDraft(
            Id: Guid.NewGuid(),
            Date: date,
            Name: item.Name,
            MealRole: mealRole,
            QuantityValue: item.Quantity.Value,
            QuantityUnit: item.Quantity.Unit,
            NutritionBasis: nutrition.Basis,
            CaloriesKcal: nutrition.CaloriesKcal,
            ProteinG: nutrition.ProteinG,
            FatG: nutrition.FatG,
            CarbsG: nutrition.CarbsG,
            Source: item.FridgeItemId is null
                ? FoodLogSource.Manual
                : FoodLogSource.FridgeItem,
            SourceId: item.FridgeItemId,
            Note: item.Note
        );
    }

    private FoodLogDraft? TryCreateFridgeDraft(
        MealPlanItem item,
        DateOnly date,
        MealGroupRole mealRole
    ) {
        if(fridgeInventoryService is null
            || item.FridgeItemId is not Guid fridgeItemId
        ) {
            return null;
        }

        var fridgeDraft = fridgeInventoryService.CreateFoodLogDraft(
            fridgeItemId,
            date
        );

        if(fridgeDraft is null
            || fridgeDraft.QuantityUnit != item.Quantity.Unit
        ) {
            return null;
        }

        return fridgeDraft with {
            Name = item.Name,
            MealRole = mealRole,
            QuantityValue = item.Quantity.Value,
            Note = item.Note,
        };
    }

    private static NutritionFacts ConvertNutrition(
        FoodQuantity quantity,
        NutritionTotals totals
    ) {
        return quantity.Unit switch {
            FoodUnit.Gram => CreateScaledNutrition(
                NutritionBasis.Per100Grams,
                totals,
                100m / quantity.Value
            ),
            FoodUnit.Milliliter => CreateScaledNutrition(
                NutritionBasis.Per100Milliliters,
                totals,
                100m / quantity.Value
            ),
            FoodUnit.Piece => CreateScaledNutrition(
                NutritionBasis.PerItem,
                totals,
                1m / quantity.Value
            ),
            FoodUnit.Portion => new NutritionFacts(
                Basis: NutritionBasis.Total,
                CaloriesKcal: totals.CaloriesKcal,
                ProteinG: totals.ProteinG,
                FatG: totals.FatG,
                CarbsG: totals.CarbsG
            ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity.Unit,
                null
            )
        };
    }

    private static NutritionFacts CreateScaledNutrition(
        NutritionBasis basis,
        NutritionTotals totals,
        decimal multiplier
    ) {
        return new NutritionFacts(
            Basis: basis,
            CaloriesKcal: Scale(totals.CaloriesKcal, multiplier),
            ProteinG: Scale(totals.ProteinG, multiplier),
            FatG: Scale(totals.FatG, multiplier),
            CarbsG: Scale(totals.CarbsG, multiplier)
        );
    }

    private static decimal? Scale(decimal? value, decimal multiplier) {
        return value is null
            ? null
            : value.Value * multiplier;
    }
}
