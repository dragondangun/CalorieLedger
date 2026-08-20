using CalorieLedger.Application.Meals;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Fridge;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Products;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalorieLedger.Application.Cooking;

public sealed class CookingSessionService {
    private readonly ICookingSessionStore cookingSessionStore;

    public CookingSessionService(ICookingSessionStore cookingSessionStore) {
        ArgumentNullException.ThrowIfNull(cookingSessionStore);

        this.cookingSessionStore = cookingSessionStore;
    }

    public CookingSessionDraft CreateNew() {
        return new CookingSessionDraft(
            Id: Guid.NewGuid(),
            Name: string.Empty,
            Ingredients: [],
            OutputWeightG: 0m
        );
    }

    public CookingSessionDraft? Load(Guid id) {
        return cookingSessionStore.Get(id);
    }

    public IReadOnlyList<CookingSessionDraft> Search(string? query) {
        var sessions = cookingSessionStore.GetAll();

        if(string.IsNullOrWhiteSpace(query)) {
            return sessions;
        }

        var normalizedQuery = query.Trim();

        return [
            .. sessions.Where(
                session => session.Name.Contains(
                    normalizedQuery,
                    StringComparison.OrdinalIgnoreCase
                )
            ),
        ];
    }

    public CookingIngredient? CreateCatalogIngredient(
        ProductCatalogItem product,
        decimal quantityValue
    ) {
        ArgumentNullException.ThrowIfNull(product);

        if(quantityValue <= 0m) {
            return null;
        }

        var unit = GetUnitForNutritionBasis(
            product.Nutrition.Basis
        );

        if(unit is null) {
            return null;
        }

        return new CookingIngredient(
            Id: Guid.NewGuid(),
            Name: product.Name,
            Quantity: new FoodQuantity(
                quantityValue,
                unit.Value
            ),
            Nutrition: product.Nutrition,
            Source: CookingIngredientSource.ProductCatalog,
            SourceId: product.Id
        );
    }

    public CookingIngredient? CreateFridgeIngredient(
        FridgeItem fridgeItem,
        decimal quantityValue
    ) {
        ArgumentNullException.ThrowIfNull(fridgeItem);

        if(quantityValue <= 0m || quantityValue > fridgeItem.Quantity.Value) {
            return null;
        }

        var quantity = new FoodQuantity(
            quantityValue,
            fridgeItem.Quantity.Unit
        );

        if(!IsNutritionBasisCompatible(
                fridgeItem.Nutrition.Basis,
                quantity
            )
        ) {
            return null;
        }

        return new CookingIngredient(
            Id: Guid.NewGuid(),
            Name: fridgeItem.Name,
            Quantity: quantity,
            Nutrition: fridgeItem.Nutrition,
            Source: CookingIngredientSource.FridgeItem,
            SourceId: fridgeItem.Id,
            Note: fridgeItem.Note
        );
    }

    public CookingNutritionResult? CalculatePreview(CookingSessionDraft draft) {
        ArgumentNullException.ThrowIfNull(draft);

        if(draft.OutputWeightG <= 0m
            || draft.Ingredients.Count == 0
            || draft.Ingredients.Any(
                ingredient =>
                    ingredient.Quantity.Value <= 0m
                    || !IsNutritionBasisCompatible(
                        ingredient.Nutrition.Basis,
                        ingredient.Quantity
                    )
            )
        ) {
            return null;
        }

        return CookingNutritionCalculator.Calculate(draft);
    }

    public CookingSessionSaveResult Save(CookingSessionDraft draft) {
        ArgumentNullException.ThrowIfNull(draft);

        var errors = Validate(draft);

        if(errors.Count > 0) {
            return new CookingSessionSaveResult(
                IsSuccess: false,
                Errors: errors
            );
        }

        var normalized = draft with {
            Name = draft.Name.Trim(),
            Note = NormalizeOptionalText(draft.Note),
            Ingredients = [
                .. draft.Ingredients.Select(
                    ingredient => ingredient with {
                        Name = ingredient.Name.Trim(),
                        Note = NormalizeOptionalText(ingredient.Note),
                    }
                ),
            ],
        };

        cookingSessionStore.Save(normalized);

        return new CookingSessionSaveResult(
            IsSuccess: true,
            Errors: []
        );
    }

    public bool Delete(Guid id) {
        return cookingSessionStore.Delete(id);
    }

