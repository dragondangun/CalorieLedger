using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using System;

namespace CalorieLedger.Application.Meals;

public sealed record FoodLogDraft(
    Guid Id,
    DateOnly Date,
    string Name,
    MealGroupRole MealRole,
    decimal? QuantityValue,
    FoodUnit QuantityUnit,
    NutritionBasis NutritionBasis,
    decimal? CaloriesKcal,
    decimal? ProteinG,
    decimal? FatG,
    decimal? CarbsG,
    bool IsApproximate = false,
    string? Note = null
);
