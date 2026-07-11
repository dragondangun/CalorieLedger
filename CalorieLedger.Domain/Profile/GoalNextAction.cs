namespace CalorieLedger.Domain.Profile;

public enum GoalNextAction {
    ContinueCurrentGoal = 1,
    SwitchToMaintenance = 2,
    StartWeightLoss = 3,
    StartWeightGain = 4,
    SetNewGoal = 5,
    RepeatMeasurements = 6
}