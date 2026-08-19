using CalorieLedger.Application.Activities;
using CalorieLedger.Application.History;
using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Activities;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.History;
using CalorieLedger.ViewModels.Meals;

namespace CalorieLedger.Tests.ViewModels.Meals;

public sealed class DailyJournalHistoryViewModelTests {
    [Fact]
    public void Constructor_CurrentWeek_BuildsSevenDaysAndDisablesFutureDays() {
        var currentDate = new DateOnly(2026, 8, 19);

        var viewModel = CreateViewModel(
            currentDate,
            new InMemoryFoodDiaryStore()
        );

        Assert.Equal(
            7,
            viewModel.WeekDays.Count
        );

        Assert.Equal(
            new DateOnly(2026, 8, 17),
            viewModel.WeekDays[0].Date
        );

        Assert.Equal(
            new DateOnly(2026, 8, 23),
            viewModel.WeekDays[^1].Date
        );

        Assert.Equal(
            3,
            viewModel.WeekDays.Count(day => day.IsAvailable)
        );

        Assert.Equal(
            4,
            viewModel.WeekDays.Count(day => !day.IsAvailable)
        );

        var today = Assert.Single(viewModel.WeekDays, day => day.IsToday);

        Assert.True(today.IsSelected);

        Assert.False(viewModel.NextWeekCommand.CanExecute(null));
    }

    [Fact]
    public void SelectWeekDay_LoadsSelectedDayFood() {
        var currentDate = new DateOnly(2026, 8, 19);

        var selectedDate = new DateOnly(2026, 8, 18);

        var store = new InMemoryFoodDiaryStore();

        var meal = CreateMeal(selectedDate);

        store.SaveMeal(meal);

        store.SaveFoodEntry(
            CreateFood(
                meal.Id,
                caloriesKcal: 600m
            )
        );

        store.SetDateComplete(selectedDate, true);

        var viewModel = CreateViewModel(currentDate, store);

        var day = Assert.Single(viewModel.WeekDays, item => item.Date == selectedDate);

        day.SelectCommand.Execute(null);

        Assert.Equal(
            selectedDate,
            viewModel.SelectedDate
        );

        Assert.Equal(
            600m,
            viewModel.ConsumedCaloriesKcal
        );

        var selectedDay = Assert.Single(viewModel.WeekDays, item => item.IsSelected);

        Assert.Equal(
            selectedDate,
            selectedDay.Date
        );

        Assert.True(selectedDay.IsEnergyComplete);

        Assert.True(selectedDay.AreMacrosComplete);

        Assert.Equal(
            "данные полны",
            selectedDay.StatusSummary
        );
    }

    [Fact]
    public void PreviousAndNextWeek_PreserveWeekdayWithoutEnteringFuture() {
        var currentDate = new DateOnly(2026, 8, 19);

        var viewModel = CreateViewModel(
            currentDate,
            new InMemoryFoodDiaryStore()
        );

        viewModel.PreviousWeekCommand.Execute(null);

        Assert.Equal(
            new DateOnly(2026, 8, 12),
            viewModel.SelectedDate
        );

        Assert.All(
            viewModel.WeekDays,
            day => Assert.True(day.IsAvailable)
        );

        Assert.True(viewModel.NextWeekCommand.CanExecute(null));

        viewModel.NextWeekCommand.Execute(null);

        Assert.Equal(
            currentDate,
            viewModel.SelectedDate
        );

        Assert.False(viewModel.NextWeekCommand.CanExecute(null));
    }

    [Fact]
    public void Refresh_AfterDiaryChange_UpdatesSelectedWeekDay() {
        var currentDate = new DateOnly(2026, 8, 19);

        var selectedDate = new DateOnly(2026, 8, 18);

        var store = new InMemoryFoodDiaryStore();

        var viewModel = CreateViewModel(
            currentDate,
            store
        );

        viewModel.WeekDays
            .Single(day => day.Date == selectedDate)
            .SelectCommand
            .Execute(null);

        var meal = CreateMeal(selectedDate);

        store.SaveMeal(meal);

        store.SaveFoodEntry(
            CreateFood(
                meal.Id,
                caloriesKcal: 750m
            )
        );

        store.SetDateComplete(
            selectedDate,
            true
        );

        viewModel.Refresh();

        var day = Assert.Single(viewModel.WeekDays, item => item.Date == selectedDate);

        Assert.True(day.IsSelected);

        Assert.True(day.IsEnergyComplete);

        Assert.Equal(
            "750 ккал",
            day.CaloriesSummary
        );

        Assert.Equal(
            750m,
            viewModel.ConsumedCaloriesKcal
        );
    }

