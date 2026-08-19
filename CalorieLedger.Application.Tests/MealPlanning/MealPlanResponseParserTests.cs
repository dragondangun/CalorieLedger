using CalorieLedger.Application.MealPlanning;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;

namespace CalorieLedger.Application.Tests.MealPlanning;

public sealed class MealPlanResponseParserTests {
    [Fact]
    public void Parse_ValidMultiDayPlan_PreservesIndependentDaySchedules() {
        var parser = new MealPlanResponseParser();
        var fridgeItemId = Guid.NewGuid();

        var result = parser.Parse(
            $$"""
            {
              "protocol": "calorieledger.meal_plan.v1",
              "days": [
                {
                  "date": "2026-08-22",
                  "meals": [
                    {
                      "name": "Поздний завтрак",
                      "role": "custom",
                      "time": "11:00:00",
                      "items": [
                        {
                          "name": "Творог",
                          "quantity": {
                            "value": 200,
                            "unit": "g"
                          },
                          "fridgeItemId": "{{fridgeItemId}}",
                          "nutrition": {
                            "caloriesKcal": 242,
                            "proteinG": 34,
                            "fatG": 10,
                            "carbsG": 6
                          },
                          "note": null
                        }
                      ],
                      "note": "выходной"
                    }
                  ]
                },
                {
                  "date": "2026-08-21",
                  "meals": [
                    {
                      "name": "Завтрак",
                      "role": "breakfast",
                      "time": "08:00:00",
                      "items": [
                        {
                          "name": "Каша",
                          "quantity": {
                            "value": 1,
                            "unit": "portion"
                          },
                          "fridgeItemId": null,
                          "nutrition": null,
                          "note": null
                        }
                      ],
                      "note": null
                    },
                    {
                      "name": "Обед",
                      "role": "lunch",
                      "time": "13:00:00",
                      "items": [
                        {
                          "name": "Суп",
                          "quantity": {
                            "value": 350,
                            "unit": "ml"
                          },
                          "fridgeItemId": null,
                          "nutrition": {
                            "caloriesKcal": 280,
                            "proteinG": null,
                            "fatG": null,
                            "carbsG": null
                          },
                          "note": null
                        }
                      ],
                      "note": null
                    }
                  ]
                }
              ]
            }
            """
        );

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Plan);
        Assert.Equal(2, result.Plan.Days.Count);

        var weekday = result.Plan.Days[0];
        Assert.Equal(new DateOnly(2026, 8, 21), weekday.Date);
        Assert.Equal(2, weekday.Meals.Count);
        Assert.Equal(MealGroupRole.Breakfast, weekday.Meals[0].Role);
        Assert.Equal(new TimeOnly(8, 0), weekday.Meals[0].Time);
        Assert.Equal(FoodUnit.Portion, weekday.Meals[0].Items[0].Quantity.Unit);

        var weekend = result.Plan.Days[1];
        var weekendMeal = Assert.Single(weekend.Meals);
        var weekendItem = Assert.Single(weekendMeal.Items);

        Assert.Equal(new DateOnly(2026, 8, 22), weekend.Date);
        Assert.Equal(MealGroupRole.Custom, weekendMeal.Role);
        Assert.Equal(new TimeOnly(11, 0), weekendMeal.Time);
        Assert.Equal(fridgeItemId, weekendItem.FridgeItemId);
        Assert.Equal(200m, weekendItem.Quantity.Value);
        Assert.Equal(FoodUnit.Gram, weekendItem.Quantity.Unit);
        Assert.Equal(242m, weekendItem.Nutrition.CaloriesKcal);
    }

    [Fact]
    public void Parse_UnsupportedProtocol_ReturnsValidationError() {
        var parser = new MealPlanResponseParser();

        var result = parser.Parse(
            """
            {
              "protocol": "some.other.protocol",
              "days": [
                {
                  "date": "2026-08-21",
                  "meals": [
                    {
                      "name": "Завтрак",
                      "role": "breakfast",
                      "time": null,
                      "items": [
                        {
                          "name": "Продукт",
                          "quantity": { "value": 1, "unit": "piece" },
                          "fridgeItemId": null,
                          "nutrition": null,
                          "note": null
                        }
                      ],
                      "note": null
                    }
                  ]
                }
              ]
            }
            """
        );

        Assert.False(result.IsSuccess);
        Assert.Null(result.Plan);

        var error = Assert.Single(result.Errors);
        Assert.Equal(MealPlanParseErrorCode.UnsupportedProtocol, error.Code);
        Assert.Equal("protocol", error.Path);
    }

    [Fact]
    public void Parse_DuplicateDateAndInvalidItem_ReturnsAllRelevantErrors() {
        var parser = new MealPlanResponseParser();

        var result = parser.Parse(
            """
            {
              "protocol": "calorieledger.meal_plan.v1",
              "days": [
                {
                  "date": "2026-08-21",
                  "meals": [
                    {
                      "name": "Завтрак",
                      "role": "breakfast",
                      "time": null,
                      "items": [
                        {
                          "name": "Продукт",
                          "quantity": { "value": 0, "unit": "kg" },
                          "fridgeItemId": null,
                          "nutrition": {
                            "caloriesKcal": -1,
                            "proteinG": null,
                            "fatG": null,
                            "carbsG": null
                          },
                          "note": null
                        }
                      ],
                      "note": null
                    }
                  ]
                },
                {
                  "date": "2026-08-21",
                  "meals": [
                    {
                      "name": "Ужин",
                      "role": "dinner",
                      "time": null,
                      "items": [
                        {
                          "name": "Продукт",
                          "quantity": { "value": 1, "unit": "piece" },
                          "fridgeItemId": null,
                          "nutrition": null,
                          "note": null
                        }
                      ],
                      "note": null
                    }
                  ]
                }
              ]
            }
            """
        );

        Assert.False(result.IsSuccess);
        Assert.Null(result.Plan);
        Assert.Contains(
            result.Errors,
            error => error.Code == MealPlanParseErrorCode.InvalidQuantity
        );
        Assert.Contains(
            result.Errors,
            error => error.Code == MealPlanParseErrorCode.InvalidNutrition
        );
        Assert.Contains(
            result.Errors,
            error => error.Code == MealPlanParseErrorCode.DuplicateDate
        );
    }

    [Fact]
    public void CreateResponseInstructions_ContainsExampleAcceptedByParser() {
        var parser = new MealPlanResponseParser();
        var instructions = parser.CreateResponseInstructions(new DateOnly(2026, 8, 19));
        var jsonStart = instructions.IndexOf('{');

        Assert.True(jsonStart >= 0);
        Assert.Contains(MealPlanResponseParser.Protocol, instructions);
        Assert.Contains("будни и выходные", instructions);

        var result = parser.Parse(instructions[jsonStart..]);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Plan);
        Assert.Equal(2, result.Plan.Days.Count);
    }
}
