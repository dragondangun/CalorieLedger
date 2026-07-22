using CalorieLedger.Application.Adaptive;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Application.Today;
using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Profile;
using CalorieLedger.ViewModels.Today;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using CalorieLedger.Persistence;
namespace CalorieLedger.ViewModels;

public partial class MainViewModel:ViewModelBase {
    private readonly IUserNutritionProfileStore profileStore;
    private readonly ITodayDashboardSnapshotProvider todayProvider;

    private readonly NutritionGoalUpdateService goalUpdateService;
    private readonly NutritionGoalTransitionService goalTransitionService;
    private readonly NutritionGoalEditorService goalEditorService;
    private readonly BodyMeasurementHistoryService bodyMeasurementHistoryService;
    private readonly BodyMeasurementEditorService bodyMeasurementEditorService;

    [ObservableProperty]
    private TodayDashboardViewModel today;

    [ObservableProperty]
    private NutritionGoalEditorViewModel? goalEditor;
    [ObservableProperty]
    private BodyMeasurementEditorViewModel? bodyMeasurementEditor;
    [ObservableProperty]
    private BodyTrendsViewModel bodyTrends;

    public ObservableCollection<BodyMeasurementListItemViewModel> BodyMeasurements { get; } = [];

    public bool HasBodyMeasurements => BodyMeasurements.Count > 0;

    public bool HasNoBodyMeasurements => BodyMeasurements.Count == 0;

    public MainViewModel(): this(JsonBodyMeasurementStore.CreateDefault()) {
    }

    public MainViewModel(IBodyMeasurementStore bodyMeasurementStore) {
        ArgumentNullException.ThrowIfNull(bodyMeasurementStore);
        profileStore = new SampleUserNutritionProfileProvider();

        bodyMeasurementHistoryService = new BodyMeasurementHistoryService(bodyMeasurementStore);

        bodyMeasurementEditorService = new BodyMeasurementEditorService(bodyMeasurementHistoryService);

        var currentProfileProvider = new BodyMeasurementAwareNutritionProfileProvider(
            baseProfileProvider: profileStore,
            measurementHistoryService: bodyMeasurementHistoryService);

        todayProvider = new SampleTodayDashboardSnapshotProvider(currentProfileProvider);

        var adaptiveEvaluationStore = new InMemoryAdaptiveEnergyEvaluationStore();

        var adaptiveAssessmentService = new AdaptiveEnergyAssessmentService(adaptiveEvaluationStore);

        goalUpdateService = new NutritionGoalUpdateService(
            profileStore,
            adaptiveAssessmentService);

        goalTransitionService = new NutritionGoalTransitionService(goalUpdateService);

        goalEditorService = new NutritionGoalEditorService(
            profileProvider: currentProfileProvider,
            goalUpdateService: goalUpdateService);

        bodyTrends = BodyTrendsViewModel.CreateUnavailable();
        today = CreateTodayDashboardViewModel();

        RefreshBodyMeasurements();
    }

    public bool IsGoalEditorOpen => GoalEditor is not null;

    public bool IsBodyMeasurementEditorOpen => BodyMeasurementEditor is not null;

    public bool IsTodayDashboardVisible => GoalEditor is null && BodyMeasurementEditor is null;

    [RelayCommand]
    private void AddBodyMeasurement() {
        var currentDate = DateOnly.FromDateTime(DateTime.Today);

        var draft = bodyMeasurementEditorService.CreateNew(currentDate);

        OpenBodyMeasurementEditor(
            draft,
            currentDate);
    }

    private void EditBodyMeasurement(Guid id) {
        var draft = bodyMeasurementEditorService.Load(id);

        if(draft is null) {
            return;
        }

        var currentDate = DateOnly.FromDateTime(DateTime.Today);

        OpenBodyMeasurementEditor(
            draft,
            currentDate);
    }

    private void DeleteBodyMeasurement(Guid id) {
        var deleted = bodyMeasurementEditorService.Delete(id);

        if(!deleted) {
            return;
        }

        RefreshBodyMeasurements();

        Today = CreateTodayDashboardViewModel("Измерение удалено. Текущий профиль и дневная норма КБЖУ обновлены.");
    }

    private void OpenBodyMeasurementEditor(BodyMeasurementDraft draft, DateOnly currentDate) {
        BodyMeasurementEditor = new BodyMeasurementEditorViewModel(
            editorService: bodyMeasurementEditorService,
            draft: draft,
            currentDate: currentDate,
            onSaved: OnBodyMeasurementSaved,
            onCancelled: CloseBodyMeasurementEditor);
    }

    private void RefreshBodyMeasurements() {
        BodyMeasurements.Clear();

        var entries = bodyMeasurementHistoryService.GetAll();

        // Хранилище возвращает записи от старых к новым.
        // На экране показываем новые сверху.
        for(var index = entries.Count - 1; index >= 0; index--) {
            BodyMeasurements.Add(new BodyMeasurementListItemViewModel(
                entry: entries[index],
                onEdit: EditBodyMeasurement,
                onDelete: DeleteBodyMeasurement));
        }

        OnPropertyChanged(nameof(HasBodyMeasurements));

        OnPropertyChanged(nameof(HasNoBodyMeasurements));
        RefreshBodyTrends();
    }

    private void RefreshBodyTrends() {
        var currentDate = DateOnly.FromDateTime(DateTime.Today);
        var measurements = bodyMeasurementHistoryService.GetAll();
        BodyTrends = BodyTrendsViewModelFactory.Create(measurements, currentDate);
    }

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

    private void OnBodyMeasurementSaved() {
        BodyMeasurementEditor = null;

        RefreshBodyMeasurements();

        Today = CreateTodayDashboardViewModel("Измерение сохранено. Текущий профиль и дневная норма КБЖУ обновлены.");
    }

    private void CloseBodyMeasurementEditor() {
        BodyMeasurementEditor = null;
    }

    private void CloseGoalEditor() {
        GoalEditor = null;
    }

    partial void OnGoalEditorChanged(NutritionGoalEditorViewModel? value) {
        OnPropertyChanged(nameof(IsGoalEditorOpen));
        OnPropertyChanged(nameof(IsTodayDashboardVisible));
    }

    partial void OnBodyMeasurementEditorChanged(BodyMeasurementEditorViewModel? value) {
        OnPropertyChanged(nameof(IsBodyMeasurementEditorOpen));

        OnPropertyChanged(nameof(IsTodayDashboardVisible));
    }
}