    [Fact]
    public void SelectDay_LoadsActivityAndAdjustedCalories() {
        var currentDate = new DateOnly(2026, 8, 19);
        var selectedDate = currentDate.AddDays(-1);

        var foodStore = new InMemoryFoodDiaryStore();
        var activityStore = new InMemoryActivityStore();

        activityStore.Save(new ActivityEntry(
            Id: Guid.NewGuid(),
            Date: selectedDate,
            Name: "HEMA",
            BurnedCaloriesKcal: 350m,
            Duration: TimeSpan.FromMinutes(75))
        );

        var viewModel = CreateViewModel(currentDate, foodStore, activityStore);

        Assert.Single(viewModel.WeekDays, x => x.Date == selectedDate).SelectCommand.Execute(null);

        Assert.Equal(selectedDate, viewModel.SelectedDate);
        Assert.Equal(350m, viewModel.ExtraActivityBurnedCaloriesKcal);
        Assert.Equal(-350m, viewModel.ActivityAdjustedCaloriesKcal);

        var activity = Assert.Single(viewModel.Activities);
        Assert.Equal("HEMA", activity.Name);
        Assert.Equal("1,3 ч", activity.DurationSummary);

        var weekDay = Assert.Single(viewModel.WeekDays, x => x.Date == selectedDate);
        Assert.Equal("+350 акт.", weekDay.ActivitySummary);
    }

    [Fact]
    public void PreviousWeek_RefreshesWeeklySummaryForSelectedWeek() {
        var currentDate = new DateOnly(2026, 8, 19);
        var previousWeekDate = currentDate.AddDays(-7);

        var foodStore = new InMemoryFoodDiaryStore();
        var bodyStore = new InMemoryBodyMeasurementStore();

        var meal = new MealEntry(
            Id: Guid.NewGuid(),
            Date: previousWeekDate,
            Name: "Другое",
            Role: MealGroupRole.Custom
        );

        foodStore.SaveMeal(meal);

        foodStore.SaveFoodEntry(
            new FoodLogEntry(
                Id: Guid.NewGuid(),
                MealEntryId: meal.Id,
                Name: "Еда",
                Quantity: FoodQuantity.Portions(1m),
                Nutrition: new NutritionFacts(
                    Basis: NutritionBasis.Total,
                    CaloriesKcal: 1800m,
                    ProteinG: 90m,
                    FatG: 60m,
                    CarbsG: 200m
                ),
                Source: FoodLogSource.Manual
            )
        );

        foodStore.SetDateComplete(previousWeekDate, true);

        bodyStore.Save(
            new BodyMeasurementEntry(
                Id: Guid.NewGuid(),
                Date: previousWeekDate,
                WeightKg: 60m
            )
        );

        var viewModel = CreateViewModel(
            currentDate,
            foodStore,
            bodyMeasurementStore: bodyStore
        );

        viewModel.PreviousWeekCommand.Execute(null);
        Assert.Equal(8, viewModel.TrendChartPoints.Count);
        Assert.Equal("10.08", viewModel.TrendChartPoints[^1].Label);
        Assert.False(viewModel.TrendChartPoints[^1].IsPartialWeek);

        Assert.Equal(previousWeekDate, viewModel.SelectedDate);
        Assert.Equal(1800m, viewModel.WeeklySummary.AverageFoodCaloriesKcal);
        Assert.Equal(1, viewModel.WeeklySummary.EnergyCompleteDayCount);
        Assert.Equal(1, viewModel.WeeklySummary.WeightMeasurementCount);
    }

