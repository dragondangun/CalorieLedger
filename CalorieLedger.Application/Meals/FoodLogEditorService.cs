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
            CarbsG: null,
            Source: FoodLogSource.Manual,
            SourceId: null
        );
    }

    public FoodLogDraft CreateNewApproximation(DateOnly date) {
        return new FoodLogDraft(
            Id: Guid.NewGuid(),
            Date: date,
            Name: string.Empty,
            MealRole: MealGroupRole.Custom,
            QuantityValue: 1m,
            QuantityUnit: FoodUnit.Portion,
            NutritionBasis: NutritionBasis.Total,
            CaloriesKcal: null,
            ProteinG: null,
            FatG: null,
            CarbsG: null,
            Source: FoodLogSource.Approximation,
            SourceId: null,
            IsApproximate: true
        );
    }

    public FoodLogDraft? Load(Guid id) {
        var foodEntry = foodDiaryStore.GetFoodEntry(id);

        if(foodEntry is null) {
            return null;
        }

        var meal = foodDiaryStore.GetMeal(foodEntry.MealEntryId)
            ?? throw new InvalidOperationException(
                "Food log entry references a missing meal."
            );

        return new FoodLogDraft(
            Id: foodEntry.Id,
            Date: meal.Date,
            Name: foodEntry.Name,
            MealRole: meal.Role,
            QuantityValue: foodEntry.Quantity.Value,
            QuantityUnit: foodEntry.Quantity.Unit,
            NutritionBasis: foodEntry.Nutrition.Basis,
            CaloriesKcal: foodEntry.Nutrition.CaloriesKcal,
            ProteinG: foodEntry.Nutrition.ProteinG,
            FatG: foodEntry.Nutrition.FatG,
            CarbsG: foodEntry.Nutrition.CarbsG,
            Source: foodEntry.Source,
            SourceId: foodEntry.SourceId,
            IsApproximate: foodEntry.IsApproximate,
            Note: foodEntry.Note
        );
    }

    public NutritionTotals? CalculatePreview(FoodLogDraft draft) {
        ArgumentNullException.ThrowIfNull(draft);

        if(draft.QuantityValue is not > 0m
            || !IsNutritionBasisCompatible(
                draft.NutritionBasis,
                draft.QuantityUnit
            )
        ) {
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

    public FoodLogSaveResult Save(FoodLogDraft draft, DateOnly currentDate) {
        ArgumentNullException.ThrowIfNull(draft);

        var errors = Validate(
            draft,
            currentDate
        );

        if(errors.Count > 0) {
            return new FoodLogSaveResult(
                IsSuccess: false,
                Errors: errors
            );
        }

        var existingEntry = foodDiaryStore.GetFoodEntry(draft.Id);

        MealEntry? existingMeal = null;

        if(existingEntry is not null) {
            existingMeal = foodDiaryStore.GetMeal(existingEntry.MealEntryId)
                ?? throw new InvalidOperationException("Food log entry references a missing meal.");
        }

        var mealName = GetMealName(draft.MealRole);

        var targetMeal = foodDiaryStore
            .GetMeals(draft.Date, draft.Date)
            .FirstOrDefault(meal => meal.Role == draft.MealRole && meal.Name == mealName);

        if(targetMeal is null) {
            targetMeal = new MealEntry(
                Id: Guid.NewGuid(),
                Date: draft.Date,
                Name: mealName,
                Role: draft.MealRole
            );

            foodDiaryStore.SaveMeal(targetMeal);
        }

        foodDiaryStore.SaveFoodEntry(
            new FoodLogEntry(
                Id: draft.Id,
                MealEntryId: targetMeal.Id,
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
                Source: ResolveSource(draft),
                SourceId: draft.SourceId,
                IsApproximate: draft.IsApproximate,
                Note: string.IsNullOrWhiteSpace(draft.Note)
                    ? null
                    : draft.Note.Trim()
            )
        );

        if(existingMeal is not null && existingMeal.Id != targetMeal.Id) {
            RemoveMealIfEmpty(existingMeal.Id);

            if(existingMeal.Date != draft.Date) {
                foodDiaryStore.SetDateComplete(
                    existingMeal.Date,
                    false
                );
            }
        }

        foodDiaryStore.SetDateComplete(
            draft.Date,
            false
        );

        return new FoodLogSaveResult(
            IsSuccess: true,
            Errors: []
        );
    }

    public bool Delete(Guid id) {
        var foodEntry = foodDiaryStore.GetFoodEntry(id);

        if(foodEntry is null) {
            return false;
        }

        var meal = foodDiaryStore.GetMeal(foodEntry.MealEntryId)
            ?? throw new InvalidOperationException("Food log entry references a missing meal.");

        if(!foodDiaryStore.DeleteFoodEntry(id)) {
            return false;
        }

        RemoveMealIfEmpty(meal.Id);

        foodDiaryStore.SetDateComplete(meal.Date, false);

        return true;
    }

    private void RemoveMealIfEmpty(Guid mealId) {
        if(foodDiaryStore.GetFoodEntries([mealId]).Count > 0) {
            return;
        }

        foodDiaryStore.DeleteMeal(mealId);
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
            errors.Add(FoodLogValidationError.IncompatibleNutritionBasis);
        }

        if(draft.CaloriesKcal < 0m) {
            errors.Add(FoodLogValidationError.InvalidCalories);
        }

        if(draft.ProteinG < 0m) {
            errors.Add(FoodLogValidationError.InvalidProtein);
        }

        if(draft.FatG < 0m) {
            errors.Add(FoodLogValidationError.InvalidFat);
        }

        if(draft.CarbsG < 0m) {
            errors.Add(FoodLogValidationError.InvalidCarbs);
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

    private static FoodLogSource ResolveSource(FoodLogDraft draft) {
        return draft.Source switch {
            FoodLogSource.Manual when draft.IsApproximate => FoodLogSource.Approximation,
            FoodLogSource.Approximation when !draft.IsApproximate => FoodLogSource.Manual,
            _ => draft.Source
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
