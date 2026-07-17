using CalorieLedger.Application.Adaptive;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Application.Today;
using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Profile;
using CalorieLedger.ViewModels.Today;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CalorieLedger.ViewModels;

public partial class MainViewModel:ViewModelBase {
    private readonly IUserNutritionProfileStore profileStore;
    private readonly ITodayDashboardSnapshotProvider todayProvider;

    private readonly NutritionGoalUpdateService goalUpdateService;
    private readonly NutritionGoalTransitionService goalTransitionService;
    private readonly NutritionGoalEditorService goalEditorService;

    [ObservableProperty]
    private TodayDashboardViewModel today;

    [ObservableProperty]
    private NutritionGoalEditorViewModel? goalEditor;

    public MainViewModel() {
        profileStore = new SampleUserNutritionProfileProvider();

        todayProvider = new SampleTodayDashboardSnapshotProvider(profileStore);

        var adaptiveEvaluationStore = new InMemoryAdaptiveEnergyEvaluationStore();

        var adaptiveAssessmentService = new AdaptiveEnergyAssessmentService(adaptiveEvaluationStore);

        goalUpdateService = new NutritionGoalUpdateService(
            profileStore,
            adaptiveAssessmentService);

        goalTransitionService = new NutritionGoalTransitionService(
            goalUpdateService);

        goalEditorService = new NutritionGoalEditorService(
            profileProvider: profileStore,
            goalUpdateService: goalUpdateService);

        today = CreateTodayDashboardViewModel();
    }

    public bool IsGoalEditorOpen =>
        GoalEditor is not null;

    public bool IsTodayDashboardVisible =>
        GoalEditor is null;

    private TodayDashboardViewModel CreateTodayDashboardViewModel(
        string? actionSummary = null) {
        var snapshot = todayProvider.GetToday();

        return new TodayDashboardViewModel(
            snapshot: snapshot,
            tryExecuteGoalAction: TryExecuteGoalAction,
            initialGoalActionSummary: actionSummary);
    }

    private bool TryExecuteGoalAction(GoalNextAction action) {
        switch(action) {
            case GoalNextAction.SwitchToMaintenance:
                return SwitchToMaintenance();

            case GoalNextAction.SetNewGoal:
                OpenGoalEditor(
                    goalEditorService.LoadCurrentGoal());

                return true;

            case GoalNextAction.StartWeightLoss:
                OpenGoalEditor(
                    goalEditorService.CreateNewGoal(
                        WeightGoalType.LoseWeight));

                return true;

            case GoalNextAction.StartWeightGain:
                OpenGoalEditor(
                    goalEditorService.CreateNewGoal(
                        WeightGoalType.GainWeight));

                return true;

            default:
                return false;
        }
    }

    private bool SwitchToMaintenance() {
        var result =
            goalTransitionService.SwitchToMaintenance();

        if(!result.IsSuccess) {
            var errorCodes =
                string.Join(", ", result.Errors);

            Today.GoalActionSelectionSummary =
                "Не удалось изменить цель. " +
                $"Ошибки проверки: {errorCodes}.";

            return true;
        }

        Today = CreateTodayDashboardViewModel(
            "Цель изменена на поддержание. " +
            "Дневная норма КБЖУ пересчитана.");

        return true;
    }

    private void OpenGoalEditor(NutritionGoalDraft draft) {
        GoalEditor = new NutritionGoalEditorViewModel(
            editorService: goalEditorService,
            draft: draft,
            onSaved: OnGoalEditorSaved,
            onCancelled: CloseGoalEditor);
    }

    private void OnGoalEditorSaved() {
        GoalEditor = null;

        Today = CreateTodayDashboardViewModel(
            "Цель сохранена. " +
            "Дневная норма КБЖУ пересчитана.");
    }

    private void CloseGoalEditor() {
        GoalEditor = null;
    }

    partial void OnGoalEditorChanged(NutritionGoalEditorViewModel? value) {
        OnPropertyChanged(nameof(IsGoalEditorOpen));
        OnPropertyChanged(nameof(IsTodayDashboardVisible));
    }
}