    [Fact]
    public void Constructor_BuildsEightWeekTrendEndingWithSelectedWeek() {
        var currentDate = new DateOnly(2026, 8, 19);

        var viewModel = CreateViewModel(
            currentDate,
            new InMemoryFoodDiaryStore()
        );

        Assert.Equal(8, viewModel.RecentWeeks.Count);

        Assert.Equal(
            new DateOnly(2026, 6, 29),
            viewModel.RecentWeeks[0].WeekStartDate
        );

        Assert.Equal(
            new DateOnly(2026, 8, 17),
            viewModel.RecentWeeks[^1].WeekStartDate
        );

        Assert.True(viewModel.RecentWeeks[^1].IsSelectedWeek);
        Assert.False(viewModel.RecentWeeks[0].IsSelectedWeek);
        Assert.Equal(8, viewModel.TrendChartPoints.Count);

        Assert.Equal(
            "29.06",
            viewModel.TrendChartPoints[0].Label
        );

        Assert.Equal(
            "17.08*",
            viewModel.TrendChartPoints[^1].Label
        );

        Assert.False(viewModel.TrendChartPoints[0].IsPartialWeek);
        Assert.True(viewModel.TrendChartPoints[^1].IsPartialWeek);
    }

    [Fact]
    public void PreviousWeek_ShiftsEightWeekTrendWindow() {
        var currentDate = new DateOnly(2026, 8, 19);

        var viewModel = CreateViewModel(
            currentDate,
            new InMemoryFoodDiaryStore()
        );

        viewModel.PreviousWeekCommand.Execute(null);

        Assert.Equal(
            new DateOnly(2026, 8, 10),
            viewModel.RecentWeeks[^1].WeekStartDate
        );

        Assert.True(viewModel.RecentWeeks[^1].IsSelectedWeek);
    }

    [Fact]
    public void TrendWeek_Select_PreservesSelectedDayOfWeek() {
        var currentDate = new DateOnly(2026, 8, 19);
        var viewModel = CreateViewModel(
            currentDate,
            new InMemoryFoodDiaryStore()
        );

        var targetWeek = viewModel.RecentWeeks[^3];
        Assert.Equal(new DateOnly(2026, 8, 3), targetWeek.WeekStartDate);

        targetWeek.SelectCommand.Execute(null);

        Assert.Equal(new DateOnly(2026, 8, 5), viewModel.SelectedDate);
        Assert.Equal(new DateOnly(2026, 8, 3), viewModel.RecentWeeks[^1].WeekStartDate);
        Assert.True(viewModel.RecentWeeks[^1].IsSelectedWeek);
    }

    [Fact]
    public void SelectWeekCommand_RefreshesChartSelection() {
        var currentDate = new DateOnly(2026, 8, 19);
        var viewModel = CreateViewModel(
            currentDate,
            new InMemoryFoodDiaryStore()
        );

        viewModel.SelectWeekCommand.Execute(new DateOnly(2026, 8, 10));

        Assert.Equal(new DateOnly(2026, 8, 12), viewModel.SelectedDate);

        var selectedPoint = Assert.Single(
            viewModel.TrendChartPoints,
            point => point.IsSelectedWeek
        );

        Assert.Equal(new DateOnly(2026, 8, 10), selectedPoint.WeekStartDate);
    }

    [Fact]
    public void SelectDay_LoadsRecurringPlannedActivityOccurrence() {
        var currentDate = new DateOnly(2026, 8, 19);
        var selectedDate = new DateOnly(2026, 8, 18);
        var recurringStore = new InMemoryRecurringPlannedActivityStore();
        var recurringService = new RecurringPlannedActivityService(recurringStore);

        var saveResult = recurringService.Save(
            new RecurringPlannedActivityDraft(
                Id: Guid.NewGuid(),
                StartDate: selectedDate,
                DayOfWeek: selectedDate.DayOfWeek,
                IntervalWeeks: 1,
                Name: "HEMA"
            )
        );

        Assert.True(saveResult.IsSuccess);

        var viewModel = CreateViewModel(
            currentDate,
            new InMemoryFoodDiaryStore(),
            recurringPlannedActivityService: recurringService,
            editRecurringPlannedActivity: _ => { },
            completeRecurringPlannedActivity: (_, _) => { },
            skipRecurringPlannedActivity: (_, _) => { }
        );

        Assert.Empty(viewModel.RecurringPlannedActivities);

        viewModel.WeekDays
            .Single(day => day.Date == selectedDate)
            .SelectCommand
            .Execute(null);

        var occurrence = Assert.Single(viewModel.RecurringPlannedActivities);

        Assert.Equal(selectedDate, occurrence.Date);
        Assert.Equal("HEMA", occurrence.Name);
    }

