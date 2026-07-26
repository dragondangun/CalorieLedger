using System;
using System.IO;

namespace CalorieLedger.Persistence;

internal static class CalorieLedgerDataPaths {
    private static readonly string applicationDirectory =
        Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData
            ),
            "CalorieLedger"
        );

    public static string BodyMeasurementsFilePath =>
        Path.Combine(
            applicationDirectory,
            "body-measurements.json"
        );

    public static string UserProfileFilePath =>
        Path.Combine(
            applicationDirectory,
            "user-profile.json"
        );
}