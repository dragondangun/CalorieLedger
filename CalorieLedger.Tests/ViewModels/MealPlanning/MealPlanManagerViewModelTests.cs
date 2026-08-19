using CalorieLedger.Application.MealPlanning;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.ViewModels.MealPlanning;

namespace CalorieLedger.Tests.ViewModels.MealPlanning;

public sealed class MealPlanManagerViewModelTests {
    [Fact]
    public void Constructor_LoadsSelectedDayAndFuturePlannedDays() {
        var currentDate = new DateOnly(2026, 8, 19);
        var service = CreateService(
            CreateDay(currentDate.AddDays(-1), "Прошлый"),
            CreateDay(currentDate, "Завтрак"),
            CreateDay(currentDate.AddDays(2), "Обед")
        );

        var viewModel = new MealPlanManagerViewModel(
            service,
            currentDate,
            () => { }
        );

        Assert.Equal(currentDate, viewModel.SelectedDate);
        Assert.True(viewModel.HasPlan);
        Assert.False(viewModel.HasNoPlan);
        Assert.Single(viewModel.Meals);
        Assert.Equal("Завтрак", viewModel.Meals[0].Name);
        Assert.Equal(2, viewModel.PlannedDays.Count);
        Assert.DoesNotContain(
            viewModel.PlannedDays,
            day => day.Date < currentDate
        );
        Assert.Contains("100 ккал", viewModel.NutritionSummary);
    }

    [Fact]
    public void DailyNutritionSummary_DoesNotTreatUnknownNutritionAsZero() {
        var currentDate = new DateOnly(2026, 8, 19);
        var knownItem = CreateDay(currentDate, "Завтрак").Meals[0].Items[0];
        var day = new MealPlanDay(
            currentDate,
            [
                new MealPlanMeal(
                    "Завтрак",
                    MealGroupRole.Breakfast,
                    null,
                    [
                        knownItem,
                        new MealPlanItem(
                            "Неизвестный продукт",
                            FoodQuantity.Grams(50m),
                            null,
                            NutritionTotals.Empty
                        ),
                    ]
                ),
            ]
        );
        var service = CreateService(day);

        var viewModel = new MealPlanManagerViewModel(service, currentDate, () => { });

        Assert.Contains("КБЖУ плана: ?", viewModel.NutritionSummary);
        Assert.Contains("Б ?", viewModel.NutritionSummary);
        Assert.Contains("Ж ?", viewModel.NutritionSummary);
        Assert.Contains("У ?", viewModel.NutritionSummary);
    }

    [Fact]
    public void DayNavigation_DoesNotGoEarlierThanCurrentDate() {
        var currentDate = new DateOnly(2026, 8, 19);
        var viewModel = new MealPlanManagerViewModel(
            new MealPlanService(new InMemoryMealPlanStore()),
            currentDate,
            () => { }
        );

        viewModel.PreviousDayCommand.Execute(null);
        Assert.Equal(currentDate, viewModel.SelectedDate);

        viewModel.NextDayCommand.Execute(null);
        Assert.Equal(currentDate.AddDays(1), viewModel.SelectedDate);
        Assert.True(viewModel.CanGoToPreviousDay);

        viewModel.PreviousDayCommand.Execute(null);
        Assert.Equal(currentDate, viewModel.SelectedDate);
        Assert.False(viewModel.CanGoToPreviousDay);
    }

    [Fact]
    public void PlannedDayLink_SelectsSavedFutureDay() {
        var currentDate = new DateOnly(2026, 8, 19);
        var futureDate = currentDate.AddDays(4);
        var service = CreateService(CreateDay(futureDate, "Ужин"));
        var viewModel = new MealPlanManagerViewModel(service, currentDate, () => { });

        var futureDay = Assert.Single(viewModel.PlannedDays);
        futureDay.SelectCommand.Execute(null);

        Assert.Equal(futureDate, viewModel.SelectedDate);
        Assert.True(viewModel.HasPlan);
        Assert.Equal("Ужин", Assert.Single(viewModel.Meals).Name);
        Assert.True(Assert.Single(viewModel.PlannedDays).IsSelected);
    }

    [Fact]
    public void DeleteSelectedDay_RequiresConfirmationAndRemovesOnlySelectedDate() {
        var currentDate = new DateOnly(2026, 8, 19);
        var nextDate = currentDate.AddDays(1);
        var service = CreateService(
            CreateDay(currentDate, "Сегодня"),
            CreateDay(nextDate, "Завтра")
        );
        var viewModel = new MealPlanManagerViewModel(service, currentDate, () => { });

        viewModel.RequestDeleteSelectedDayCommand.Execute(null);
        Assert.True(viewModel.IsDeleteConfirmationVisible);
        Assert.Equal(2, service.GetAll().Count);

        viewModel.ConfirmDeleteSelectedDayCommand.Execute(null);

        Assert.False(viewModel.IsDeleteConfirmationVisible);
        Assert.False(viewModel.HasPlan);
        Assert.Empty(viewModel.Meals);
        var remaining = Assert.Single(service.GetAll());
        Assert.Equal(nextDate, remaining.Date);
    }

    [Fact]
    public void Close_InvokesCallback() {
        var closed = false;
        var viewModel = new MealPlanManagerViewModel(
            new MealPlanService(new InMemoryMealPlanStore()),
            new DateOnly(2026, 8, 19),
            () => closed = true
        );

        viewModel.CloseCommand.Execute(null);

        Assert.True(closed);
    }

    private static MealPlanService CreateService(params MealPlanDay[] days) {
        var service = new MealPlanService(new InMemoryMealPlanStore());
        service.Save(new MealPlan(days));
        return service;
    }

    private static MealPlanDay CreateDay(DateOnly date, string mealName) {
        return new MealPlanDay(
            Date: date,
            Meals: [
                new MealPlanMeal(
                    Name: mealName,
                    Role: MealGroupRole.Custom,
                    Time: new TimeOnly(12, 30),
                    Items: [
                        new MealPlanItem(
                            Name: "Продукт",
                            Quantity: FoodQuantity.Grams(100m),
                            FridgeItemId: null,
                            Nutrition: new NutritionTotals(
                                CaloriesKcal: 100m,
                                ProteinG: 10m,
                                FatG: 5m,
                                CarbsG: 7m
                            )
                        ),
                    ]
                ),
            ]
        );
    }
}
