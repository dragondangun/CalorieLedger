using CalorieLedger.Application.Adaptive;
using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Nutrition;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Application.Time;
using CalorieLedger.Application.Today;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Meals;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;
using CalorieLedger.Infrastructure;
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

public partial class MainViewModel:ViewModelBase {
    private readonly ITodayDashboardSnapshotProvider todayProvider;

    private readonly NutritionGoalUpdateService goalUpdateService;
    private readonly NutritionGoalTransitionService goalTransitionService;
    private readonly NutritionGoalEditorService goalEditorService;
    private readonly BodyMeasurementHistoryService bodyMeasurementHistoryService;
    private readonly BodyMeasurementEditorService bodyMeasurementEditorService;
    private readonly UserNutritionProfileEditorService profileEditorService;
    private readonly BodyMeasurementAwareNutritionProfileProvider  currentProfileProvider;
    private readonly IAdaptiveEnergyAssessmentPresentationProvider adaptiveEnergyAssessmentPresentationProvider;
    private readonly ICurrentDateProvider currentDateProvider;
    private readonly IFoodDiaryStore foodDiaryStore;

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
    private UserNutritionProfileSummaryViewModel profileSummary = null!;
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

    private delegate IAdaptiveEnergyAssessmentPresentationProvider AdaptiveEnergyAssessmentPresentationProviderFactory(
        BodyMeasurementAwareNutritionProfileProvider profileProvider,
        IFoodDiaryStore foodDiaryStore
    );

    public MainViewModel() : this(
    JsonBodyMeasurementStore.CreateDefault(),
    JsonUserNutritionProfileStore.CreateDefault(),
    JsonFoodDiaryStore.CreateDefault(),
    CreatePersistentAdaptiveProvider,
    new SystemCurrentDateProvider()
) { }

