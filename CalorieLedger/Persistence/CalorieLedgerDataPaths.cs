using System;
using System.IO;

namespace CalorieLedger.Persistence;

internal static class CalorieLedgerDataPaths {
    private static readonly string ApplicationDirectory = Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData
        ),
        "CalorieLedger"
    );

    public static string BodyMeasurementsFilePath => Path.Combine(
        ApplicationDirectory,
        "body-measurements.json"
    );

    public static string UserProfileFilePath => Path.Combine(
        ApplicationDirectory,
        "user-profile.json"
    );

    public static string AdaptiveEnergyEvaluationsFilePath => Path.Combine(
        ApplicationDirectory,
        "adaptive-energy-evaluations.json"
    );

    public static string FoodDiaryFilePath => Path.Combine(
        ApplicationDirectory,
        "food-diary.json"
    );

    public static string ProductCatalogFilePath => Path.Combine(
        ApplicationDirectory,
        "product-catalog.json"
    );

    public static string CookingSessionsFilePath => Path.Combine(
        ApplicationDirectory,
        "cooking-sessions.json"
    );
}
