namespace CalorieLedger.Application.Profiles;

public sealed record BodyMeasurementSaveResult(
    bool IsSuccess,
    IReadOnlyList<
        BodyMeasurementValidationError> Errors);