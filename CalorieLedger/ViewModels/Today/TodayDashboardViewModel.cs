using CalorieLedger.Application.Today;
using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Activities;
using CalorieLedger.ViewModels.Meals;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace CalorieLedger.ViewModels.Today;

public sealed partial class TodayDashboardViewModel:ObservableObject {
    public decimal BaseTargetCaloriesKcal => target.CaloriesKcal;

    public decimal TargetCaloriesKcal => BaseTargetCaloriesKcal + ActivityBurnedCaloriesKcal;

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

    public ObservableCollection<ActivityItemViewModel> Activities { get; } = [];

    private readonly Func<GoalNextAction, bool> tryExecuteGoalAction;

    public decimal ActivityBurnedCaloriesKcal => Activities.Sum(x => x.BurnedCaloriesKcal);

    public string ActivitySummary => ActivityBurnedCaloriesKcal > 0m
        ? $"Дополнительная активность: +{ActivityBurnedCaloriesKcal:0} ккал к дневному бюджету"
        : "Дополнительная активность не указана";

    private readonly Action addFood;
    private readonly Action addApproximateFood;
    private readonly Action<bool> setFoodLogComplete;
    private readonly Action<Guid> editFood;
    private readonly Action<Guid> deleteFood;

    private readonly Action addActivity;
    private readonly Action<Guid> editActivity;
    private readonly Action<Guid> deleteActivity;

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
        Action addActivity,
        Action<Guid> editActivity,
        Action<Guid> deleteActivity,
        string? initialGoalActionSummary = null
    ) {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(tryExecuteGoalAction);
        ArgumentNullException.ThrowIfNull(addFood);
        ArgumentNullException.ThrowIfNull(addApproximateFood);
        ArgumentNullException.ThrowIfNull(setFoodLogComplete);
        ArgumentNullException.ThrowIfNull(editFood);
        ArgumentNullException.ThrowIfNull(deleteFood);
        ArgumentNullException.ThrowIfNull(addActivity);
        ArgumentNullException.ThrowIfNull(editActivity);
        ArgumentNullException.ThrowIfNull(deleteActivity);

        this.tryExecuteGoalAction = tryExecuteGoalAction;
        this.addFood = addFood;
        this.addApproximateFood = addApproximateFood;
        this.setFoodLogComplete = setFoodLogComplete;
        this.editFood = editFood;
        this.deleteFood = deleteFood;
        this.addActivity = addActivity;
        this.editActivity = editActivity;
        this.deleteActivity = deleteActivity;

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
                    meal: meal,
                    editFood: editFood,
                    deleteFood: deleteFood
                )
            );
        }

        foreach(var activity in snapshot.Activities) {
            Activities.Add(new ActivityItemViewModel(
                id: activity.Id,
                name: activity.Name,
                burnedCaloriesKcal: activity.BurnedCaloriesKcal,
                startedAt: activity.StartedAt,
                duration: activity.Duration,
                note: activity.Note,
                edit: editActivity,
                delete: deleteActivity)
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
    private void AddActivity() {
        addActivity();
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

    public string WeeklyCaloriesAverageSummary {
        get {
            if(weeklySummary.AverageCaloriesKcal is not decimal averageCalories) {
                return "Средняя калорийность пока недоступна";
            }

            if(weeklySummary.AverageExtraActivityBurnedCaloriesKcal is not > 0m) {
                return $"{averageCalories:0} / {BaseTargetCaloriesKcal:0} ккал в среднем";
            }

            return $"{averageCalories:0} ккал съедено · {weeklySummary.AverageExtraActivityBurnedCaloriesKcal.Value:0} ккал доп. активности";
        }
    }

    public string WeeklyMacrosAverageSummary =>
        weeklySummary.AverageProteinG is decimal averageProteinG
        && weeklySummary.AverageFatG is decimal averageFatG
        && weeklySummary.AverageCarbsG is decimal averageCarbsG
            ? $"Б: {averageProteinG:0.#} г · Ж: {averageFatG:0.#} г · У: {averageCarbsG:0.#} г"
            : "Среднее БЖУ пока недоступно";

    public string WeeklyBalanceSummary {
        get {
            if(weeklySummary.AverageActivityAdjustedCaloriesKcal is not decimal averageCalories) {
                return "Завершите хотя бы один день для расчёта среднего.";
            }

            var balance = averageCalories - BaseTargetCaloriesKcal;

            return balance switch {
                > 100m => $"Средний профицит с учётом активности: +{balance:0} ккал/день",
                < -100m => $"Средний дефицит с учётом активности: {balance:0} ккал/день",
                _ => "В среднем около цели с учётом активности"
            };
        }
    }

    public string WeeklyDataQualitySummary {
        get {
            if(weeklySummary.Days.Count == 0) {
                return "Нет данных за последние дни.";
            }

            if(weeklySummary.EnergyCompleteDayCount == 0) {
                return "Нет завершённых дней с полной калорийностью.";
            }

            if(weeklySummary.MacroCompleteDayCount == weeklySummary.EnergyCompleteDayCount) {
                return $"Для среднего использовано {weeklySummary.EnergyCompleteDayCount} из {weeklySummary.Days.Count} дней.";
            }

            return $"Калории: {weeklySummary.EnergyCompleteDayCount} из {weeklySummary.Days.Count} дней · БЖУ: {weeklySummary.MacroCompleteDayCount} из {weeklySummary.Days.Count} дней.";
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
