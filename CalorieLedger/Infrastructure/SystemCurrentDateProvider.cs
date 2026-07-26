using CalorieLedger.Application.Time;
using System;

namespace CalorieLedger.Infrastructure;

public sealed class SystemCurrentDateProvider:ICurrentDateProvider {
    public DateOnly GetCurrentDate() {
        return DateOnly.FromDateTime(DateTime.Today);
    }
}