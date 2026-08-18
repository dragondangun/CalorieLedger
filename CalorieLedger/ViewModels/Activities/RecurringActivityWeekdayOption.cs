using System;
using System.Collections.Generic;

namespace CalorieLedger.ViewModels.Activities;

public sealed record RecurringActivityWeekdayOption(
    DayOfWeek Value,
    string Name
) {
    public static IReadOnlyList<RecurringActivityWeekdayOption> All { get; } = [
        new(DayOfWeek.Monday, "Понедельник"),
        new(DayOfWeek.Tuesday, "Вторник"),
        new(DayOfWeek.Wednesday, "Среда"),
        new(DayOfWeek.Thursday, "Четверг"),
        new(DayOfWeek.Friday, "Пятница"),
        new(DayOfWeek.Saturday, "Суббота"),
        new(DayOfWeek.Sunday, "Воскресенье")
    ];
}
