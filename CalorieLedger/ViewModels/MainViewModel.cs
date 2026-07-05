using CalorieLedger.Application.Profiles;
using CalorieLedger.Application.Today;
using CalorieLedger.ViewModels.Today;

namespace CalorieLedger.ViewModels;

public partial class MainViewModel:ViewModelBase {
    public TodayDashboardViewModel Today { get; }

    public MainViewModel() {
        IUserNutritionProfileProvider profileProvider =
            new SampleUserNutritionProfileProvider();

        ITodayDashboardSnapshotProvider todayProvider =
            new SampleTodayDashboardSnapshotProvider(profileProvider);

        var today = todayProvider.GetToday();

        Today = new TodayDashboardViewModel(today);
    }
}