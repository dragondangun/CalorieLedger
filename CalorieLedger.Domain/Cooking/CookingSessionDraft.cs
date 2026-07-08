namespace CalorieLedger.Domain.Cooking;

public sealed record CookingSessionDraft(
    Guid Id,
    string Name,
    IReadOnlyList<CookingIngredient> Ingredients,
    decimal OutputWeightG,
    string? Note = null);