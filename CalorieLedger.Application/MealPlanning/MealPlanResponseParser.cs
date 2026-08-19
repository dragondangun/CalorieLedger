using System.Text.Encodings.Web;
using System.Text.Json;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;

namespace CalorieLedger.Application.MealPlanning;

public sealed class MealPlanResponseParser {
    public const string Protocol = "calorieledger.meal_plan.v1";

    private static readonly JsonSerializerOptions SerializerOptions = new() {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        WriteIndented = true
    };

    public MealPlanParseResult Parse(string? text) {
        if(string.IsNullOrWhiteSpace(text)) {
            return Failure(
                new MealPlanParseError(
                    MealPlanParseErrorCode.InvalidJson,
                    "$"
                )
            );
        }

        ResponseDocumentDto? document;

        try {
            document = JsonSerializer.Deserialize<ResponseDocumentDto>(
                text,
                SerializerOptions
            );
        }
        catch(JsonException) {
            return Failure(
                new MealPlanParseError(
                    MealPlanParseErrorCode.InvalidJson,
                    "$"
                )
            );
        }

        if(document is null) {
            return Failure(
                new MealPlanParseError(
                    MealPlanParseErrorCode.InvalidJson,
                    "$"
                )
            );
        }

        var errors = new List<MealPlanParseError>();

        if(!string.Equals(document.Protocol, Protocol, StringComparison.Ordinal)) {
            errors.Add(
                new MealPlanParseError(
                    MealPlanParseErrorCode.UnsupportedProtocol,
                    "protocol"
                )
            );
        }

        if(document.Days is not { Count: > 0 }) {
            errors.Add(
                new MealPlanParseError(
                    MealPlanParseErrorCode.MissingDays,
                    "days"
                )
            );

            return Failure(errors);
        }

        var parsedDays = new List<MealPlanDay>();
        var usedDates = new HashSet<DateOnly>();

        for(var dayIndex = 0; dayIndex < document.Days.Count; dayIndex++) {
            var day = document.Days[dayIndex];
            var dayPath = $"days[{dayIndex}]";

            if(day.Date == default) {
                errors.Add(
                    new MealPlanParseError(
                        MealPlanParseErrorCode.MissingDate,
                        $"{dayPath}.date"
                    )
                );
            }
            else if(!usedDates.Add(day.Date)) {
                errors.Add(
                    new MealPlanParseError(
                        MealPlanParseErrorCode.DuplicateDate,
                        $"{dayPath}.date"
                    )
                );
            }

            if(day.Meals is not { Count: > 0 }) {
                errors.Add(
                    new MealPlanParseError(
                        MealPlanParseErrorCode.MissingMeals,
                        $"{dayPath}.meals"
                    )
                );

                continue;
            }

            var parsedMeals = new List<MealPlanMeal>();

            for(var mealIndex = 0; mealIndex < day.Meals.Count; mealIndex++) {
                var meal = day.Meals[mealIndex];
                var mealPath = $"{dayPath}.meals[{mealIndex}]";

                if(string.IsNullOrWhiteSpace(meal.Name)) {
                    errors.Add(
                        new MealPlanParseError(
                            MealPlanParseErrorCode.MissingMealName,
                            $"{mealPath}.name"
                        )
                    );
                }

                if(!TryParseMealRole(meal.Role, out var role)) {
                    errors.Add(
                        new MealPlanParseError(
                            MealPlanParseErrorCode.UnsupportedMealRole,
                            $"{mealPath}.role"
                        )
                    );
                }

                if(meal.Items is not { Count: > 0 }) {
                    errors.Add(
                        new MealPlanParseError(
                            MealPlanParseErrorCode.MissingItems,
                            $"{mealPath}.items"
                        )
                    );

                    continue;
                }

                var parsedItems = new List<MealPlanItem>();

                for(var itemIndex = 0; itemIndex < meal.Items.Count; itemIndex++) {
                    var item = meal.Items[itemIndex];
                    var itemPath = $"{mealPath}.items[{itemIndex}]";
                    var errorCountBeforeItem = errors.Count;

                    if(string.IsNullOrWhiteSpace(item.Name)) {
                        errors.Add(
                            new MealPlanParseError(
                                MealPlanParseErrorCode.MissingItemName,
                                $"{itemPath}.name"
                            )
                        );
                    }

                    FoodUnit unit = default;

                    if(item.Quantity is null || item.Quantity.Value is not > 0m) {
                        errors.Add(
                            new MealPlanParseError(
                                MealPlanParseErrorCode.InvalidQuantity,
                                $"{itemPath}.quantity"
                            )
                        );
                    }
                    else if(!TryParseUnit(item.Quantity.Unit, out unit)) {
                        errors.Add(
                            new MealPlanParseError(
                                MealPlanParseErrorCode.UnsupportedQuantityUnit,
                                $"{itemPath}.quantity.unit"
                            )
                        );
                    }

                    if(!IsNutritionValid(item.Nutrition)) {
                        errors.Add(
                            new MealPlanParseError(
                                MealPlanParseErrorCode.InvalidNutrition,
                                $"{itemPath}.nutrition"
                            )
                        );
                    }

                    if(errors.Count != errorCountBeforeItem) {
                        continue;
                    }

                    parsedItems.Add(
                        new MealPlanItem(
                            Name: item.Name!.Trim(),
                            Quantity: new FoodQuantity(
                                item.Quantity!.Value!.Value,
                                unit
                            ),
                            FridgeItemId: item.FridgeItemId,
                            Nutrition: CreateNutrition(item.Nutrition),
                            Note: NormalizeOptionalText(item.Note)
                        )
                    );
                }

                if(string.IsNullOrWhiteSpace(meal.Name)
                    || !TryParseMealRole(meal.Role, out role)
                    || parsedItems.Count != meal.Items.Count
                ) {
                    continue;
                }

                parsedMeals.Add(
                    new MealPlanMeal(
                        Name: meal.Name.Trim(),
                        Role: role,
                        Time: meal.Time,
                        Items: parsedItems,
                        Note: NormalizeOptionalText(meal.Note)
                    )
                );
            }

            if(day.Date == default
                || parsedMeals.Count != day.Meals.Count
            ) {
                continue;
            }

            parsedDays.Add(
                new MealPlanDay(
                    Date: day.Date,
                    Meals: parsedMeals
                )
            );
        }

        if(errors.Count > 0) {
            return Failure(errors);
        }

        return new MealPlanParseResult(
            IsSuccess: true,
            Plan: new MealPlan(
                Days: [
                    .. parsedDays.OrderBy(day => day.Date),
                ]
            ),
            Errors: []
        );
    }

