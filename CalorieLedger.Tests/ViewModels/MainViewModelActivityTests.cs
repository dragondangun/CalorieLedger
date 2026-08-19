using CalorieLedger.Application.Activities;
using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Activities;
using CalorieLedger.Tests.TestDoubles;
using CalorieLedger.ViewModels;
using CalorieLedger.ViewModels.Activities;
using CalorieLedger.ViewModels.History;

namespace CalorieLedger.Tests.ViewModels;

public sealed class MainViewModelActivityTests {
    [Fact]
    public void AddActivity_Save_PersistsAndRefreshesToday() {
        var currentDate = new DateOnly(2026, 8, 18);

        var activityStore = new InMemoryActivityStore();

        var viewModel = CreateViewModel(currentDate, activityStore);

        viewModel.Today.AddActivityCommand.Execute(null);

        var baseTargetCalories = viewModel.Today.TargetCaloriesKcal;

        var editor = Assert.IsType<ActivityEditorViewModel>(viewModel.ActivityEditor);

        editor.Name = "HEMA";

        editor.BurnedCaloriesKcal = 350m;

        editor.StartedAtTime = new TimeSpan(18, 30, 0);

        editor.DurationMinutes = 75m;

        editor.SaveCommand.Execute(null);

        Assert.Null(viewModel.ActivityEditor);

        Assert.Equal(
            350m,
            viewModel.Today.ActivityBurnedCaloriesKcal
        );

        var saved = Assert.Single(activityStore.Get(currentDate, currentDate));

        Assert.Equal(
            "HEMA",
            saved.Name
        );

        Assert.Equal(
            new TimeOnly(18, 30),
            saved.StartedAt
        );

        Assert.Equal(
            TimeSpan.FromMinutes(75),
            saved.Duration
        );

        var item = Assert.Single(viewModel.Today.Activities);

        Assert.Equal(
            saved.Id,
            item.Id
        );

        Assert.Equal(
            baseTargetCalories + 350m,
            viewModel.Today.TargetCaloriesKcal
        );
    }

    [Fact]
    public void EditAndDeleteActivity_UpdatesStoreAndToday() {
        var currentDate = new DateOnly(2026, 8, 18);

        var activityStore = new InMemoryActivityStore();

        var entry = new ActivityEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            Name: "Ходьба",
            BurnedCaloriesKcal: 150m
        );

        activityStore.Save(entry);

        var viewModel = CreateViewModel(
            currentDate,
            activityStore
        );

        var item = Assert.Single(viewModel.Today.Activities);

        item.EditCommand.Execute(null);

        var editor = Assert.IsType<ActivityEditorViewModel>(
            viewModel.ActivityEditor
        );

        Assert.Equal(
            "Редактирование активности",
            editor.Title
        );

        editor.BurnedCaloriesKcal = 220m;

        editor.SaveCommand.Execute(null);

        Assert.Equal(
            220m,
            activityStore.Get(entry.Id)?.BurnedCaloriesKcal
        );

        Assert.Equal(
            220m,
            viewModel.Today.ActivityBurnedCaloriesKcal
        );

        var updatedItem = Assert.Single(viewModel.Today.Activities);

        updatedItem.DeleteCommand.Execute(null);

        Assert.True(updatedItem.IsDeleteConfirmationVisible);

        updatedItem.ConfirmDeleteCommand.Execute(null);

        Assert.Null(activityStore.Get(entry.Id));

        Assert.Empty(viewModel.Today.Activities);

