using CalorieLedger.Application.Fridge;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Fridge;
using System;
using System.Collections.Generic;

namespace CalorieLedger.Application.Cooking;

public sealed class CookingExecutionService {
    private readonly ICookingSessionStore cookingSessionStore;
    private readonly ICookingBatchStore cookingBatchStore;
    private readonly IFridgeStore fridgeStore;

    public CookingExecutionService(
        ICookingSessionStore cookingSessionStore,
        ICookingBatchStore cookingBatchStore,
        IFridgeStore fridgeStore
    ) {
        ArgumentNullException.ThrowIfNull(cookingSessionStore);
        ArgumentNullException.ThrowIfNull(cookingBatchStore);
        ArgumentNullException.ThrowIfNull(fridgeStore);

        this.cookingSessionStore = cookingSessionStore;

        this.cookingBatchStore = cookingBatchStore;

        this.fridgeStore = fridgeStore;
    }

    public bool HasCompletedSession(Guid sessionId) {
        return cookingBatchStore.GetBySessionId(sessionId) is not null;
    }

    public CookingExecutionResult Execute(
        Guid sessionId,
        DateOnly cookedDate
    ) {
        if(HasCompletedSession(sessionId)) {
            return Failure(CookingExecutionError.AlreadyCompleted);
        }

        var session = cookingSessionStore.Get(sessionId);

        if(session is null) {
            return Failure(CookingExecutionError.MissingSession);
        }

        CookingNutritionResult nutrition;

        try {
            nutrition = CookingNutritionCalculator.Calculate(session);
        }
        catch(ArgumentException) {
            return Failure(CookingExecutionError.InvalidSession);
        }

        var preparation = PrepareFridgeChanges(session);

        if(!preparation.IsSuccess) {
            return new CookingExecutionResult(
                IsSuccess: false,
                Batch: null,
                Errors: preparation.Errors
            );
        }

        var outputFridgeItemId = Guid.NewGuid();

        var outputFridgeItem = new FridgeItem(
            Id: outputFridgeItemId,
            Name: session.Name,
            Quantity: FoodQuantity.Grams(session.OutputWeightG),
            Nutrition: nutrition.NutritionPer100Grams,
            Note: session.Note,
            Source: FridgeItemSource.CookingSession,
            SourceId: session.Id
        );

        var batch = new CookingBatch(
            Id: Guid.NewGuid(),
            SessionId: session.Id,
            Name: session.Name,
            Ingredients: [
                .. session.Ingredients,
            ],
            OutputWeightG: session.OutputWeightG,
            Nutrition: nutrition,
            CookedDate: cookedDate,
            OutputFridgeItemId: outputFridgeItemId,
            Note: session.Note
        );

        cookingBatchStore.Save(batch);

        try {
            fridgeStore.SaveMany(
                [
                    .. preparation.UpdatedItems,
                    outputFridgeItem,
                ]
            );
        }
        catch {
            cookingBatchStore.Delete(batch.Id);

            throw;
        }

        return new CookingExecutionResult(
            IsSuccess: true,
            Batch: batch,
            Errors: []
        );
    }

    private FridgePreparationResult PrepareFridgeChanges(CookingSessionDraft session) {
        var consumptions = new Dictionary<Guid, FoodQuantity>();

        foreach(var ingredient in session.Ingredients) {
            if(ingredient.Source != CookingIngredientSource.FridgeItem) {
                continue;
            }

            if(ingredient.SourceId is not Guid fridgeItemId) {
                return FridgePreparationResult.Failure(CookingExecutionError.MissingFridgeSource);
            }

            if(ingredient.Quantity.Value <= 0m) {
                return FridgePreparationResult.Failure(
                    CookingExecutionError.InvalidSession);
            }

            if(consumptions.TryGetValue(
                    fridgeItemId,
                    out var existingQuantity
                )
            ) {
                if(existingQuantity.Unit != ingredient.Quantity.Unit) {
                    return FridgePreparationResult.Failure(
                        CookingExecutionError.IncompatibleFridgeQuantity
                    );
                }

                consumptions[fridgeItemId] = new FoodQuantity(
                    existingQuantity.Value + ingredient.Quantity.Value,
                    existingQuantity.Unit
                );
            }
            else {
                consumptions.Add(
                    fridgeItemId,
                    ingredient.Quantity
                );
            }
        }

        var updatedItems = new List<FridgeItem>();

        foreach(var consumption in consumptions) {
            var fridgeItem = fridgeStore.Get(consumption.Key);

            if(fridgeItem is null) {
                return FridgePreparationResult.Failure(CookingExecutionError.MissingFridgeItem);
            }

            if(fridgeItem.Quantity.Unit != consumption.Value.Unit) {
                return FridgePreparationResult.Failure(CookingExecutionError.IncompatibleFridgeQuantity);
            }

            if(consumption.Value.Value > fridgeItem.Quantity.Value) {
                return FridgePreparationResult.Failure(CookingExecutionError.InsufficientFridgeQuantity);
            }

            updatedItems.Add(
                fridgeItem with {
                    Quantity = new FoodQuantity(
                        fridgeItem.Quantity.Value - consumption.Value.Value,
                        fridgeItem.Quantity.Unit
                    ),
                }
            );
        }

        return FridgePreparationResult.Success(updatedItems);
    }

    private static CookingExecutionResult Failure(CookingExecutionError error) {
        return new CookingExecutionResult(
            IsSuccess: false,
            Batch: null,
            Errors: [error]
        );
    }

    private sealed record FridgePreparationResult(
        bool IsSuccess,
        IReadOnlyList<FridgeItem> UpdatedItems,
        IReadOnlyList<CookingExecutionError> Errors
    ) {
        public static FridgePreparationResult Success(IReadOnlyList<FridgeItem> updatedItems) {
            return new FridgePreparationResult(
                IsSuccess: true,
                UpdatedItems: updatedItems,
                Errors: []
            );
        }

        public static FridgePreparationResult Failure(CookingExecutionError error) {
            return new FridgePreparationResult(
                IsSuccess: false,
                UpdatedItems: [],
                Errors: [error]
            );
        }
    }
}
