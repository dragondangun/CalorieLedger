using CalorieLedger.Application.Adaptive;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Application.Today;
using CalorieLedger.Domain.Profile;
using CalorieLedger.Persistence;
using CalorieLedger.ViewModels.Adaptive;
using CalorieLedger.ViewModels.Profile;
using CalorieLedger.ViewModels.Today;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Linq;
namespace CalorieLedger.ViewModels;
using CalorieLedger.Application.Time;
using CalorieLedger.Infrastructure;
using System.Diagnostics.Metrics;

public partial class MainViewModel:ViewModelBase {
    private readonly ITodayDashboardSnapshotProvider todayProvider;

    private readonly NutritionGoalUpdateService goalUpdateService;
    private readonly NutritionGoalTransitionService goalTransitionService;
    private readonly NutritionGoalEditorService goalEditorService;
    private readonly BodyMeasurementHistoryService bodyMeasurementHistoryService;
    private readonly BodyMeasurementEditorService bodyMeasurementEditorService;
    private readonly UserNutritionProfileEditorService profileEditorService;
    private readonly IAdaptiveEnergyAssessmentPresentationProvider adaptiveEnergyAssessmentPresentationProvider;
    private readonly IUserNutritionProfileProvider currentProfileProvider;
    private readonly ICurrentDateProvider currentDateProvider;

    [ObservableProperty]
    private TodayDashboardViewModel today;

    [ObservableProperty]
    private NutritionGoalEditorViewModel? goalEditor;
    [ObservableProperty]
    private BodyMeasurementEditorViewModel? bodyMeasurementEditor;
    [ObservableProperty]
    private BodyTrendsViewModel bodyTrends;
    [ObservableProperty]
    private AdaptiveEnergyAssessmentViewModel adaptiveEnergyAssessment;
    [ObservableProperty]
    private UserNutritionProfileEditorViewModel? profileEditor;
    [ObservableProperty]
    private UserNutritionProfileSummaryViewModel profileSummary;
    [ObservableProperty]
    private bool isBodyMeasurementHistoryExpanded;

    public ObservableCollection<BodyMeasurementListItemViewModel> BodyMeasurements { get; } = [];

    private const int CollapsedBodyMeasurementCount = 5;
    public ObservableCollection<BodyMeasurementListItemViewModel> VisibleBodyMeasurements { get; } = [];

    public bool HasBodyMeasurements => BodyMeasurements.Count > 0;

    public bool HasNoBodyMeasurements => BodyMeasurements.Count == 0;

    public bool CanToggleBodyMeasurementHistory => BodyMeasurements.Count > CollapsedBodyMeasurementCount;

    public string BodyMeasurementHistoryToggleText =>
        IsBodyMeasurementHistoryExpanded
            ? "Свернуть историю"
            : $"Показать все измерения ({BodyMeasurements.Count})";

    public bool IsProfileEditorOpen => ProfileEditor is not null;

    public MainViewModel():this(
        JsonBodyMeasurementStore.CreateDefault(),
        JsonUserNutritionProfileStore.CreateDefault(),
        new UnavailableAdaptiveEnergyAssessmentPresentationProvider(),
        new SystemCurrentDateProvider()
    ) {}

    public MainViewModel(IBodyMeasurementStore bodyMeasurementStore):this(
        bodyMeasurementStore,
        CreateInMemoryProfileStore(),
        new UnavailableAdaptiveEnergyAssessmentPresentationProvider(),
        new SystemCurrentDateProvider()
    ) {}

    public MainViewModel(IBodyMeasurementStore bodyMeasurementStore, ICurrentDateProvider currentDateProvider):this(
        bodyMeasurementStore,
        CreateInMemoryProfileStore(),
        new UnavailableAdaptiveEnergyAssessmentPresentationProvider(),
        currentDateProvider
    ) {}

    public MainViewModel(IBodyMeasurementStore bodyMeasurementStore, IAdaptiveEnergyAssessmentPresentationProvider adaptiveEnergyAssessmentPresentationProvider):this(
        bodyMeasurementStore,
        CreateInMemoryProfileStore(),
        adaptiveEnergyAssessmentPresentationProvider,
        new SystemCurrentDateProvider()
    ) {}

