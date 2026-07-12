using CalorieLedger.Domain.Profile;
using CommunityToolkit.Mvvm.Input;

namespace CalorieLedger.ViewModels.Today;

public sealed class TodayGoalActionViewModel {
    public TodayGoalActionViewModel(
        GoalNextAction action,
        string title,
        System.Action<GoalNextAction> onSelected) {
        Action = action;
        Title = title;

        SelectCommand = new RelayCommand(
            () => onSelected(Action));
    }

    public GoalNextAction Action { get; }

    public string Title { get; }

    public IRelayCommand SelectCommand { get; }
}