        Assert.Equal(
            0m,
            viewModel.Today.ActivityBurnedCaloriesKcal
        );
    }

    [Fact]
    public void DailyJournal_AddActivity_PersistsActivityForSelectedPastDate() {
        var currentDate = new DateOnly(2026, 8, 18);
        var selectedDate = currentDate.AddDays(-1);
        var activityStore = new InMemoryActivityStore();

        var viewModel = CreateViewModel(currentDate, activityStore);

        viewModel.OpenDailyJournalHistoryCommand.Execute(null);

        var history = Assert.IsType<DailyJournalHistoryViewModel>(viewModel.DailyJournalHistory);

        history.PreviousDayCommand.Execute(null);
        Assert.Equal(selectedDate, history.SelectedDate);

        history.AddActivityCommand.Execute(null);

        var editor = Assert.IsType<ActivityEditorViewModel>(viewModel.ActivityEditor);

        editor.Name = "Ходьба";
        editor.BurnedCaloriesKcal = 180m;
        editor.DurationMinutes = 45m;
        editor.SaveCommand.Execute(null);

        var saved = Assert.Single(activityStore.Get(selectedDate, selectedDate));

        Assert.Equal("Ходьба", saved.Name);
        Assert.Equal(180m, saved.BurnedCaloriesKcal);
        Assert.Empty(activityStore.Get(currentDate, currentDate));

        Assert.Equal(selectedDate, history.SelectedDate);
        Assert.Single(history.Activities);
        Assert.Equal(180m, history.ExtraActivityBurnedCaloriesKcal);
    }

    [Fact]
    public void PlannedActivityEditor_DatePickerAdapter_UpdatesDateOnlyDraftValue() {
        var currentDate = new DateOnly(2026, 8, 19);
        var viewModel = CreateViewModel(currentDate, new InMemoryActivityStore());

        viewModel.OpenPlannedActivitiesCommand.Execute(null);

        var manager = Assert.IsType<PlannedActivityManagerViewModel>(
            viewModel.PlannedActivityManager
        );

        manager.AddCommand.Execute(null);

        var editor = Assert.IsType<PlannedActivityEditorViewModel>(manager.Editor);

        Assert.NotNull(editor.DatePickerDate);
        Assert.Equal(currentDate.Year, editor.DatePickerDate.Value.Year);
        Assert.Equal(currentDate.Month, editor.DatePickerDate.Value.Month);
        Assert.Equal(currentDate.Day, editor.DatePickerDate.Value.Day);

        editor.DatePickerDate = new DateTimeOffset(
            2026,
            8,
            25,
            0,
            0,
            0,
            TimeSpan.FromHours(3)
        );

        Assert.Equal(new DateOnly(2026, 8, 25), editor.Date);
    }

    [Fact]
    public void RecurringPlannedActivityEditor_DatePickerAdapter_UpdatesStartDate() {
        var currentDate = new DateOnly(2026, 8, 19);
        var viewModel = CreateViewModel(currentDate, new InMemoryActivityStore());

        viewModel.OpenPlannedActivitiesCommand.Execute(null);

        var plannedManager = Assert.IsType<PlannedActivityManagerViewModel>(
            viewModel.PlannedActivityManager
        );

        plannedManager.OpenRecurringActivitiesCommand.Execute(null);

        var recurringManager = Assert.IsType<RecurringPlannedActivityManagerViewModel>(
            viewModel.RecurringPlannedActivityManager
        );

        recurringManager.AddCommand.Execute(null);

        var editor = Assert.IsType<RecurringPlannedActivityEditorViewModel>(
            recurringManager.Editor
        );

        Assert.NotNull(editor.StartDatePickerDate);
        Assert.Equal(currentDate.Year, editor.StartDatePickerDate.Value.Year);
        Assert.Equal(currentDate.Month, editor.StartDatePickerDate.Value.Month);
        Assert.Equal(currentDate.Day, editor.StartDatePickerDate.Value.Day);

        editor.StartDatePickerDate = new DateTimeOffset(
            2026,
            9,
            2,
            0,
            0,
            0,
            TimeSpan.FromHours(3)
        );

        Assert.Equal(new DateOnly(2026, 9, 2), editor.StartDate);
    }

    [Fact]
    public void CompletePlannedActivity_HidesPlanManagerUntilCompletionEditorCloses() {
        var currentDate = new DateOnly(2026, 8, 19);
        var viewModel = CreateViewModel(currentDate, new InMemoryActivityStore());

        viewModel.OpenPlannedActivitiesCommand.Execute(null);

        var manager = Assert.IsType<PlannedActivityManagerViewModel>(
            viewModel.PlannedActivityManager
        );

        manager.AddCommand.Execute(null);

        var planEditor = Assert.IsType<PlannedActivityEditorViewModel>(manager.Editor);
        planEditor.Name = "Ходьба";
        planEditor.SaveCommand.Execute(null);

        var item = Assert.Single(manager.Activities);
        Assert.True(viewModel.IsPlannedActivityManagerVisible);

        item.CompleteCommand.Execute(null);

        var completionEditor = Assert.IsType<ActivityEditorViewModel>(
            viewModel.ActivityEditor
        );

        Assert.True(viewModel.IsPlannedActivityManagerOpen);
        Assert.False(viewModel.IsPlannedActivityManagerVisible);

        completionEditor.CancelCommand.Execute(null);

        Assert.Null(viewModel.ActivityEditor);
        Assert.True(viewModel.IsPlannedActivityManagerVisible);
    }

    [Fact]
    public void OpenRecurringSchedule_HidesPlanManagerAndRestoresItOnClose() {
        var currentDate = new DateOnly(2026, 8, 19);
        var viewModel = CreateViewModel(currentDate, new InMemoryActivityStore());

        viewModel.OpenPlannedActivitiesCommand.Execute(null);

        var plannedManager = Assert.IsType<PlannedActivityManagerViewModel>(
            viewModel.PlannedActivityManager
        );

        Assert.True(viewModel.IsPlannedActivityManagerVisible);

        plannedManager.OpenRecurringActivitiesCommand.Execute(null);

        var recurringManager = Assert.IsType<RecurringPlannedActivityManagerViewModel>(
            viewModel.RecurringPlannedActivityManager
        );

        Assert.True(viewModel.IsPlannedActivityManagerOpen);
        Assert.False(viewModel.IsPlannedActivityManagerVisible);
        Assert.True(viewModel.IsRecurringPlannedActivityManagerOpen);

        recurringManager.CloseCommand.Execute(null);

        Assert.Null(viewModel.RecurringPlannedActivityManager);
        Assert.True(viewModel.IsPlannedActivityManagerVisible);
    }

    private static MainViewModel CreateViewModel(
        DateOnly currentDate,
        IActivityStore activityStore
    ) {
        var profileStore = new InMemoryUserNutritionProfileStore(
            new SampleUserNutritionProfileProvider().GetCurrentProfile()
        );

        return new MainViewModel(
            new InMemoryBodyMeasurementStore(),
            profileStore,
            new InMemoryFoodDiaryStore(),
            activityStore,
            new FixedCurrentDateProvider(currentDate)
        );
    }
}
