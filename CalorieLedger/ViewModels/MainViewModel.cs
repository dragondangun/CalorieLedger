using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.ViewModels.Today;

namespace CalorieLedger.ViewModels;

public partial class MainViewModel:ViewModelBase {
    public TodayDashboardViewModel Today { get; }

    public MainViewModel() {
        IUserNutritionProfileProvider profileProvider =
            new SampleUserNutritionProfileProvider();

        var profile = profileProvider.GetCurrentProfile();
        var target = NutritionTargetCalculator.Calculate(profile);

        Today = new TodayDashboardViewModel(target);
    }
}