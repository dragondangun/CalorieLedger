using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;
using System;
using System.Linq;

namespace CalorieLedger.ViewModels.Meals;

public static class FoodDiaryPresentationFactory {
    public static FoodDiaryMealGroupViewModel CreateMealGroup(
        FoodDiaryMealSnapshot meal,
        Action<Guid> editFood,
        Action<Guid> deleteFood
    ) {
        ArgumentNullException.ThrowIfNull(meal);
        ArgumentNullException.ThrowIfNull(editFood);
        ArgumentNullException.ThrowIfNull(deleteFood);

        return new FoodDiaryMealGroupViewModel(
            name: meal.Name,
            timeSummary: FormatTime(meal.EatenAt),
            foodItems: meal.FoodItems.Select(
                item => CreateFoodItem(
                    item,
                    editFood,
                    deleteFood
                )
            )
        );
    }

    private static FoodDiaryFoodItemViewModel CreateFoodItem(
        FoodDiaryFoodSnapshotItem item,
        Action<Guid> editFood,
        Action<Guid> deleteFood
    ) {
        return new FoodDiaryFoodItemViewModel(
            id: item.Id,
            name: item.Name,
            quantitySummary: FormatQuantity(item.Quantity),
            caloriesSummary: FormatCalories(item.Totals.CaloriesKcal),
            macrosSummary: FormatMacros(item.Totals),
            onEdit: editFood,
            onDelete: deleteFood,
            isApproximate: item.IsApproximate,
            caloriesKcal: item.Totals.CaloriesKcal,
            proteinG: item.Totals.ProteinG,
            fatG: item.Totals.FatG,
            carbsG: item.Totals.CarbsG
        );
    }

    private static string FormatQuantity(FoodQuantity quantity) {
        var unit = quantity.Unit switch {
            FoodUnit.Gram => "г",
            FoodUnit.Milliliter => "мл",
            FoodUnit.Piece => "шт",
            FoodUnit.Portion => "порц.",
            _ => quantity.Unit.ToString()
        };

        return $"{quantity.Value:0.##} {unit}";
    }

    private static string FormatCalories(decimal? caloriesKcal) {
        return caloriesKcal is null ? "калорийность неизвестна" : $"{caloriesKcal.Value:0} ккал";
    }

    private static string FormatMacros(NutritionTotals totals) {
        if(totals.ProteinG is null
            && totals.FatG is null
            && totals.CarbsG is null
        ) {
            return "Б/Ж/У неизвестны";
        }

        return $"Б: {FormatNutrient(totals.ProteinG)} г · Ж: {FormatNutrient(totals.FatG)} г · У: {FormatNutrient(totals.CarbsG)} г";
    }

    private static string FormatNutrient(decimal? value) {
        return value is null ? "—" : $"{value.Value:0.#}";
    }

    private static string FormatTime(TimeOnly? time) {
        return time is null ? string.Empty : time.Value.ToString("HH:mm");
    }
}