    [Fact]
    public void ActivityRepeatCommand_PassesSourceActivityId() {
        var currentDate = new DateOnly(2026, 8, 18);
        var activityId = Guid.NewGuid();
        var activityStore = new InMemoryActivityStore();
        Guid? repeatedId = null;

        activityStore.Save(
            new ActivityEntry(
                Id: activityId,
                Date: currentDate,
                Name: "HEMA",
                BurnedCaloriesKcal: 300m
            )
        );

        var viewModel = CreateViewModel(
            currentDate,
            new InMemoryFoodDiaryStore(),
            activityStore: activityStore,
            repeatActivity: id => repeatedId = id
        );

        var activity = Assert.Single(viewModel.Activities);

        Assert.True(activity.HasRepeatAction);

        activity.RepeatCommand.Execute(null);

        Assert.Equal(activityId, repeatedId);
    }

    private static DailyJournalHistoryViewModel CreateViewModel(
        DateOnly currentDate,
        IFoodDiaryStore foodDiaryStore,
        IActivityStore? activityStore = null,
        IBodyMeasurementStore? bodyMeasurementStore = null,
        Action<Guid>? repeatActivity = null,
        RecurringPlannedActivityService? recurringPlannedActivityService = null,
        Action<Guid>? editRecurringPlannedActivity = null,
        Action<Guid, DateOnly>? completeRecurringPlannedActivity = null,
        Action<Guid, DateOnly>? skipRecurringPlannedActivity = null
    ) {
        var journalProvider = new DailyJournalDaySnapshotProvider(
            new FoodDiaryDaySnapshotProvider(foodDiaryStore),
            activityStore ?? new InMemoryActivityStore()
        );

        var weeklySummaryProvider = new WeeklyJournalSummaryProvider(
            journalProvider,
            new BodyMeasurementHistoryService(bodyMeasurementStore ?? new InMemoryBodyMeasurementStore())
        );

        return new DailyJournalHistoryViewModel(
            snapshotProvider: journalProvider,
            weeklySummaryProvider: weeklySummaryProvider,
            currentDate: currentDate,
            addFood: _ => { },
            addApproximateFood: _ => { },
            editFood: _ => { },
            deleteFood: _ => { },
            setFoodLogComplete: (_, _) => { },
            addActivity: _ => { },
            editActivity: _ => { },
            deleteActivity: _ => { },
            onClosed: () => { },
            repeatActivity: repeatActivity,
            recurringPlannedActivityService: recurringPlannedActivityService,
            editRecurringPlannedActivity: editRecurringPlannedActivity,
            completeRecurringPlannedActivity: completeRecurringPlannedActivity,
            skipRecurringPlannedActivity: skipRecurringPlannedActivity
        );
    }

    private static MealEntry CreateMeal(DateOnly date) {
        return new MealEntry(
            Id: Guid.NewGuid(),
            Date: date,
            Name: "Обед",
            Role: MealGroupRole.Lunch
        );
    }

    private static FoodLogEntry CreateFood(Guid mealId, decimal caloriesKcal) {
        return new FoodLogEntry(
            Id: Guid.NewGuid(),
            MealEntryId: mealId,
            Name: "Тестовая еда",
            Quantity: FoodQuantity.Portions(1m),
            Nutrition: new NutritionFacts(
                Basis: NutritionBasis.Total,
                CaloriesKcal: caloriesKcal,
                ProteinG: 30m,
                FatG: 20m,
                CarbsG: 60m
            ),
            Source: FoodLogSource.Manual
        );
    }
}
