namespace CalorieLedger.Application.Today;

public sealed record TodayActivitySnapshotItem(
    string Name,
    decimal BurnedCaloriesKcal,
    TimeOnly? StartedAt = null,
    TimeSpan? Duration = null);