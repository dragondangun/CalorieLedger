using CalorieLedger.Domain.Activities;

namespace CalorieLedger.Application.Activities;

public sealed class RecentActivityService {
    private const int DefaultLookbackDays = 90;
    private readonly IActivityStore activityStore;

    public RecentActivityService(IActivityStore activityStore) {
        ArgumentNullException.ThrowIfNull(activityStore);
        this.activityStore = activityStore;
    }

    public IReadOnlyList<ActivityEntry> GetRecent(
        DateOnly targetDate,
        int maxCount = 5,
        int lookbackDays = DefaultLookbackDays
    ) {
        if(maxCount <= 0) {
            throw new ArgumentOutOfRangeException(nameof(maxCount));
        }

        if(lookbackDays <= 0) {
            throw new ArgumentOutOfRangeException(nameof(lookbackDays));
        }

        var startDate = targetDate.AddDays(-(lookbackDays - 1));

        return [
            .. activityStore.Get(startDate, targetDate)
                .OrderByDescending(activity => activity.Date)
                .ThenByDescending(activity => activity.StartedAt)
                .ThenByDescending(activity => activity.Id)
                .Take(maxCount)
        ];
    }
}
