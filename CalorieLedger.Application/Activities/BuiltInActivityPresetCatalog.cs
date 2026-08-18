using System.Collections.Generic;
using System.Linq;

namespace CalorieLedger.Application.Activities;

public static class BuiltInActivityPresetCatalog {
    public static IReadOnlyList<ActivityPreset> All { get; } = [
        new("17190", "Ходьба, умеренный темп", 3.8m, true),
        new("17200", "Ходьба, быстрый темп", 4.8m, true),
        new("02054", "Силовая тренировка, несколько упражнений", 3.5m, true),
        new("02050", "Силовая тренировка, интенсивная", 6.0m, true),
        new("15200", "Фехтование, общее", 6.0m, true),
        new("15425", "Боевые искусства, спокойная практика", 5.3m, true)
    ];

    public static ActivityPreset? Find(string? code) {
        return code is null ? null : All.FirstOrDefault(preset => preset.Code == code);
    }
}
