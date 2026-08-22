using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Tests.TestDoubles;
using CalorieLedger.ViewModels;
using CalorieLedger.ViewModels.Activities;
using CalorieLedger.ViewModels.History;
using CalorieLedger.ViewModels.Meals;

namespace CalorieLedger.Tests.ViewModels;

public sealed class MainViewModelNavigationTests {
    [Fact]
    public void DailyJournal_ChildEditors_HideHistoryAndRestoreItOnClose() {
        var viewModel = CreateViewModel(new DateOnly(2026, 8, 19));

        viewModel.OpenDailyJournalHistoryCommand.Execute(null);

        var history = Assert.IsType<DailyJournalHistoryViewModel>(
            viewModel.DailyJournalHistory
        );

        Assert.True(viewModel.IsDailyJournalHistoryVisible);
        AssertSingleVisibleSurface(viewModel);

        history.AddFoodCommand.Execute(null);

        var foodEditor = Assert.IsType<FoodLogEditorViewModel>(viewModel.FoodLogEditor);

        Assert.True(viewModel.IsDailyJournalHistoryOpen);
        Assert.False(viewModel.IsDailyJournalHistoryVisible);
        Assert.True(viewModel.IsFoodLogEditorVisible);
        AssertSingleVisibleSurface(viewModel);

        foodEditor.CancelCommand.Execute(null);

        Assert.True(viewModel.IsDailyJournalHistoryVisible);
        AssertSingleVisibleSurface(viewModel);

        history.AddActivityCommand.Execute(null);

        var activityEditor = Assert.IsType<ActivityEditorViewModel>(viewModel.ActivityEditor);

        Assert.True(viewModel.IsDailyJournalHistoryOpen);
        Assert.False(viewModel.IsDailyJournalHistoryVisible);
        Assert.True(viewModel.IsActivityEditorVisible);
        AssertSingleVisibleSurface(viewModel);

        activityEditor.CancelCommand.Execute(null);

        Assert.True(viewModel.IsDailyJournalHistoryVisible);
        AssertSingleVisibleSurface(viewModel);
    }

    [Fact]
    public void MealPlan_IsAStandaloneMainSurfaceAndReturnsToTodayOnClose() {
        var viewModel = CreateViewModel(new DateOnly(2026, 8, 19));

        viewModel.OpenMealPlanCommand.Execute(null);

        Assert.True(viewModel.IsMealPlanOpen);
        Assert.True(viewModel.IsMealPlanVisible);
        Assert.False(viewModel.IsTodayDashboardVisible);
        AssertSingleVisibleSurface(viewModel);

        viewModel.MealPlanManager!.CloseCommand.Execute(null);

        Assert.False(viewModel.IsMealPlanOpen);
        Assert.True(viewModel.IsTodayDashboardVisible);
        AssertSingleVisibleSurface(viewModel);
    }

    [Fact]
    public void Synchronization_IsAStandaloneMainSurfaceAndReturnsToTodayOnClose() {
        var viewModel = CreateViewModel(new DateOnly(2026, 8, 20));

        viewModel.OpenSynchronizationCommand.Execute(null);

        Assert.True(viewModel.IsSyncManagerOpen);
        Assert.True(viewModel.IsSyncManagerVisible);
        Assert.False(viewModel.IsTodayDashboardVisible);
        AssertSingleVisibleSurface(viewModel);

        viewModel.SyncManager!.CloseCommand.Execute(null);

        Assert.False(viewModel.IsSyncManagerOpen);
        Assert.True(viewModel.IsTodayDashboardVisible);
        AssertSingleVisibleSurface(viewModel);
    }

    [Fact]
    public void OpeningAnotherMainSurface_HidesCurrentSurfaceAndReturnsToItOnClose() {
        var viewModel = CreateViewModel(new DateOnly(2026, 8, 19));

        viewModel.OpenDailyJournalHistoryCommand.Execute(null);

        var history = Assert.IsType<DailyJournalHistoryViewModel>(
            viewModel.DailyJournalHistory
        );

        viewModel.OpenFridgeCommand.Execute(null);

        Assert.True(viewModel.IsDailyJournalHistoryOpen);
        Assert.False(viewModel.IsDailyJournalHistoryVisible);
        Assert.True(viewModel.IsFridgeVisible);
        AssertSingleVisibleSurface(viewModel);

        viewModel.FridgeManager!.CloseCommand.Execute(null);

        Assert.True(viewModel.IsDailyJournalHistoryVisible);
        AssertSingleVisibleSurface(viewModel);

        history.CloseCommand.Execute(null);

        Assert.True(viewModel.IsTodayDashboardVisible);
        AssertSingleVisibleSurface(viewModel);
    }

    private static MainViewModel CreateViewModel(DateOnly currentDate) {
        var profileStore = new InMemoryUserNutritionProfileStore(
            new SampleUserNutritionProfileProvider().GetCurrentProfile()
        );

        return new MainViewModel(
            new InMemoryBodyMeasurementStore(),
            profileStore,
            new InMemoryFoodDiaryStore(),
            new FixedCurrentDateProvider(currentDate)
        );
    }

    private static void AssertSingleVisibleSurface(MainViewModel viewModel) {
        var visibleSurfaceCount = new[] {
            viewModel.IsTodayDashboardVisible,
            viewModel.IsGoalEditorVisible,
            viewModel.IsBodyMeasurementEditorVisible,
            viewModel.IsProfileEditorVisible,
            viewModel.IsFoodLogEditorVisible,
            viewModel.IsProductCatalogVisible,
            viewModel.IsDailyJournalHistoryVisible,
            viewModel.IsCookingSessionManagerVisible,
            viewModel.IsFridgeVisible,
            viewModel.IsMealPlanVisible,
            viewModel.IsActivityEditorVisible,
            viewModel.IsPlannedActivityManagerVisible,
            viewModel.IsRecurringPlannedActivityManagerVisible,
            viewModel.IsSyncManagerVisible,
        }.Count(isVisible => isVisible);

        Assert.Equal(1, visibleSurfaceCount);
    }
}