    public MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore,
        IUserNutritionProfileStore profileStore,
        IAdaptiveEnergyAssessmentPresentationProvider
        adaptiveEnergyAssessmentPresentationProvider)
    :this(
        bodyMeasurementStore,
        profileStore,
        adaptiveEnergyAssessmentPresentationProvider,
        new SystemCurrentDateProvider()
    ) {}

    public MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore,
        IUserNutritionProfileStore profileStore,
        IAdaptiveEnergyAssessmentPresentationProvider adaptiveEnergyAssessmentPresentationProvider,
        ICurrentDateProvider currentDateProvider) 
    {
        ArgumentNullException.ThrowIfNull(bodyMeasurementStore);
        ArgumentNullException.ThrowIfNull(profileStore);
        ArgumentNullException.ThrowIfNull(adaptiveEnergyAssessmentPresentationProvider);
        ArgumentNullException.ThrowIfNull(currentDateProvider);

        if(profileStore is not IUserNutritionProfileWriter profileWriter) {
            throw new ArgumentException(
                "Profile store must implement IUserNutritionProfileWriter.",
                nameof(profileStore)
            );
        }

        this.currentDateProvider = currentDateProvider;
        this.adaptiveEnergyAssessmentPresentationProvider = adaptiveEnergyAssessmentPresentationProvider;

        profileEditorService = new UserNutritionProfileEditorService(
            profileStore: profileStore,
            profileWriter: profileWriter
        );

        bodyMeasurementHistoryService = new BodyMeasurementHistoryService(bodyMeasurementStore);

        bodyMeasurementEditorService = new BodyMeasurementEditorService(bodyMeasurementHistoryService);

        currentProfileProvider = new BodyMeasurementAwareNutritionProfileProvider(
            baseProfileProvider: profileStore,
            measurementHistoryService: bodyMeasurementHistoryService
        );

        todayProvider = new SampleTodayDashboardSnapshotProvider(
            currentProfileProvider
        );

        var adaptiveEvaluationStore = new InMemoryAdaptiveEnergyEvaluationStore();

        var adaptiveAssessmentService = new AdaptiveEnergyAssessmentService(adaptiveEvaluationStore);

        goalUpdateService = new NutritionGoalUpdateService(
            profileStore,
            adaptiveAssessmentService
        );

        goalTransitionService = new NutritionGoalTransitionService(goalUpdateService);

        goalEditorService = new NutritionGoalEditorService(
            profileProvider: currentProfileProvider,
            goalUpdateService: goalUpdateService
        );

        bodyTrends = BodyTrendsViewModel.CreateUnavailable();

        adaptiveEnergyAssessment = AdaptiveEnergyAssessmentViewModel.CreateUnavailable(
            "Адаптивная оценка ещё не рассчитана."
        );

        profileSummary = UserNutritionProfileSummaryViewModelFactory.Create(
            profile: currentProfileProvider.GetCurrentProfile(),
            latestMeasurement: bodyMeasurementHistoryService.GetLatest(),
            currentDate: currentDateProvider.GetCurrentDate(),
            editProfile: EditProfile,
            addBodyMeasurement: AddBodyMeasurement
        );

        today = CreateTodayDashboardViewModel();

        RefreshBodyMeasurements();
        RefreshProfileSummary();
    }

    public bool IsGoalEditorOpen => GoalEditor is not null;

    public bool IsBodyMeasurementEditorOpen => BodyMeasurementEditor is not null;

    public bool IsTodayDashboardVisible => GoalEditor is null && BodyMeasurementEditor is null && ProfileEditor is null;

    [RelayCommand]
    private void AddBodyMeasurement() {
        var currentDate = currentDateProvider.GetCurrentDate();

        var existingMeasurement = bodyMeasurementHistoryService.GetByDate(currentDate);

        var draft = existingMeasurement is null
            ? bodyMeasurementEditorService.CreateNew(currentDate)
            : BodyMeasurementDraftMapper.FromEntry(existingMeasurement);

        OpenBodyMeasurementEditor(
            draft,
            currentDate
        );
    }

    [RelayCommand]
    private void EditProfile() {
        ProfileEditor =
            new UserNutritionProfileEditorViewModel(
                editorService: profileEditorService,
                draft: profileEditorService.LoadCurrentProfile(),
                onSaved: OnProfileEditorSaved,
                onCancelled: CloseProfileEditor
            );
    }

    [RelayCommand(CanExecute = nameof(CanToggleBodyMeasurementHistory))]
    private void ToggleBodyMeasurementHistory() {
        IsBodyMeasurementHistoryExpanded = !IsBodyMeasurementHistoryExpanded;

        RefreshVisibleBodyMeasurements();
    }

    partial void OnIsBodyMeasurementHistoryExpandedChanged(bool value) {
        OnPropertyChanged(nameof(BodyMeasurementHistoryToggleText));
    }

    private void EditBodyMeasurement(Guid id) {
        var draft = bodyMeasurementEditorService.Load(id);

        if(draft is null) {
            return;
        }

        var currentDate = currentDateProvider.GetCurrentDate();

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

        var measurements = bodyMeasurementHistoryService.GetAll();
        var currentDate = currentDateProvider.GetCurrentDate();
        // Хранилище возвращает записи от старых к новым.
        // На экране показываем новые сверху.

        for(var index = measurements.Count - 1; index >= 0; index--) {
            BodyMeasurementEntry? previousMeasurement = index > 0
                ? measurements[index - 1]
                : null;

            BodyMeasurements.Add(
                new BodyMeasurementListItemViewModel(
                    entry: measurements[index],
                    onEdit: EditBodyMeasurement,
                    onDelete: DeleteBodyMeasurement,
                    previousMeasurement: previousMeasurement,
                    isLatest: index == measurements.Count - 1,
                    currentDate: currentDate
                )
            );
        }

        OnPropertyChanged(nameof(HasBodyMeasurements));
        OnPropertyChanged(nameof(HasNoBodyMeasurements));

        RefreshBodyTrends();
        RefreshProfileSummary();
        RefreshAdaptiveEnergyAssessment();
        RefreshVisibleBodyMeasurements();
    }

    private void RefreshBodyTrends() {
        var currentDate = currentDateProvider.GetCurrentDate();
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
                OpenCurrentGoalEditor();

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

    private void OpenGoalEditorWithSuggestedStrategy(AdaptiveEnergyStrategySuggestion suggestion) {
        ArgumentNullException.ThrowIfNull(suggestion);

        var draft = goalEditorService.LoadCurrentGoalWithSuggestedStrategy(
            strategyMode: suggestion.Mode,
            strategyValue: suggestion.Value
        );

        OpenGoalEditor(draft);
    }

    private void OpenCurrentGoalEditor() {
        OpenGoalEditor(
            goalEditorService.LoadCurrentGoal()
        );
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

    private void RefreshAdaptiveEnergyAssessment() {
        var presentation = adaptiveEnergyAssessmentPresentationProvider.GetCurrent();

        AdaptiveEnergyAssessment = AdaptiveEnergyAssessmentViewModelFactory.Create(
            presentation: presentation,
            openGoalEditor: OpenGoalEditorWithSuggestedStrategy
        );
    }

    private static InMemoryUserNutritionProfileStore CreateInMemoryProfileStore() {
        var profile = new SampleUserNutritionProfileProvider().GetCurrentProfile();

        return new InMemoryUserNutritionProfileStore(profile);
    }

    private void OnProfileEditorSaved() {
        ProfileEditor = null;

        RefreshProfileSummary();
        Today = CreateTodayDashboardViewModel("Профиль сохранён. Дневная норма КБЖУ пересчитана.");

        RefreshAdaptiveEnergyAssessment();
    }

    private void CloseProfileEditor() {
        ProfileEditor = null;
    }

    partial void OnProfileEditorChanged(UserNutritionProfileEditorViewModel? value) {
        OnPropertyChanged(nameof(IsProfileEditorOpen));
        OnPropertyChanged(nameof(IsTodayDashboardVisible));
    }

    private void RefreshProfileSummary() {
        ProfileSummary = UserNutritionProfileSummaryViewModelFactory.Create(
            profile: currentProfileProvider.GetCurrentProfile(),
            latestMeasurement: bodyMeasurementHistoryService.GetLatest(),
            currentDate: currentDateProvider.GetCurrentDate(),
            editProfile: EditProfile,
            addBodyMeasurement: AddBodyMeasurement
        );
    }

    private void RefreshVisibleBodyMeasurements() {
        if(BodyMeasurements.Count <= CollapsedBodyMeasurementCount) {
            IsBodyMeasurementHistoryExpanded = false;
        }

        VisibleBodyMeasurements.Clear();

        var visibleCount = 
            IsBodyMeasurementHistoryExpanded
                ? BodyMeasurements.Count
                : Math.Min(
                    CollapsedBodyMeasurementCount,
                    BodyMeasurements.Count
                );

        for(var index = 0; index < visibleCount; index++) {
            VisibleBodyMeasurements.Add(
                BodyMeasurements[index]
            );
        }

        OnPropertyChanged(nameof(CanToggleBodyMeasurementHistory));
        OnPropertyChanged(nameof(BodyMeasurementHistoryToggleText));

        ToggleBodyMeasurementHistoryCommand.NotifyCanExecuteChanged();
    }
}