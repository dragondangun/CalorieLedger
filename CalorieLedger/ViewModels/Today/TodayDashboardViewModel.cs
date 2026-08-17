using CalorieLedger.Application.Today;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Meals;

namespace CalorieLedger.ViewModels.Today;

public sealed partial class TodayDashboardViewModel:ObservableObject {
    public decimal TargetCaloriesKcal => target.CaloriesKcal;

    public decimal? TargetProteinG => target.ProteinG;

    public decimal? TargetFatG => target.FatG;

    public decimal? TargetCarbsG => target.CarbsG;

    public string GoalStatusSummary { get; }

    public string GoalDetailsSummary { get; }

    public ObservableCollection<TodayGoalActionViewModel> GoalActions { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RemainingCaloriesKcal))]
    [NotifyPropertyChangedFor(nameof(CaloriesSummary))]
    [NotifyPropertyChangedFor(nameof(RemainingCaloriesSummary))]
    private decimal consumedCaloriesKcal;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MacrosSummary))]
    private decimal proteinG;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MacrosSummary))]
    private decimal fatG;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MacrosSummary))]
    private decimal carbsG;

    [ObservableProperty]
    private string goalActionSelectionSummary = string.Empty;

    public ObservableCollection<FoodDiaryMealGroupViewModel> MealGroups { get; } = [];

    public decimal RemainingCaloriesKcal => TargetCaloriesKcal - ConsumedCaloriesKcal;

    public string CaloriesSummary => $"{ConsumedCaloriesKcal:0} / {TargetCaloriesKcal:0} ккал";

    public string RemainingCaloriesSummary => RemainingCaloriesKcal >= 0
            ? $"Осталось {RemainingCaloriesKcal:0} ккал"
            : $"Превышение {-RemainingCaloriesKcal:0} ккал";

    public string MacrosSummary => $"Б: {ProteinG:0.#}/{FormatTarget(TargetProteinG)} г · Ж: {FatG:0.#}/{FormatTarget(TargetFatG)} г · У: {CarbsG:0.#}/{FormatTarget(TargetCarbsG)} г";

    private readonly DailyNutritionTarget target;

    private readonly WeeklyNutritionSummarySnapshot weeklySummary;

    public ObservableCollection<TodayActivityItemViewModel> Activities { get; } = [];

    private readonly Func<GoalNextAction, bool> tryExecuteGoalAction;

    public decimal ActivityBurnedCaloriesKcal => Activities.Sum(x => x.BurnedCaloriesKcal);

    public string ActivitySummary => ActivityBurnedCaloriesKcal > 0m
        ? $"Потрачено дополнительно: {ActivityBurnedCaloriesKcal:0} ккал"
        : "Дополнительная активность не указана";

    private readonly Action addFood;
    private readonly Action addApproximateFood;
    private readonly Action<bool> setFoodLogComplete;
    private readonly Action<Guid> editFood;
    private readonly Action<Guid> deleteFood;

    public bool IsFoodLogComplete { get; }

    public string FoodLogCompletionActionText => IsFoodLogComplete
        ? "Открыть день снова"
        : "Завершить день";

    public TodayDashboardViewModel(
        TodayDashboardSnapshot snapshot,
        Func<GoalNextAction, bool> tryExecuteGoalAction,
        Action addFood,
        Action addApproximateFood,
        Action<bool> setFoodLogComplete,
        Action<Guid> editFood,
        Action<Guid> deleteFood,
        string? initialGoalActionSummary = null
    ) {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(tryExecuteGoalAction);
        ArgumentNullException.ThrowIfNull(addFood);
        ArgumentNullException.ThrowIfNull(addApproximateFood);
        ArgumentNullException.ThrowIfNull(setFoodLogComplete);
        ArgumentNullException.ThrowIfNull(editFood);
        ArgumentNullException.ThrowIfNull(deleteFood);

        this.tryExecuteGoalAction = tryExecuteGoalAction;
        this.addFood = addFood;
        this.addApproximateFood = addApproximateFood;
        this.setFoodLogComplete = setFoodLogComplete;
        this.editFood = editFood;
        this.deleteFood = deleteFood;

        IsFoodLogComplete = snapshot.IsFoodLogComplete;

        GoalActionSelectionSummary = initialGoalActionSummary ?? "Выберите дальнейшее действие.";

        target = snapshot.Target;

        weeklySummary = snapshot.WeeklySummary;

        ConsumedCaloriesKcal = snapshot.ConsumedTotals.CaloriesKcal ?? 0m;

        ProteinG = snapshot.ConsumedTotals.ProteinG ?? 0m;
        FatG = snapshot.ConsumedTotals.FatG ?? 0m;
        CarbsG = snapshot.ConsumedTotals.CarbsG ?? 0m;

        foreach(var meal in snapshot.Meals) {
            MealGroups.Add(
                FoodDiaryPresentationFactory.CreateMealGroup(
                    meal:meal,
                    editFood: editFood,
                    deleteFood: deleteFood
                )
            );
        }

        foreach(var activity in snapshot.Activities) {
            Activities.Add(
                new TodayActivityItemViewModel(
                    Name: activity.Name,
                    BurnedCaloriesKcal: activity.BurnedCaloriesKcal,
                    TimeSummary: FormatTime(activity.StartedAt),
                    DurationSummary: FormatDuration(activity.Duration)
                )
            );
        }

        GoalStatusSummary = FormatGoalDecisionStatus(snapshot.GoalDecision.Status);

        GoalDetailsSummary = FormatGoalDecisionDetails(snapshot.GoalDecision);

        foreach(var action in snapshot.GoalDecision.AvailableActions) {
            GoalActions.Add(
                new TodayGoalActionViewModel(
                    action: action,
                    title: FormatGoalAction(action),
                    onSelected: SelectGoalAction
                )
            );
        }
    }

    [RelayCommand]
    private void AddFood() {
        addFood();
    }

    [RelayCommand]
    private void AddApproximateFood() {
        addApproximateFood();
    }

    [RelayCommand]
    private void ToggleFoodLogCompletion() {
        setFoodLogComplete(!IsFoodLogComplete);
    }

    private static string FormatTarget(decimal? value) {
        return value is null ? "—" : $"{value.Value:0.#}";
    }

    private static string FormatTime(TimeOnly? time) {
        return time is null ? "" : time.Value.ToString("HH:mm");
    }

    public string WeeklyCaloriesAverageSummary => $"{weeklySummary.AverageCaloriesKcal:0} / {TargetCaloriesKcal:0} ккал в среднем за 7 дней";

    public string WeeklyMacrosAverageSummary =>
        $"Б: {weeklySummary.AverageProteinG:0.#} г · " +
        $"Ж: {weeklySummary.AverageFatG:0.#} г · " +
        $"У: {weeklySummary.AverageCarbsG:0.#} г";

    public string WeeklyBalanceSummary {
        get {
            var balance = weeklySummary.AverageCaloriesKcal - TargetCaloriesKcal;

            return balance switch {
                > 100m => $"Средний профицит: +{balance:0} ккал/день",
                < -100m => $"Средний дефицит: {balance:0} ккал/день",
                _ => "В среднем около цели"
            };
        }
    }

    private static string FormatDuration(TimeSpan? duration) {
        if(duration is null) {
            return "";
        }

        if(duration.Value.TotalHours >= 1) {
            return $"{duration.Value.TotalHours:0.#} ч";
        }

        return $"{duration.Value.TotalMinutes:0} мин";
    }

    private static string FormatGoalDecisionStatus(NutritionGoalDecisionStatus status) {
        return status switch {
            NutritionGoalDecisionStatus.NotConfigured =>
                "Цель не настроена",

            NutritionGoalDecisionStatus.MeasurementMissing =>
                "Не хватает измерений",

            NutritionGoalDecisionStatus.InProgress =>
                "Цель выполняется",

            NutritionGoalDecisionStatus.PartiallyReached =>
                "Часть цели достигнута",

            NutritionGoalDecisionStatus.GoalReached =>
                "Цель достигнута",

            NutritionGoalDecisionStatus.StopLimitReached =>
                "Достигнут предел текущей фазы",

            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                null)
        };
    }

    private static string FormatGoalDecisionDetails(NutritionGoalDecision decision) {
        return decision.Status switch {
            NutritionGoalDecisionStatus.NotConfigured =>
                "Задайте желаемый вес, процент жира или мышечные показатели.",

            NutritionGoalDecisionStatus.MeasurementMissing =>
                "Для оценки цели нужно обновить измерения тела.",

            NutritionGoalDecisionStatus.InProgress =>
                "Заданные целевые показатели пока не достигнуты.",

            NutritionGoalDecisionStatus.PartiallyReached =>
                "Некоторые показатели уже достигнуты. Можно продолжить текущую цель или изменить стратегию.",

            NutritionGoalDecisionStatus.GoalReached =>
                "Все заданные целевые показатели достигнуты. Выберите дальнейший режим питания.",

            NutritionGoalDecisionStatus.StopLimitReached =>
                FormatStopLimitDetails(decision.StopCondition),

            _ => throw new ArgumentOutOfRangeException(
                nameof(decision.Status),
                decision.Status,
                null)
        };
    }

    private static string FormatStopLimitDetails(
        GoalStopEvaluation stopCondition) {
        if(stopCondition.StopAtBodyFatPercent is null) {
            return "Текущую фазу набора массы следует пересмотреть.";
        }

        return
            $"Процент жира достиг установленного предела " +
            $"{stopCondition.StopAtBodyFatPercent.Value:0.#}%. " +
            "Выберите дальнейший режим.";
    }

    private static string FormatGoalAction(GoalNextAction action) {
        return action switch {
            GoalNextAction.ContinueCurrentGoal =>
                "Продолжить текущую цель",

            GoalNextAction.SwitchToMaintenance =>
                "Перейти на поддержание",

            GoalNextAction.StartWeightLoss =>
                "Начать снижение веса",

            GoalNextAction.StartWeightGain =>
                "Начать набор массы",

            GoalNextAction.SetNewGoal =>
                "Задать новую цель",

            GoalNextAction.RepeatMeasurements =>
                "Повторить измерения",

            _ => throw new ArgumentOutOfRangeException(
                nameof(action),
                action,
                null)
        };
    }

    private void SelectGoalAction(GoalNextAction action) {
        if(tryExecuteGoalAction(action)) {
            return;
        }

        GoalActionSelectionSummary = action switch {
            GoalNextAction.ContinueCurrentGoal =>
                "Текущая цель оставлена без изменений.",

            GoalNextAction.SwitchToMaintenance =>
                "Выбран переход на поддержание. Изменение цели пока не сохранено.",

            GoalNextAction.StartWeightLoss =>
                "Выбрано снижение веса. Далее потребуется настроить параметры новой цели.",

            GoalNextAction.StartWeightGain =>
                "Выбран набор массы. Далее потребуется настроить параметры новой цели.",

            GoalNextAction.SetNewGoal =>
                "Выбрано создание новой цели. Форма настройки будет добавлена позже.",

            GoalNextAction.RepeatMeasurements =>
                "Выбрано обновление измерений тела. Форма измерений будет добавлена позже.",

            _ => throw new ArgumentOutOfRangeException(
                nameof(action),
                action,
                null)
        };
    }
}