    public FoodLogDraft? CreateFoodLogDraft(
        Guid cookingSessionId,
        DateOnly date
    ) {
        var session = cookingSessionStore.Get(cookingSessionId);

        if(session is null) {
            return null;
        }

        var nutrition = CalculatePreview(session);

        if(nutrition is null) {
            return null;
        }

        return new FoodLogDraft(
            Id: Guid.NewGuid(),
            Date: date,
            Name: session.Name,
            MealRole: MealGroupRole.Custom,
            QuantityValue: 100m,
            QuantityUnit: FoodUnit.Gram,
            NutritionBasis: NutritionBasis.Per100Grams,
            CaloriesKcal: nutrition.NutritionPer100Grams.CaloriesKcal,
            ProteinG: nutrition.NutritionPer100Grams.ProteinG,
            FatG: nutrition.NutritionPer100Grams.FatG,
            CarbsG: nutrition.NutritionPer100Grams.CarbsG,
            Source: FoodLogSource.CookingSession,
            SourceId: session.Id
        );
    }

    private static IReadOnlyList<CookingSessionValidationError> Validate(CookingSessionDraft draft) {
        var errors = new List<CookingSessionValidationError>();

        if(draft.Id == Guid.Empty) {
            errors.Add(CookingSessionValidationError.MissingId);
        }

        if(string.IsNullOrWhiteSpace(draft.Name)) {
            errors.Add(CookingSessionValidationError.MissingName);
        }

        if(draft.Ingredients.Count == 0) {
            errors.Add(CookingSessionValidationError.NoIngredients);
        }

        if(draft.OutputWeightG <= 0m) {
            errors.Add(CookingSessionValidationError.InvalidOutputWeight);
        }

        if(draft.NutritionPer100GramsOverride is not null
            && (draft.NutritionPer100GramsOverride.Basis != NutritionBasis.Per100Grams
                || HasIncompleteNutrition(draft.NutritionPer100GramsOverride)
                || HasInvalidNutrition(draft.NutritionPer100GramsOverride))
        ) {
            errors.Add(CookingSessionValidationError.InvalidNutritionOverride);
        }

        foreach(var ingredient in draft.Ingredients) {
            if(ingredient.Id == Guid.Empty) {
                AddUnique(
                    errors,
                    CookingSessionValidationError.InvalidIngredientId
                );
            }

            if(string.IsNullOrWhiteSpace(ingredient.Name)) {
                AddUnique(
                    errors,
                    CookingSessionValidationError.MissingIngredientName
                );
            }

            if(ingredient.Quantity.Value <= 0m) {
                AddUnique(
                    errors,
                    CookingSessionValidationError.InvalidIngredientQuantity
                );
            }

            if(!IsNutritionBasisCompatible(ingredient.Nutrition.Basis, ingredient.Quantity)) {
                AddUnique(
                    errors,
                    CookingSessionValidationError.IncompatibleIngredientNutritionBasis
                );
            }

            if(HasInvalidNutrition(ingredient.Nutrition)) {
                AddUnique(
                    errors,
                    CookingSessionValidationError.InvalidIngredientNutrition
                );
            }
        }

        return errors;
    }

    private static bool IsNutritionBasisCompatible(
        NutritionBasis basis,
        FoodQuantity quantity
    ) {
        return basis switch {
            NutritionBasis.Per100Grams => quantity.Unit == FoodUnit.Gram,
            NutritionBasis.Per100Milliliters => quantity.Unit == FoodUnit.Milliliter,
            NutritionBasis.PerItem => quantity.Unit == FoodUnit.Piece,
            NutritionBasis.Total => quantity.Unit == FoodUnit.Portion && quantity.Value == 1m,
            _ => false
        };
    }

    private static FoodUnit? GetUnitForNutritionBasis(NutritionBasis basis) {
        return basis switch {
            NutritionBasis.Per100Grams => FoodUnit.Gram,
            NutritionBasis.Per100Milliliters => FoodUnit.Milliliter,
            NutritionBasis.PerItem => FoodUnit.Piece,
            _ => null
        };
    }

    private static bool HasIncompleteNutrition(NutritionFacts nutrition) {
        return nutrition.CaloriesKcal is null
            || nutrition.ProteinG is null
            || nutrition.FatG is null
            || nutrition.CarbsG is null;
    }

    private static bool HasInvalidNutrition(NutritionFacts nutrition) {
        return nutrition.CaloriesKcal < 0m
            || nutrition.ProteinG < 0m
            || nutrition.FatG < 0m
            || nutrition.CarbsG < 0m;
    }

    private static void AddUnique(
        ICollection<CookingSessionValidationError> errors,
        CookingSessionValidationError error
    ) {
        if(!errors.Contains(error)) {
            errors.Add(error);
        }
    }

    private static string? NormalizeOptionalText(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
