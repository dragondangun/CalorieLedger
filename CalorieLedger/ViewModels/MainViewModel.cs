using CalorieLedger.Application.Activities;
using CalorieLedger.Application.Adaptive;
using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Fridge;
using CalorieLedger.Application.History;
using CalorieLedger.Application.MealPlanning;
using CalorieLedger.Application.Meals;
using CalorieLedger.Application.Nutrition;
using CalorieLedger.Application.Products;
using CalorieLedger.Application.Profiles;
using CalorieLedger.Application.Time;
using CalorieLedger.Application.Today;
using CalorieLedger.ViewModels.History;
using CalorieLedger.Domain.Profile;
using CalorieLedger.Infrastructure;
using CalorieLedger.Persistence;
using CalorieLedger.ViewModels.Activities;
using CalorieLedger.ViewModels.Adaptive;
using CalorieLedger.ViewModels.Cooking;
using CalorieLedger.ViewModels.Fridge;
using CalorieLedger.ViewModels.Meals;
using CalorieLedger.ViewModels.Products;
using CalorieLedger.ViewModels.Profile;
using CalorieLedger.ViewModels.Today;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
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
    private readonly ProductCatalogService productCatalogService;
    private readonly IAdaptiveEnergyAssessmentPresentationProvider adaptiveEnergyAssessmentPresentationProvider;
    private readonly ICurrentDateProvider currentDateProvider;
    private readonly IFoodDiaryStore foodDiaryStore;
    private readonly FoodLogEditorService foodLogEditorService;
    private readonly FoodDiaryDaySnapshotProvider foodDiaryDaySnapshotProvider;
    private readonly CookingSessionService cookingSessionService;
    private readonly FridgeInventoryService fridgeInventoryService;
    private readonly CookingExecutionService cookingExecutionService;
    private readonly IActivityStore activityStore;
    private readonly ActivityEditorService activityEditorService;
    private readonly DailyJournalDaySnapshotProvider dailyJournalSnapshotProvider;
    private readonly WeeklyJournalSummaryProvider weeklyJournalSummaryProvider;
    private readonly ActivityEnergySuggestionService activityEnergySuggestionService;
    private readonly ActivityPresetCatalogService activityPresetCatalogService;
    private readonly ActivityRepeatService activityRepeatService;
    private readonly RecentActivityService recentActivityService;
    private readonly PlannedActivityService plannedActivityService;
    private readonly PlannedActivityCompletionService plannedActivityCompletionService;
    private readonly RecurringPlannedActivityService recurringPlannedActivityService;
    private readonly RecurringPlannedActivityCompletionService recurringPlannedActivityCompletionService;
    private readonly MealPlanService mealPlanService;

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
    [ObservableProperty]
    private FoodLogEditorViewModel? foodLogEditor;
    [ObservableProperty]
    private ProductCatalogManagerViewModel? productCatalogManager;
    [ObservableProperty]
    private DailyJournalHistoryViewModel? dailyJournalHistory;
    [ObservableProperty]
    private CookingSessionManagerViewModel? cookingSessionManager;
    [ObservableProperty]
    private FridgeManagerViewModel? fridgeManager;
    [ObservableProperty]
    private ActivityEditorViewModel? activityEditor;
    [ObservableProperty]
    private PlannedActivityManagerViewModel? plannedActivityManager;
    [ObservableProperty]
    private RecurringPlannedActivityManagerViewModel? recurringPlannedActivityManager;

    private enum MainSurface {
        TodayDashboard,
        GoalEditor,
        BodyMeasurementEditor,
        ProfileEditor,
        FoodLogEditor,
        ProductCatalog,
        DailyJournalHistory,
        CookingSessions,
        Fridge,
        ActivityEditor,
        PlannedActivities,
        RecurringPlannedActivities,
    }

    private readonly Stack<MainSurface> navigationStack = new();
    private MainSurface activeSurface = MainSurface.TodayDashboard;

    public bool IsRecurringPlannedActivityManagerOpen => RecurringPlannedActivityManager is not null;

    public bool IsPlannedActivityManagerOpen => PlannedActivityManager is not null;

    public bool IsPlannedActivityManagerVisible =>
        activeSurface == MainSurface.PlannedActivities
        && PlannedActivityManager is not null;

    public bool IsRecurringPlannedActivityManagerVisible =>
        activeSurface == MainSurface.RecurringPlannedActivities
        && RecurringPlannedActivityManager is not null;

    public ObservableCollection<BodyMeasurementListItemViewModel> BodyMeasurements { get; } = [];

    private const int CollapsedBodyMeasurementCount = 5;
    public ObservableCollection<BodyMeasurementListItemViewModel> VisibleBodyMeasurements { get; } = [];

    public bool HasBodyMeasurements => BodyMeasurements.Count > 0;

    public bool HasNoBodyMeasurements => BodyMeasurements.Count == 0;

    public bool CanToggleBodyMeasurementHistory => BodyMeasurements.Count > CollapsedBodyMeasurementCount;

    public string BodyMeasurementHistoryToggleText => IsBodyMeasurementHistoryExpanded
        ? "Свернуть историю"
        : $"Показать все измерения ({BodyMeasurements.Count})";

    public bool IsProfileEditorOpen => ProfileEditor is not null;
    public bool IsProfileEditorVisible =>
        activeSurface == MainSurface.ProfileEditor
        && ProfileEditor is not null;

    public bool IsFoodLogEditorOpen => FoodLogEditor is not null;
    public bool IsFoodLogEditorVisible =>
        activeSurface == MainSurface.FoodLogEditor
        && FoodLogEditor is not null;

    public bool IsProductCatalogOpen => ProductCatalogManager is not null;
    public bool IsProductCatalogVisible =>
        activeSurface == MainSurface.ProductCatalog
        && ProductCatalogManager is not null;

    public bool IsDailyJournalHistoryOpen => DailyJournalHistory is not null;
    public bool IsDailyJournalHistoryVisible =>
        activeSurface == MainSurface.DailyJournalHistory
        && DailyJournalHistory is not null;

    public bool IsCookingSessionManagerOpen => CookingSessionManager is not null;
    public bool IsCookingSessionManagerVisible =>
        activeSurface == MainSurface.CookingSessions
        && CookingSessionManager is not null;

    public bool IsFridgeOpen => FridgeManager is not null;
    public bool IsFridgeVisible =>
        activeSurface == MainSurface.Fridge
        && FridgeManager is not null;

    public bool IsActivityEditorOpen => ActivityEditor is not null;
    public bool IsActivityEditorVisible =>
        activeSurface == MainSurface.ActivityEditor
        && ActivityEditor is not null;

    public ObservableCollection<PlannedActivityItemViewModel> TodayPlannedActivities { get; } = [];
    public ObservableCollection<RecurringPlannedActivityOccurrenceItemViewModel> TodayRecurringPlannedActivities { get; } = [];
    public bool HasTodayRecurringPlannedActivities => TodayRecurringPlannedActivities.Count > 0;
    public bool HasTodayPlannedActivities => TodayPlannedActivities.Count > 0;

    private delegate IAdaptiveEnergyAssessmentPresentationProvider AdaptiveEnergyAssessmentPresentationProviderFactory(
        BodyMeasurementAwareNutritionProfileProvider profileProvider,
        IFoodDiaryStore foodDiaryStore,
        IActivityStore activityStore
    );

    public MainViewModel() : this(
        JsonBodyMeasurementStore.CreateDefault(),
        JsonUserNutritionProfileStore.CreateDefault(),
        JsonFoodDiaryStore.CreateDefault(),
        JsonProductCatalogStore.CreateDefault(),
        CreatePersistentAdaptiveProvider,
        new SystemCurrentDateProvider(),
        JsonCookingSessionStore.CreateDefault(),
        JsonFridgeStore.CreateDefault(),
        JsonCookingBatchStore.CreateDefault(),
        JsonActivityStore.CreateDefault(),
        JsonActivityPresetStore.CreateDefault(),
        JsonPlannedActivityStore.CreateDefault(),
        JsonRecurringPlannedActivityStore.CreateDefault(),
        JsonMealPlanStore.CreateDefault()
    ) { }

    public MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore
    ) : this(
        bodyMeasurementStore,
        CreateInMemoryProfileStore(),
        new InMemoryFoodDiaryStore(),
        new InMemoryProductCatalogStore(),
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
        new InMemoryProductCatalogStore(),
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
        new InMemoryProductCatalogStore(),
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
        new InMemoryProductCatalogStore(),
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
        new InMemoryProductCatalogStore(),
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
        new InMemoryProductCatalogStore(),
        CreateInMemoryAdaptiveProvider,
        currentDateProvider
    ) { }

    public MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore,
        IUserNutritionProfileStore profileStore,
        IFoodDiaryStore foodDiaryStore,
        IProductCatalogStore productCatalogStore,
        ICurrentDateProvider currentDateProvider
    ) : this(
        bodyMeasurementStore,
        profileStore,
        foodDiaryStore,
        productCatalogStore,
        CreateInMemoryAdaptiveProvider,
        currentDateProvider
    ) { }

    public MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore,
        IUserNutritionProfileStore profileStore,
        IFoodDiaryStore foodDiaryStore,
        IProductCatalogStore productCatalogStore,
        ICookingSessionStore cookingSessionStore,
        ICurrentDateProvider currentDateProvider
    ) : this(
        bodyMeasurementStore,
        profileStore,
        foodDiaryStore,
        productCatalogStore,
        CreateInMemoryAdaptiveProvider,
        currentDateProvider,
        cookingSessionStore
    ) { }

    public MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore,
        IUserNutritionProfileStore profileStore,
        IFoodDiaryStore foodDiaryStore,
        IProductCatalogStore productCatalogStore,
        ICookingSessionStore cookingSessionStore,
        IFridgeStore fridgeStore,
        ICurrentDateProvider currentDateProvider
    ) : this(
        bodyMeasurementStore,
        profileStore,
        foodDiaryStore,
        productCatalogStore,
        CreateInMemoryAdaptiveProvider,
        currentDateProvider,
        cookingSessionStore,
        fridgeStore
    ) { }

    public MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore,
        IUserNutritionProfileStore profileStore,
        IFoodDiaryStore foodDiaryStore,
        IActivityStore activityStore,
        ICurrentDateProvider currentDateProvider
    ) : this(
        bodyMeasurementStore,
        profileStore,
        foodDiaryStore,
        new InMemoryProductCatalogStore(),
        CreateInMemoryAdaptiveProvider,
        currentDateProvider,
        activityStore: activityStore
    ) { }

    private MainViewModel(
        IBodyMeasurementStore bodyMeasurementStore,
        IUserNutritionProfileStore profileStore,
        IFoodDiaryStore foodDiaryStore,
        IProductCatalogStore productCatalogStore,
        AdaptiveEnergyAssessmentPresentationProviderFactory adaptiveEnergyAssessmentPresentationProviderFactory,
        ICurrentDateProvider currentDateProvider,
        ICookingSessionStore? cookingSessionStore = null,
        IFridgeStore? fridgeStore = null,
        ICookingBatchStore? cookingBatchStore = null,
        IActivityStore? activityStore = null,
        IActivityPresetStore? activityPresetStore = null,
        IPlannedActivityStore? plannedActivityStore = null,
        IRecurringPlannedActivityStore? recurringPlannedActivityStore = null,
        IMealPlanStore? mealPlanStore = null
    ) {
        ArgumentNullException.ThrowIfNull(bodyMeasurementStore);
        ArgumentNullException.ThrowIfNull(profileStore);
        ArgumentNullException.ThrowIfNull(adaptiveEnergyAssessmentPresentationProviderFactory);
        ArgumentNullException.ThrowIfNull(currentDateProvider);
        ArgumentNullException.ThrowIfNull(foodDiaryStore);
        ArgumentNullException.ThrowIfNull(productCatalogStore);

        if(profileStore is not IUserNutritionProfileWriter profileWriter) {
            throw new ArgumentException(
                "Profile store must implement IUserNutritionProfileWriter.",
                nameof(profileStore)
            );
        }

        this.currentDateProvider = currentDateProvider;
        this.foodDiaryStore = foodDiaryStore;
        this.activityStore = activityStore ?? new InMemoryActivityStore();

        activityEditorService = new ActivityEditorService(this.activityStore);
        activityPresetCatalogService = new ActivityPresetCatalogService(
            activityPresetStore ?? new InMemoryActivityPresetStore()
        );

        var resolvedFridgeStore = fridgeStore ?? new InMemoryFridgeStore();
        foodLogEditorService = new FoodLogEditorService(foodDiaryStore, resolvedFridgeStore);
        fridgeInventoryService = new FridgeInventoryService(resolvedFridgeStore);
        foodDiaryDaySnapshotProvider = new FoodDiaryDaySnapshotProvider(foodDiaryStore);
        dailyJournalSnapshotProvider = new DailyJournalDaySnapshotProvider(
            foodDiaryDaySnapshotProvider,
            this.activityStore
        );
        productCatalogService = new ProductCatalogService(productCatalogStore);
        var resolvedCookingSessionStore = cookingSessionStore ?? new InMemoryCookingSessionStore();
        var resolvedCookingBatchStore = cookingBatchStore ?? new InMemoryCookingBatchStore();
        cookingSessionService = new CookingSessionService(resolvedCookingSessionStore);
        cookingExecutionService = new CookingExecutionService(
            cookingSessionStore: resolvedCookingSessionStore,
            cookingBatchStore: resolvedCookingBatchStore,
            fridgeStore: resolvedFridgeStore
        );

        profileEditorService = new UserNutritionProfileEditorService(
            profileStore: profileStore,
            profileWriter: profileWriter
        );

        bodyMeasurementHistoryService = new BodyMeasurementHistoryService(bodyMeasurementStore);
        activityEnergySuggestionService = new ActivityEnergySuggestionService(bodyMeasurementHistoryService);
        activityRepeatService = new ActivityRepeatService(
            this.activityStore,
            activityPresetCatalogService,
            activityEnergySuggestionService
        );
        recentActivityService = new RecentActivityService(this.activityStore);
        weeklyJournalSummaryProvider = new WeeklyJournalSummaryProvider(
            dailyJournalSnapshotProvider,
            bodyMeasurementHistoryService
        );

        var resolvedPlannedActivityStore = plannedActivityStore ?? new InMemoryPlannedActivityStore();

        var completionDraftFactory = new PlannedActivityCompletionDraftFactory(
            activityPresetCatalogService,
            activityEnergySuggestionService
        );

        plannedActivityCompletionService = new PlannedActivityCompletionService(
            resolvedPlannedActivityStore,
            completionDraftFactory
        );

        var resolvedRecurringStore = recurringPlannedActivityStore ?? new InMemoryRecurringPlannedActivityStore();

        recurringPlannedActivityService = new RecurringPlannedActivityService(resolvedRecurringStore);

        recurringPlannedActivityCompletionService = new RecurringPlannedActivityCompletionService(
            recurringPlannedActivityService,
            completionDraftFactory
        );

        plannedActivityService = new PlannedActivityService(resolvedPlannedActivityStore);
        mealPlanService = new MealPlanService(
            mealPlanStore ?? new InMemoryMealPlanStore()
        );

        bodyMeasurementEditorService = new BodyMeasurementEditorService(bodyMeasurementHistoryService);

        currentProfileProvider = new BodyMeasurementAwareNutritionProfileProvider(
            baseProfileProvider: profileStore,
            measurementHistoryService: bodyMeasurementHistoryService,
            currentDateProvider: currentDateProvider
        );

        todayProvider = new TodayDashboardSnapshotProvider(
            profileProvider: currentProfileProvider,
            dailyJournalSnapshotProvider: dailyJournalSnapshotProvider,
            currentDateProvider: currentDateProvider
        );

        adaptiveEnergyAssessmentPresentationProvider = adaptiveEnergyAssessmentPresentationProviderFactory(
            currentProfileProvider,
            foodDiaryStore,
            this.activityStore
        );

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

        adaptiveEnergyAssessment = AdaptiveEnergyAssessmentViewModel.CreateUnavailable("Адаптивная оценка ещё не рассчитана.");

        today = CreateTodayDashboardViewModel();
        RefreshTodayPlannedActivities();
        RefreshTodayRecurringPlannedActivities();
        RefreshAfterBodyMeasurementChange();
    }

    public bool IsGoalEditorOpen => GoalEditor is not null;
    public bool IsGoalEditorVisible =>
        activeSurface == MainSurface.GoalEditor
        && GoalEditor is not null;

    public bool IsBodyMeasurementEditorOpen => BodyMeasurementEditor is not null;
    public bool IsBodyMeasurementEditorVisible =>
        activeSurface == MainSurface.BodyMeasurementEditor
        && BodyMeasurementEditor is not null;

    public bool IsTodayDashboardVisible => activeSurface == MainSurface.TodayDashboard;

    private void NavigateTo(MainSurface surface) {
        if(activeSurface == surface) {
            NotifyNavigationVisibilityChanged();
            return;
        }

        RemoveFromNavigationStack(surface);

        if(IsSurfaceAvailable(activeSurface)) {
            navigationStack.Push(activeSurface);
        }

        activeSurface = surface;
        NotifyNavigationVisibilityChanged();
    }

    private void CloseSurface(MainSurface surface) {
        RemoveFromNavigationStack(surface);

        if(activeSurface != surface) {
            NotifyNavigationVisibilityChanged();
            return;
        }

        activeSurface = PopAvailableSurface();
        NotifyNavigationVisibilityChanged();
    }

    private void UpdateSurface(MainSurface surface, bool isOpen) {
        if(isOpen) {
            NavigateTo(surface);
        }
        else {
            CloseSurface(surface);
        }
    }

    private MainSurface PopAvailableSurface() {
        while(navigationStack.TryPop(out var surface)) {
            if(IsSurfaceAvailable(surface)) {
                return surface;
            }
        }

        return MainSurface.TodayDashboard;
    }

    private void RemoveFromNavigationStack(MainSurface surface) {
        if(navigationStack.Count == 0) {
            return;
        }

        var remaining = navigationStack
            .Where(item => item != surface)
            .Reverse()
            .ToArray();

        navigationStack.Clear();

        foreach(var item in remaining) {
            navigationStack.Push(item);
        }
    }

    private bool IsSurfaceAvailable(MainSurface surface) {
        return surface switch {
            MainSurface.TodayDashboard => true,
            MainSurface.GoalEditor => GoalEditor is not null,
            MainSurface.BodyMeasurementEditor => BodyMeasurementEditor is not null,
            MainSurface.ProfileEditor => ProfileEditor is not null,
            MainSurface.FoodLogEditor => FoodLogEditor is not null,
            MainSurface.ProductCatalog => ProductCatalogManager is not null,
            MainSurface.DailyJournalHistory => DailyJournalHistory is not null,
            MainSurface.CookingSessions => CookingSessionManager is not null,
            MainSurface.Fridge => FridgeManager is not null,
            MainSurface.ActivityEditor => ActivityEditor is not null,
            MainSurface.PlannedActivities => PlannedActivityManager is not null,
            MainSurface.RecurringPlannedActivities => RecurringPlannedActivityManager is not null,
            _ => false,
        };
    }

    private void NotifyNavigationVisibilityChanged() {
        OnPropertyChanged(nameof(IsTodayDashboardVisible));
        OnPropertyChanged(nameof(IsGoalEditorVisible));
        OnPropertyChanged(nameof(IsBodyMeasurementEditorVisible));
        OnPropertyChanged(nameof(IsProfileEditorVisible));
        OnPropertyChanged(nameof(IsFoodLogEditorVisible));
        OnPropertyChanged(nameof(IsProductCatalogVisible));
        OnPropertyChanged(nameof(IsDailyJournalHistoryVisible));
        OnPropertyChanged(nameof(IsCookingSessionManagerVisible));
        OnPropertyChanged(nameof(IsFridgeVisible));
        OnPropertyChanged(nameof(IsActivityEditorVisible));
        OnPropertyChanged(nameof(IsPlannedActivityManagerVisible));
        OnPropertyChanged(nameof(IsRecurringPlannedActivityManagerVisible));
    }

    partial void OnRecurringPlannedActivityManagerChanged(
        RecurringPlannedActivityManagerViewModel? value
    ) {
        OnPropertyChanged(nameof(IsRecurringPlannedActivityManagerOpen));
        UpdateSurface(MainSurface.RecurringPlannedActivities, value is not null);
    }

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
        ProfileEditor = new UserNutritionProfileEditorViewModel(
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

    [RelayCommand]
    private void OpenProductCatalog() {
        ProductCatalogManager = new ProductCatalogManagerViewModel(
            productCatalogService: productCatalogService,
            onClosed: CloseProductCatalog
        );
    }

    [RelayCommand]
    private void OpenCookingSessions() {
        CookingSessionManager = new CookingSessionManagerViewModel(
            cookingSessionService: cookingSessionService,
            cookingExecutionService: cookingExecutionService,
            productCatalogService: productCatalogService,
            fridgeInventoryService: fridgeInventoryService,
            currentDate: currentDateProvider.GetCurrentDate(),
            onClosed: CloseCookingSessions
        );
    }

    [RelayCommand]
    private void OpenFridge() {
        FridgeManager = new FridgeManagerViewModel(
            fridgeInventoryService: fridgeInventoryService,
            productCatalogService: productCatalogService,
            currentDate: currentDateProvider.GetCurrentDate(),
            logFood: OpenFridgeFoodLog,
            onClosed: CloseFridge,
            mealPlanService: mealPlanService
        );
    }

    [RelayCommand]
    private void OpenDailyJournalHistory() {
        DailyJournalHistory = new DailyJournalHistoryViewModel(
            snapshotProvider: dailyJournalSnapshotProvider,
            weeklySummaryProvider: weeklyJournalSummaryProvider,
            currentDate: currentDateProvider.GetCurrentDate(),
            addFood: OpenFoodLogEditorForDate,
            addApproximateFood: OpenApproximateFoodLogEditorForDate,
            editFood: EditFoodLog,
            deleteFood: DeleteFoodLog,
            setFoodLogComplete: SetFoodLogComplete,
            addActivity: OpenActivityEditorForDate,
            editActivity: EditActivity,
            deleteActivity: DeleteActivity,
            onClosed: CloseDailyJournalHistory,
            repeatActivity: RepeatActivity,
            recurringPlannedActivityService: recurringPlannedActivityService,
            editRecurringPlannedActivity: OpenRecurringPlannedActivityEditor,
            completeRecurringPlannedActivity: CompleteRecurringPlannedActivity,
            skipRecurringPlannedActivity: SkipRecurringPlannedActivity
        );
    }

    [RelayCommand]
    private void OpenPlannedActivities() {
        PlannedActivityManager = new PlannedActivityManagerViewModel(
            plannedActivityService,
            activityPresetCatalogService,
            currentDateProvider.GetCurrentDate(),
            CompletePlannedActivity,
            OpenRecurringPlannedActivities,
            ClosePlannedActivities,
            RefreshAfterPlannedActivityChange
        );
    }

    private void ClosePlannedActivities() {
        PlannedActivityManager = null;
    }

    private void CompletePlannedActivity(Guid id) {
        var draft = plannedActivityCompletionService.CreateCompletionDraft(
            id,
            currentDateProvider.GetCurrentDate()
        );

        if(draft is null) {
            PlannedActivityManager?.Refresh();
            return;
        }

        OpenActivityEditor(
            draft,
            true,
            () => {
                if(plannedActivityService.Delete(id)) {
                    RefreshAfterPlannedActivityChange();
                }

                PlannedActivityManager?.Refresh();
            }
        );
    }

    partial void OnPlannedActivityManagerChanged(PlannedActivityManagerViewModel? value) {
        OnPropertyChanged(nameof(IsPlannedActivityManagerOpen));
        UpdateSurface(MainSurface.PlannedActivities, value is not null);
    }

    private void CloseDailyJournalHistory() {
        DailyJournalHistory = null;
    }

    partial void OnDailyJournalHistoryChanged(DailyJournalHistoryViewModel? value) {
        OnPropertyChanged(nameof(IsDailyJournalHistoryOpen));
        UpdateSurface(MainSurface.DailyJournalHistory, value is not null);
    }

    private void OpenFridgeFoodLog(Guid fridgeItemId) {
        var draft = fridgeInventoryService.CreateFoodLogDraft(
            fridgeItemId,
            currentDateProvider.GetCurrentDate()
        );

        if(draft is null) {
            FridgeManager?.RefreshItems();
            return;
        }

        OpenFoodLogEditor(draft);
    }

    private void CloseFridge() {
        FridgeManager = null;
    }

    partial void OnFridgeManagerChanged(FridgeManagerViewModel? value) {
        OnPropertyChanged(nameof(IsFridgeOpen));
        UpdateSurface(MainSurface.Fridge, value is not null);
    }

    private void CloseCookingSessions() {
        CookingSessionManager = null;
    }

    partial void OnCookingSessionManagerChanged(CookingSessionManagerViewModel? value) {
        OnPropertyChanged(nameof(IsCookingSessionManagerOpen));
        UpdateSurface(MainSurface.CookingSessions, value is not null);
    }


    private void CloseFoodDiaryHistory() {
        DailyJournalHistory = null;
    }

    private void CloseProductCatalog() {
        ProductCatalogManager = null;
    }

    partial void OnProductCatalogManagerChanged(ProductCatalogManagerViewModel? value) {
        OnPropertyChanged(nameof(IsProductCatalogOpen));
        UpdateSurface(MainSurface.ProductCatalog, value is not null);
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
        BodyTrends = BodyTrendsViewModelFactory.Create(measurementSnapshot);
    }

    private TodayDashboardViewModel CreateTodayDashboardViewModel(string? actionSummary = null) {
        var snapshot = todayProvider.GetToday();

        return new TodayDashboardViewModel(
            snapshot: snapshot,
            tryExecuteGoalAction: TryExecuteGoalAction,
            addFood: OpenFoodLogEditor,
            addApproximateFood: OpenApproximateFoodLogEditor,
            setFoodLogComplete: SetTodayFoodLogComplete,
            editFood: EditFoodLog,
            deleteFood: DeleteFoodLog,
            initialGoalActionSummary: actionSummary,
            addActivity: OpenActivityEditor,
            editActivity: EditActivity,
            deleteActivity: DeleteActivity
        );
    }

    private void OpenFoodLogEditor() {
        OpenFoodLogEditorForDate(currentDateProvider.GetCurrentDate());
    }

    private void OpenFoodLogEditorForDate(DateOnly date) {
        OpenFoodLogEditor(foodLogEditorService.CreateNew(date));
    }

    private void OpenFoodLogEditor(
        FoodLogDraft draft,
        bool isQuickApproximation = false
    ) {
        var currentDate = currentDateProvider.GetCurrentDate();

        FoodLogEditor = new FoodLogEditorViewModel(
            editorService: foodLogEditorService,
            productCatalogService: productCatalogService,
            draft: draft,
            currentDate: currentDate,
            onSaved: OnFoodLogSaved,
            onCancelled: CloseFoodLogEditor,
            isQuickApproximation: isQuickApproximation
        );
    }

    private void OnFoodLogSaved() {
        FoodLogEditor = null;
        RefreshAfterFoodDiaryChange();
    }

    private void CloseFoodLogEditor() {
        FoodLogEditor = null;
    }

    partial void OnFoodLogEditorChanged(FoodLogEditorViewModel? value) {
        OnPropertyChanged(nameof(IsFoodLogEditorOpen));
        UpdateSurface(MainSurface.FoodLogEditor, value is not null);
    }

    private void OpenApproximateFoodLogEditor() {
        OpenApproximateFoodLogEditorForDate(currentDateProvider.GetCurrentDate());
    }

    private void OpenApproximateFoodLogEditorForDate(DateOnly date) {
        OpenFoodLogEditor(
            draft: foodLogEditorService.CreateNewApproximation(date),
            isQuickApproximation: true
        );
    }

    private void SetTodayFoodLogComplete(bool isComplete) {
        SetFoodLogComplete(
            currentDateProvider.GetCurrentDate(),
            isComplete
        );
    }

    private void SetFoodLogComplete(
        DateOnly date,
        bool isComplete
    ) {
        foodDiaryStore.SetDateComplete(date, isComplete);

        RefreshAfterFoodDiaryChange();
    }

    private void RefreshAfterFoodDiaryChange() {
        Today = CreateTodayDashboardViewModel();
        DailyJournalHistory?.Refresh();
        FridgeManager?.RefreshItems();
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
                OpenGoalEditor(goalEditorService.CreateNewGoal(WeightGoalType.LoseWeight));
                return true;
            case GoalNextAction.StartWeightGain:
                OpenGoalEditor(goalEditorService.CreateNewGoal(WeightGoalType.GainWeight));
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
        OpenGoalEditor(goalEditorService.LoadCurrentGoal());
    }

    private void OpenGoalEditor(NutritionGoalDraft draft) {
        GoalEditor = new NutritionGoalEditorViewModel(
            editorService: goalEditorService,
            draft: draft,
            onSaved: OnGoalEditorSaved,
            onCancelled: CloseGoalEditor
        );
    }

    private void OnGoalEditorSaved() {
        GoalEditor = null;

        Today = CreateTodayDashboardViewModel("Цель сохранена. Дневная норма КБЖУ пересчитана.");

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
        UpdateSurface(MainSurface.GoalEditor, value is not null);
    }

    partial void OnBodyMeasurementEditorChanged(BodyMeasurementEditorViewModel? value) {
        OnPropertyChanged(nameof(IsBodyMeasurementEditorOpen));
        UpdateSurface(MainSurface.BodyMeasurementEditor, value is not null);
    }

    private void RefreshAdaptiveEnergyAssessment() {
        RefreshAdaptiveEnergyAssessment(GetCurrentBodyMeasurementSnapshot());
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
        IFoodDiaryStore foodDiaryStore,
        IActivityStore activityStore
    ) {
        return CreateAdaptiveProvider(
            JsonAdaptiveEnergyEvaluationStore.CreateDefault(),
            foodDiaryStore,
            activityStore,
            profileProvider
        );
    }

    private static IAdaptiveEnergyAssessmentPresentationProvider CreateInMemoryAdaptiveProvider(
        BodyMeasurementAwareNutritionProfileProvider profileProvider,
        IFoodDiaryStore foodDiaryStore,
        IActivityStore activityStore
    ) {
        return CreateAdaptiveProvider(
            new InMemoryAdaptiveEnergyEvaluationStore(),
            foodDiaryStore,
            activityStore,
            profileProvider
        );
    }

    private static IAdaptiveEnergyAssessmentPresentationProvider CreateAdaptiveProvider(
        IAdaptiveEnergyEvaluationStore evaluationStore,
        IFoodDiaryStore foodDiaryStore,
        IActivityStore activityStore,
        BodyMeasurementAwareNutritionProfileProvider profileProvider
    ) {
        return new AdaptiveEnergyAssessmentPresentationProvider(
            new AdaptiveEnergyAssessmentService(evaluationStore),
            new DailyEnergyIntakeHistoryProvider(foodDiaryStore, activityStore),
            profileProvider
        );
    }

    private static AdaptiveEnergyAssessmentPresentationProviderFactory CreateInjectedAdaptiveProviderFactory(
        IAdaptiveEnergyAssessmentPresentationProvider adaptiveEnergyAssessmentPresentationProvider
    ) {
        ArgumentNullException.ThrowIfNull(adaptiveEnergyAssessmentPresentationProvider);

        return (_, _, _) => adaptiveEnergyAssessmentPresentationProvider;
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
        UpdateSurface(MainSurface.ProfileEditor, value is not null);
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
        DailyJournalHistory?.Refresh();
        RefreshAdaptiveEnergyAssessment(measurementSnapshot);
    }

    private void RefreshBodyMeasurementDerivedState(BodyMeasurementHistorySnapshot measurementSnapshot) {
        RefreshProfileSummary(measurementSnapshot);
        RefreshBodyTrends(measurementSnapshot);
    }

    private BodyMeasurementHistorySnapshot GetCurrentBodyMeasurementSnapshot() {
        return bodyMeasurementHistoryService.GetSnapshot(currentDateProvider.GetCurrentDate());
    }

    private void EditFoodLog(Guid id) {
        var draft = foodLogEditorService.Load(id);

        if(draft is null) {
            RefreshAfterFoodDiaryChange();
            return;
        }

        OpenFoodLogEditor(draft);
    }

    private void DeleteFoodLog(Guid id) {
        if(!foodLogEditorService.Delete(id)) {
            RefreshAfterFoodDiaryChange();
            return;
        }

        RefreshAfterFoodDiaryChange();
    }

    private void OpenActivityEditor() {
        OpenActivityEditorForDate(currentDateProvider.GetCurrentDate());
    }

    private void OpenActivityEditorForDate(DateOnly date) {
        OpenActivityEditor(activityEditorService.CreateNew(date), isNew: true);
    }

    private void EditActivity(Guid id) {
        var draft = activityEditorService.Load(id);

        if(draft is null) {
            RefreshAfterActivityChange();
            return;
        }

        OpenActivityEditor(draft, isNew: false);
    }

    private void OpenActivityEditor(
        ActivityDraft draft,
        bool isNew,
        Action? afterSaved = null
    ) {
        ActivityEditor = new ActivityEditorViewModel(
            activityEditorService,
            draft,
            draft.Date,
            isNew,
            () => {
                OnActivitySaved();
                afterSaved?.Invoke();
            },
            CloseActivityEditor,
            activityPresetCatalogService,
            activityEnergySuggestionService,
            recentActivityService,
            activityRepeatService
        );
    }

    private void OnActivitySaved() {
        ActivityEditor = null;

        RefreshAfterActivityChange();
    }

    private void CloseActivityEditor() {
        ActivityEditor = null;
    }

    private void DeleteActivity(Guid id) {
        if(!activityEditorService.Delete(id)) {
            return;
        }

        RefreshAfterActivityChange();
    }

    private void RefreshAfterActivityChange() {
        Today = CreateTodayDashboardViewModel();
        DailyJournalHistory?.Refresh();
        RefreshAdaptiveEnergyAssessment();
    }

    partial void OnActivityEditorChanged(ActivityEditorViewModel? value) {
        OnPropertyChanged(nameof(IsActivityEditorOpen));
        UpdateSurface(MainSurface.ActivityEditor, value is not null);
    }

    private void RepeatActivity(Guid id) {
        var draft = activityRepeatService.CreateDraft(
            id,
            currentDateProvider.GetCurrentDate()
        );

        if(draft is null) {
            DailyJournalHistory?.Refresh();
            return;
        }

        OpenActivityEditor(draft, isNew: true);
    }

    private void RefreshTodayPlannedActivities() {
        TodayPlannedActivities.Clear();

        var currentDate = currentDateProvider.GetCurrentDate();

        foreach(var activity in plannedActivityService.Get(currentDate)) {
            TodayPlannedActivities.Add(
                new PlannedActivityItemViewModel(
                    activity,
                    currentDate,
                    OpenPlannedActivityEditor,
                    CompletePlannedActivity,
                    DeletePlannedActivity,
                    showDate: false
                )
            );
        }

        OnPropertyChanged(nameof(HasTodayPlannedActivities));
    }

    private void OpenPlannedActivityEditor(Guid id) {
        OpenPlannedActivities();
        PlannedActivityManager?.OpenEditor(id);
    }

    private void DeletePlannedActivity(Guid id) {
        if(plannedActivityService.Delete(id)) {
            RefreshAfterPlannedActivityChange();
        }
    }

    private void RefreshAfterPlannedActivityChange() {
        RefreshTodayPlannedActivities();
        DailyJournalHistory?.Refresh();
    }

    private void OpenRecurringPlannedActivities() {
        RecurringPlannedActivityManager =
            new RecurringPlannedActivityManagerViewModel(
                recurringPlannedActivityService,
                activityPresetCatalogService,
                currentDateProvider.GetCurrentDate(),
                RefreshAfterRecurringPlannedActivityChange,
                CloseRecurringPlannedActivities
            );
    }

    private void OpenRecurringPlannedActivityEditor(Guid scheduleId) {
        OpenRecurringPlannedActivities();
        RecurringPlannedActivityManager?.OpenEditor(scheduleId);
    }

    private void CloseRecurringPlannedActivities() {
        RecurringPlannedActivityManager = null;
    }

    private void CompleteRecurringPlannedActivity(
        Guid scheduleId,
        DateOnly occurrenceDate
    ) {
        var draft = recurringPlannedActivityCompletionService.CreateCompletionDraft(
        scheduleId,
        occurrenceDate
    );

        if(draft is null) {
            RefreshAfterRecurringPlannedActivityChange();
            return;
        }

        OpenActivityEditor(
            draft,
            true,
            () => {
                recurringPlannedActivityService.CompleteOccurrence(
                    scheduleId,
                    occurrenceDate,
                    draft.Id
                );

                RefreshAfterRecurringPlannedActivityChange();
            }
        );
    }

    private void SkipRecurringPlannedActivity(
        Guid scheduleId,
        DateOnly occurrenceDate
    ) {
        recurringPlannedActivityService.SkipOccurrence(scheduleId, occurrenceDate);
        RefreshAfterRecurringPlannedActivityChange();
    }

    private void RefreshTodayRecurringPlannedActivities() {
        TodayRecurringPlannedActivities.Clear();

        var currentDate = currentDateProvider.GetCurrentDate();

        foreach(var occurrence in recurringPlannedActivityService.GetOccurrences(currentDate)) {
            TodayRecurringPlannedActivities.Add(
                new RecurringPlannedActivityOccurrenceItemViewModel(
                    occurrence,
                    currentDate,
                    OpenRecurringPlannedActivityEditor,
                    CompleteRecurringPlannedActivity,
                    SkipRecurringPlannedActivity
                )
            );
        }

        OnPropertyChanged(nameof(HasTodayRecurringPlannedActivities));
    }

    private void RefreshAfterRecurringPlannedActivityChange() {
        RefreshTodayRecurringPlannedActivities();
        DailyJournalHistory?.Refresh();
    }
}
