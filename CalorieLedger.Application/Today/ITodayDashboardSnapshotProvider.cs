namespace CalorieLedger.Application.Today;

public interface ITodayDashboardSnapshotProvider {
    TodayDashboardSnapshot GetToday();
}