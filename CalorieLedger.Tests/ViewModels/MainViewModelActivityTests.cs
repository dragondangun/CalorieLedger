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
