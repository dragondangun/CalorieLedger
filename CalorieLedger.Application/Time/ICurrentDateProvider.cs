namespace CalorieLedger.Application.Time;

public interface ICurrentDateProvider {
    DateOnly GetCurrentDate();
}