using CalorieLedger.Domain.Profile;

namespace CalorieLedger.ViewModels.Today;

public sealed record TodayGoalActionViewModel(
    GoalNextAction Action,
    string Title);