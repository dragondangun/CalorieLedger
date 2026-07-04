using CalorieLedger.ViewModels.Today;

namespace CalorieLedger.ViewModels;

public partial class MainViewModel:ViewModelBase {
    public TodayDashboardViewModel Today { get; } = new();
}