using CalorieLedger.Application.Profiles;
using CalorieLedger.Application.Today;
using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Today;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CalorieLedger.ViewModels;

public partial class MainViewModel:ViewModelBase {
    private readonly IUserNutritionProfileStore profileStore;
    private readonly ITodayDashboardSnapshotProvider todayProvider;
    private readonly NutritionGoalTransitionService
        goalTransitionService;

    [ObservableProperty]
    private TodayDashboardViewModel today;

    public MainViewModel() {
        profileStore =
            new SampleUserNutritionProfileProvider();

        todayProvider =
            new SampleTodayDashboardSnapshotProvider(
                profileStore);

        goalTransitionService =
            new NutritionGoalTransitionService(
                profileStore);

        today = CreateTodayDashboardViewModel();
    }

    private TodayDashboardViewModel
        CreateTodayDashboardViewModel(
            string? actionSummary = null) {
        var snapshot = todayProvider.GetToday();

        return new TodayDashboardViewModel(
            snapshot: snapshot,
            tryExecuteGoalAction: TryExecuteGoalAction,
            initialGoalActionSummary: actionSummary);
    }

    private bool TryExecuteGoalAction(
        GoalNextAction action) {
        if(action != GoalNextAction.SwitchToMaintenance) {
            return false;
        }

        goalTransitionService.SwitchToMaintenance();

        Today = CreateTodayDashboardViewModel(
            "Цель изменена на поддержание. " +
            "Дневная норма КБЖУ пересчитана.");

        return true;
    }
}