    public string CreateResponseInstructions(DateOnly firstDate) {
        var example = new ResponseDocumentDto(
            Protocol: Protocol,
            Days: [
                new ResponseDayDto(
                    Date: firstDate,
                    Meals: [
                        new ResponseMealDto(
                            Name: "Завтрак",
                            Role: "breakfast",
                            Time: new TimeOnly(8, 0),
                            Items: [
                                new ResponseItemDto(
                                    Name: "Пример продукта",
                                    Quantity: new ResponseQuantityDto(
                                        Value: 200m,
                                        Unit: "g"
                                    ),
                                    FridgeItemId: null,
                                    Nutrition: new ResponseNutritionDto(
                                        CaloriesKcal: 250m,
                                        ProteinG: 15m,
                                        FatG: 8m,
                                        CarbsG: 30m
                                    ),
                                    Note: null
                                ),
                            ],
                            Note: null
                        ),
                    ]
                ),
                new ResponseDayDto(
                    Date: firstDate.AddDays(1),
                    Meals: [
                        new ResponseMealDto(
                            Name: "Поздний завтрак",
                            Role: "custom",
                            Time: new TimeOnly(11, 0),
                            Items: [
                                new ResponseItemDto(
                                    Name: "Другой пример",
                                    Quantity: new ResponseQuantityDto(
                                        Value: 1m,
                                        Unit: "portion"
                                    ),
                                    FridgeItemId: null,
                                    Nutrition: null,
                                    Note: "КБЖУ можно оставить неизвестными"
                                ),
                            ],
                            Note: "Каждый день может иметь собственный режим питания"
                        ),
                    ]
                ),
            ]
        );

        var json = JsonSerializer.Serialize(
            example,
            SerializerOptions
        );

        return $"""
Верни только JSON без Markdown и пояснений.
Протокол ответа: {Protocol}.
Поле days содержит отдельный объект для каждой даты, поэтому число и время приёмов пищи могут различаться по дням, включая будни и выходные.
Допустимые role: breakfast, lunch, dinner, snack, custom.
Допустимые unit: g, ml, piece, portion.
fridgeItemId указывай только при использовании конкретной позиции из переданного холодильника; иначе null.
nutrition — итоговые КБЖУ именно для указанного количества; все поля могут быть null, если оценка неизвестна.

Пример корректного ответа:
{json}
""";
    }