    public MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore
    ) : this(
        bodyMeasurementStore,
        CreateInMemoryProfileStore(),
        new InMemoryFoodDiaryStore(),
        CreateInMemoryAdaptiveProvider,
        new SystemCurrentDateProvider()
    ) { }

    public MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore,
        ICurrentDateProvider currentDateProvider
    ) : this(
        bodyMeasurementStore,
        CreateInMemoryProfileStore(),
        new InMemoryFoodDiaryStore(),
        CreateInMemoryAdaptiveProvider,
        currentDateProvider
    ) { }

    public MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore,
        IAdaptiveEnergyAssessmentPresentationProvider adaptiveEnergyAssessmentPresentationProvider
    ) : this(
        bodyMeasurementStore,
        CreateInMemoryProfileStore(),
        new InMemoryFoodDiaryStore(),
        CreateInjectedAdaptiveProviderFactory(
            adaptiveEnergyAssessmentPresentationProvider
        ),
        new SystemCurrentDateProvider()
    ) { }

    public MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore,
        IUserNutritionProfileStore profileStore,
        IAdaptiveEnergyAssessmentPresentationProvider adaptiveEnergyAssessmentPresentationProvider
    ) : this(
        bodyMeasurementStore,
        profileStore,
        new InMemoryFoodDiaryStore(),
        CreateInjectedAdaptiveProviderFactory(
            adaptiveEnergyAssessmentPresentationProvider
        ),
        new SystemCurrentDateProvider()
    ) { }

    public MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore,
        IUserNutritionProfileStore profileStore,
        IAdaptiveEnergyAssessmentPresentationProvider adaptiveEnergyAssessmentPresentationProvider,
        ICurrentDateProvider currentDateProvider
    ) : this(
        bodyMeasurementStore,
        profileStore,
        new InMemoryFoodDiaryStore(),
        CreateInjectedAdaptiveProviderFactory(
            adaptiveEnergyAssessmentPresentationProvider
        ),
        currentDateProvider
    ) { }

    public MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore,
        IUserNutritionProfileStore profileStore,
        IFoodDiaryStore foodDiaryStore,
        ICurrentDateProvider currentDateProvider
    ) : this(
        bodyMeasurementStore,
        profileStore,
        foodDiaryStore,
        CreateInMemoryAdaptiveProvider,
        currentDateProvider
    ) { }

    private MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore,
        IUserNutritionProfileStore profileStore,
        IFoodDiaryStore foodDiaryStore,
        AdaptiveEnergyAssessmentPresentationProviderFactory adaptiveEnergyAssessmentPresentationProviderFactory,
        ICurrentDateProvider currentDateProvider
    ) {
        ArgumentNullException.ThrowIfNull(bodyMeasurementStore);
        ArgumentNullException.ThrowIfNull(profileStore);
        ArgumentNullException.ThrowIfNull(adaptiveEnergyAssessmentPresentationProviderFactory);
        ArgumentNullException.ThrowIfNull(currentDateProvider);
        ArgumentNullException.ThrowIfNull(foodDiaryStore);

        if(profileStore is not IUserNutritionProfileWriter profileWriter) {
            throw new ArgumentException(
                "Profile store must implement IUserNutritionProfileWriter.",
                nameof(profileStore)
            );
        }

        this.currentDateProvider = currentDateProvider;
        this.foodDiaryStore = foodDiaryStore;

        profileEditorService = new UserNutritionProfileEditorService(
            profileStore: profileStore,
            profileWriter: profileWriter
        );

        bodyMeasurementHistoryService = new BodyMeasurementHistoryService(bodyMeasurementStore);

        bodyMeasurementEditorService = new BodyMeasurementEditorService(bodyMeasurementHistoryService);

        currentProfileProvider = new BodyMeasurementAwareNutritionProfileProvider(
            baseProfileProvider: profileStore,
            measurementHistoryService: bodyMeasurementHistoryService,
            currentDateProvider: currentDateProvider
        );

        todayProvider = new TodayDashboardSnapshotProvider(
            profileProvider: currentProfileProvider,
            foodDiaryStore: foodDiaryStore,
            currentDateProvider: currentDateProvider
        );

        adaptiveEnergyAssessmentPresentationProvider = adaptiveEnergyAssessmentPresentationProviderFactory(currentProfileProvider, foodDiaryStore);

        ArgumentNullException.ThrowIfNull(adaptiveEnergyAssessmentPresentationProvider);

        var adaptiveEnergyHistoryResetter = adaptiveEnergyAssessmentPresentationProvider as IAdaptiveEnergyHistoryResetter;

        goalUpdateService = new NutritionGoalUpdateService(
            profileStore,
            adaptiveEnergyHistoryResetter
        );

        ArgumentNullException.ThrowIfNull(adaptiveEnergyAssessmentPresentationProvider);

        goalUpdateService = new NutritionGoalUpdateService(
            profileStore,
            adaptiveEnergyHistoryResetter
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

        today = CreateTodayDashboardViewModel();

        RefreshAfterBodyMeasurementChange();
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

        OpenBodyMeasurementEditor(draft, currentDate);
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

        OpenBodyMeasurementEditor(draft, currentDate);
    }

    private void DeleteBodyMeasurement(Guid id) {
        var deleted = bodyMeasurementEditorService.Delete(id);

        if(!deleted) {
            return;
        }

        RefreshAfterBodyMeasurementChange();

        Today = CreateTodayDashboardViewModel("Измерение удалено. Текущий профиль и дневная норма КБЖУ обновлены.");
    }

    private void OpenBodyMeasurementEditor(BodyMeasurementDraft draft, DateOnly currentDate) {
        BodyMeasurementEditor = new BodyMeasurementEditorViewModel(
            editorService: bodyMeasurementEditorService,
            draft: draft,
            currentDate: currentDate,
            onSaved: OnBodyMeasurementSaved,
            onCancelled: CloseBodyMeasurementEditor
        );
    }

    private void RefreshBodyMeasurements(BodyMeasurementHistorySnapshot measurementSnapshot) {
        BodyMeasurements.Clear();

        var measurements = measurementSnapshot.AllMeasurements;

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
                    currentDate: measurementSnapshot.AsOfDate,
                    onAddMeasurement: AddBodyMeasurement
                )
            );
        }

        OnPropertyChanged(nameof(HasBodyMeasurements));
        OnPropertyChanged(nameof(HasNoBodyMeasurements));

        RefreshVisibleBodyMeasurements();
    }

    private void RefreshBodyTrends(BodyMeasurementHistorySnapshot measurementSnapshot) {
        BodyTrends = BodyTrendsViewModelFactory.Create(
            measurementSnapshot
        );
    }

    private TodayDashboardViewModel CreateTodayDashboardViewModel(string? actionSummary = null) {
        var snapshot = todayProvider.GetToday();

        return new TodayDashboardViewModel(
            snapshot: snapshot,
            tryExecuteGoalAction: TryExecuteGoalAction,
            addSampleFood: AddSampleFood,
            markOvereating: MarkOvereating,
            setFoodLogComplete: SetTodayFoodLogComplete,
            initialGoalActionSummary: actionSummary
        );
    }

    private void AddSampleFood() {
        var currentDate = currentDateProvider.GetCurrentDate();

        var meal = GetOrCreateTodayMeal(
            currentDate,
            "Перекусы",
            MealGroupRole.Snack
        );

        foodDiaryStore.SaveFoodEntry(
            new FoodLogEntry(
                Id: Guid.NewGuid(),
                MealEntryId: meal.Id,
                Name: "Творог тестовый",
                Quantity: FoodQuantity.Grams(
                    250m
                ),
                Nutrition: new NutritionFacts(
                    Basis: NutritionBasis.Per100Grams,
                    CaloriesKcal: 120m,
                    ProteinG: 17m,
                    FatG: 5m,
                    CarbsG: 3m
                ),
                Source: FoodLogSource.Manual
            )
        );

        foodDiaryStore.SetDateComplete(
            currentDate,
            false
        );

        RefreshAfterFoodDiaryChange();
    }

    private void MarkOvereating() {
        var currentDate = currentDateProvider.GetCurrentDate();

        var meal = GetOrCreateTodayMeal(
            currentDate,
            "Особые события",
            MealGroupRole.Custom
        );

        foodDiaryStore.SaveFoodEntry(
            new FoodLogEntry(
                Id: Guid.NewGuid(),
                MealEntryId: meal.Id,
                Name: "Праздник / переедание",
                Quantity: FoodQuantity.Portions(1m),
                Nutrition: new NutritionFacts(
                    Basis: NutritionBasis.Total,
                    CaloriesKcal: 1500m,
                    ProteinG: null,
                    FatG: null,
                    CarbsG: null
                ),
                Source: FoodLogSource.Approximation,
                IsApproximate: true
            )
        );

        foodDiaryStore.SetDateComplete(
            currentDate,
            false
        );

        RefreshAfterFoodDiaryChange();
    }

    private void SetTodayFoodLogComplete(bool isComplete) {
        foodDiaryStore.SetDateComplete(
            currentDateProvider.GetCurrentDate(),
            isComplete
        );

        RefreshAfterFoodDiaryChange();
    }

    private MealEntry GetOrCreateTodayMeal(
        DateOnly date,
        string name,
        MealGroupRole role
    ) {
        var existingMeal = foodDiaryStore
            .GetMeals(date, date)
            .FirstOrDefault(meal => meal.Name == name && meal.Role == role);

        if(existingMeal is not null) {
            return existingMeal;
        }

        var meal = new MealEntry(
            Id: Guid.NewGuid(),
            Date: date,
            Name: name,
            Role: role
        );

        foodDiaryStore.SaveMeal(
            meal
        );

        return meal;
    }

    private void RefreshAfterFoodDiaryChange() {
        Today = CreateTodayDashboardViewModel();

        RefreshAdaptiveEnergyAssessment();
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
        var result = goalTransitionService.SwitchToMaintenance();

        if(!result.IsSuccess) {
            var errorCodes = string.Join(", ", result.Errors);

            Today.GoalActionSelectionSummary = $"Не удалось изменить цель. Ошибки проверки: {errorCodes}.";

            return true;
        }

        Today = CreateTodayDashboardViewModel("Цель изменена на поддержание. Дневная норма КБЖУ пересчитана.");
        RefreshAdaptiveEnergyAssessment();

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
            "Цель сохранена. Дневная норма КБЖУ пересчитана."
        );

        RefreshAdaptiveEnergyAssessment();
    }

    private void OnBodyMeasurementSaved() {
        BodyMeasurementEditor = null;

        RefreshAfterBodyMeasurementChange();

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
        RefreshAdaptiveEnergyAssessment(
            GetCurrentBodyMeasurementSnapshot()
        );
    }

    private void RefreshAdaptiveEnergyAssessment(BodyMeasurementHistorySnapshot measurementSnapshot) {
        var presentation = adaptiveEnergyAssessmentPresentationProvider.GetCurrent(measurementSnapshot);

        AdaptiveEnergyAssessment = AdaptiveEnergyAssessmentViewModelFactory.Create(
            presentation: presentation,
            openGoalEditor: OpenGoalEditorWithSuggestedStrategy
        );
    }

    private static IAdaptiveEnergyAssessmentPresentationProvider CreatePersistentAdaptiveProvider(
        BodyMeasurementAwareNutritionProfileProvider profileProvider,
        IFoodDiaryStore foodDiaryStore
    ) {
        return CreateAdaptiveProvider(
            JsonAdaptiveEnergyEvaluationStore.CreateDefault(),
            foodDiaryStore,
            profileProvider
        );
    }

    private static IAdaptiveEnergyAssessmentPresentationProvider CreateInMemoryAdaptiveProvider(
        BodyMeasurementAwareNutritionProfileProvider profileProvider,
        IFoodDiaryStore foodDiaryStore
    ) {
        return CreateAdaptiveProvider(
            new InMemoryAdaptiveEnergyEvaluationStore(),
            foodDiaryStore,
            profileProvider
        );
    }

    private static IAdaptiveEnergyAssessmentPresentationProvider CreateAdaptiveProvider(
        IAdaptiveEnergyEvaluationStore evaluationStore,
        IFoodDiaryStore foodDiaryStore,
        BodyMeasurementAwareNutritionProfileProvider profileProvider
    ) {
        return new AdaptiveEnergyAssessmentPresentationProvider(
            new AdaptiveEnergyAssessmentService(evaluationStore),
            new DailyEnergyIntakeHistoryProvider(foodDiaryStore),
            profileProvider
        );
    }

    private static AdaptiveEnergyAssessmentPresentationProviderFactory CreateInjectedAdaptiveProviderFactory(
        IAdaptiveEnergyAssessmentPresentationProvider adaptiveEnergyAssessmentPresentationProvider
    ) {
        ArgumentNullException.ThrowIfNull(adaptiveEnergyAssessmentPresentationProvider);

        return (_, _) => adaptiveEnergyAssessmentPresentationProvider;
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
        RefreshProfileSummary(GetCurrentBodyMeasurementSnapshot());
    }

    private void RefreshProfileSummary(BodyMeasurementHistorySnapshot measurementSnapshot) {
        ProfileSummary = UserNutritionProfileSummaryViewModelFactory.Create(
            profile: currentProfileProvider.GetProfile(
                measurementSnapshot
            ),
            measurementSnapshot: measurementSnapshot,
            editProfile: EditProfile,
            addBodyMeasurement: AddBodyMeasurement
        );
    }

    private void RefreshVisibleBodyMeasurements() {
        if(BodyMeasurements.Count <= CollapsedBodyMeasurementCount) {
            IsBodyMeasurementHistoryExpanded = false;
        }

        VisibleBodyMeasurements.Clear();

        var visibleCount = IsBodyMeasurementHistoryExpanded
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

    private void RefreshAfterBodyMeasurementChange() {
        var measurementSnapshot = GetCurrentBodyMeasurementSnapshot();

        RefreshBodyMeasurements(measurementSnapshot);

        RefreshBodyMeasurementDerivedState(measurementSnapshot);

        RefreshAdaptiveEnergyAssessment(measurementSnapshot);
    }

    private void RefreshBodyMeasurementDerivedState(BodyMeasurementHistorySnapshot measurementSnapshot) {
        RefreshProfileSummary(measurementSnapshot);
        RefreshBodyTrends(measurementSnapshot);
    }

    private BodyMeasurementHistorySnapshot GetCurrentBodyMeasurementSnapshot() {
        return bodyMeasurementHistoryService.GetSnapshot(
            currentDateProvider.GetCurrentDate()
        );
    }
}
