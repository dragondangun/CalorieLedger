namespace CalorieLedger.ViewModels.Common;

public static class RussianDayCountFormatter {
    public static string Format(int dayCount) {
        var lastTwoDigits = dayCount % 100;

        var suffix = lastTwoDigits is >= 11 and <= 14
            ? "дней"
            : (dayCount % 10) switch {
                1 => "день",
                2 or 3 or 4 => "дня",
                _ => "дней",
            };

        return $"{dayCount} {suffix}";
    }
}