    private static MealPlanParseResult Failure(params MealPlanParseError[] errors) {
        return Failure((IReadOnlyList<MealPlanParseError>)errors);
    }

    private static MealPlanParseResult Failure(IReadOnlyList<MealPlanParseError> errors) {
        return new MealPlanParseResult(
            IsSuccess: false,
            Plan: null,
            Errors: errors
        );
    }

    private static bool TryParseMealRole(
        string? value,
        out MealGroupRole role
    ) {
        role = value switch {
            "breakfast" => MealGroupRole.Breakfast,
            "lunch" => MealGroupRole.Lunch,
            "dinner" => MealGroupRole.Dinner,
            "snack" => MealGroupRole.Snack,
            "custom" => MealGroupRole.Custom,
            _ => default
        };

        return role != default;
    }

    private static bool TryParseUnit(
        string? value,
        out FoodUnit unit
    ) {
        unit = value switch {
            "g" => FoodUnit.Gram,
            "ml" => FoodUnit.Milliliter,
            "piece" => FoodUnit.Piece,
            "portion" => FoodUnit.Portion,
            _ => default
        };

        return unit != default;
    }

    private static bool IsNutritionValid(ResponseNutritionDto? nutrition) {
        if(nutrition is null) {
            return true;
        }

        return IsNonNegative(nutrition.CaloriesKcal)
            && IsNonNegative(nutrition.ProteinG)
            && IsNonNegative(nutrition.FatG)
            && IsNonNegative(nutrition.CarbsG);
    }

    private static bool IsNonNegative(decimal? value) {
        return value is null or >= 0m;
    }

    private static NutritionTotals CreateNutrition(ResponseNutritionDto? nutrition) {
        return nutrition is null
            ? NutritionTotals.Empty
            : new NutritionTotals(
                CaloriesKcal: nutrition.CaloriesKcal,
                ProteinG: nutrition.ProteinG,
                FatG: nutrition.FatG,
                CarbsG: nutrition.CarbsG
            );
    }

    private static string? NormalizeOptionalText(string? value) {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private sealed record ResponseDocumentDto(
        string? Protocol,
        IReadOnlyList<ResponseDayDto>? Days
    );

    private sealed record ResponseDayDto(
        DateOnly Date,
        IReadOnlyList<ResponseMealDto>? Meals
    );

    private sealed record ResponseMealDto(
        string? Name,
        string? Role,
        TimeOnly? Time,
        IReadOnlyList<ResponseItemDto>? Items,
        string? Note
    );

    private sealed record ResponseItemDto(
        string? Name,
        ResponseQuantityDto? Quantity,
        Guid? FridgeItemId,
        ResponseNutritionDto? Nutrition,
        string? Note
    );

    private sealed record ResponseQuantityDto(
        decimal? Value,
        string? Unit
    );

    private sealed record ResponseNutritionDto(
        decimal? CaloriesKcal,
        decimal? ProteinG,
        decimal? FatG,
        decimal? CarbsG
    );
}
