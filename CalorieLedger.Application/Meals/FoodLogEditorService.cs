using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalorieLedger.Application.Meals;

public sealed class FoodLogEditorService {
    private readonly IFoodDiaryStore foodDiaryStore;

    public FoodLogEditorService(IFoodDiaryStore foodDiaryStore) {
        ArgumentNullException.ThrowIfNull(foodDiaryStore);

        this.foodDiaryStore = foodDiaryStore;
    }

    public FoodLogDraft CreateNew(DateOnly date) {
        return new FoodLogDraft(
            Id: Guid.NewGuid(),
            Date: date,
            Name: string.Empty,
            MealRole: MealGroupRole.Snack,
            QuantityValue: null,
            QuantityUnit: FoodUnit.Gram,
            NutritionBasis: NutritionBasis.Per100Grams,
            CaloriesKcal: null,
            ProteinG: null,
            FatG: null,
            CarbsG: null
        );
    }

    public NutritionTotals? CalculatePreview(FoodLogDraft draft) {
        ArgumentNullException.ThrowIfNull(draft);

        if(draft.QuantityValue is not > 0m
            || !IsNutritionBasisCompatible(
                draft.NutritionBasis,
                draft.QuantityUnit
            )) {
            return null;
        }

        return NutritionCalculator.CalculateTotal(
            new NutritionFacts(
                Basis: draft.NutritionBasis,
                CaloriesKcal: draft.CaloriesKcal,
                ProteinG: draft.ProteinG,
                FatG: draft.FatG,
                CarbsG: draft.CarbsG
            ),
            new FoodQuantity(
                draft.QuantityValue.Value,
                draft.QuantityUnit
            )
        );
    }

    public FoodLogSaveResult Save(
        FoodLogDraft draft,
        DateOnly currentDate
    ) {
        ArgumentNullException.ThrowIfNull(draft);

        var errors = Validate(draft, currentDate);

        if(errors.Count > 0) {
            return new FoodLogSaveResult(
                IsSuccess: false,
                Errors: errors
            );
        }

        var mealName = GetMealName(draft.MealRole);

        var meal = foodDiaryStore
            .GetMeals(
                draft.Date,
                draft.Date
            )
            .FirstOrDefault(
                existing =>
                    existing.Role == draft.MealRole
                    && existing.Name == mealName
            );

        if(meal is null) {
            meal = new MealEntry(
                Id: Guid.NewGuid(),
                Date: draft.Date,
                Name: mealName,
                Role: draft.MealRole
            );

            foodDiaryStore.SaveMeal(meal);
        }

        foodDiaryStore.SaveFoodEntry(
            new FoodLogEntry(
                Id: draft.Id,
                MealEntryId: meal.Id,
                Name: draft.Name.Trim(),
                Quantity: new FoodQuantity(
                    draft.QuantityValue!.Value,
                    draft.QuantityUnit
                ),
                Nutrition: new NutritionFacts(
                    Basis: draft.NutritionBasis,
                    CaloriesKcal: draft.CaloriesKcal,
                    ProteinG: draft.ProteinG,
                    FatG: draft.FatG,
                    CarbsG: draft.CarbsG
                ),
                Source: draft.IsApproximate ? FoodLogSource.Approximation : FoodLogSource.Manual,
                IsApproximate: draft.IsApproximate,
                Note: string.IsNullOrWhiteSpace(draft.Note) ? null : draft.Note.Trim()
            )
        );

        foodDiaryStore.SetDateComplete(
            draft.Date,
            false
        );

        return new FoodLogSaveResult(
            IsSuccess: true,
            Errors: []
        );
    }

    private static IReadOnlyList<FoodLogValidationError> Validate(
        FoodLogDraft draft,
        DateOnly currentDate
    ) {
        var errors = new List<FoodLogValidationError>();

        if(draft.Id == Guid.Empty) {
            errors.Add(FoodLogValidationError.MissingId);
        }

        if(draft.Date > currentDate) {
            errors.Add(FoodLogValidationError.FutureDate);
        }

        if(string.IsNullOrWhiteSpace(draft.Name)) {
            errors.Add(FoodLogValidationError.MissingName);
        }

        if(draft.QuantityValue is not > 0m) {
            errors.Add(FoodLogValidationError.InvalidQuantity);
        }

        if(!IsNutritionBasisCompatible(
            draft.NutritionBasis,
            draft.QuantityUnit
        )) {
            errors.Add(
                FoodLogValidationError.IncompatibleNutritionBasis
            );
        }

        if(draft.CaloriesKcal < 0m) {
            errors.Add(
                FoodLogValidationError.InvalidCalories
            );
        }

        if(draft.ProteinG < 0m) {
            errors.Add(
                FoodLogValidationError.InvalidProtein
            );
        }

        if(draft.FatG < 0m) {
            errors.Add(
                FoodLogValidationError.InvalidFat
            );
        }

        if(draft.CarbsG < 0m) {
            errors.Add(
                FoodLogValidationError.InvalidCarbs
            );
        }

        return errors;
    }

    private static bool IsNutritionBasisCompatible(
        NutritionBasis nutritionBasis,
        FoodUnit quantityUnit
    ) {
        return nutritionBasis switch {
            NutritionBasis.Per100Grams => quantityUnit == FoodUnit.Gram,
            NutritionBasis.Per100Milliliters => quantityUnit == FoodUnit.Milliliter,
            NutritionBasis.PerItem => quantityUnit == FoodUnit.Piece,
            NutritionBasis.Total => true,
            _ => throw new ArgumentOutOfRangeException(
                nameof(nutritionBasis),
                nutritionBasis,
                null
            )
        };
    }

    private static string GetMealName(MealGroupRole role) {
        return role switch {
            MealGroupRole.Breakfast => "Завтрак",
            MealGroupRole.Lunch => "Обед",
            MealGroupRole.Dinner => "Ужин",
            MealGroupRole.Snack => "Перекусы",
            MealGroupRole.Custom => "Другое",

            _ => throw new ArgumentOutOfRangeException(
                nameof(role),
                role,
                null
            )
        };
    }
}
