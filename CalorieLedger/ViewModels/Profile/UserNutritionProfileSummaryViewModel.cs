using System;
using CommunityToolkit.Mvvm.Input;

namespace CalorieLedger.ViewModels.Profile;

public sealed partial class UserNutritionProfileSummaryViewModel:ViewModelBase {
    private readonly Action editProfile;
    private readonly Action addBodyMeasurement;

    public string DisplayName { get; }

    public string PersonalDataSummary { get; }

    public string ActivitySummary { get; }

    public string WeightSummary { get; }

    public string WeightSourceSummary { get; }

    public string BodyCompositionSummary { get; }

    public string MeasurementWarning { get; }

    public bool HasBodyCompositionSummary => !string.IsNullOrWhiteSpace(BodyCompositionSummary);

    public bool HasMeasurementWarning => !string.IsNullOrWhiteSpace(MeasurementWarning);

    public bool CanAddBodyMeasurement => HasMeasurementWarning;

    public UserNutritionProfileSummaryViewModel(
        string displayName,
        string personalDataSummary,
        string activitySummary,
        string weightSummary,
        string weightSourceSummary,
        string bodyCompositionSummary,
        string measurementWarning,
        Action editProfile,
        Action addBodyMeasurement)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(personalDataSummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(activitySummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(weightSummary);
        ArgumentException.ThrowIfNullOrWhiteSpace(weightSourceSummary);
        ArgumentNullException.ThrowIfNull(editProfile);
        ArgumentNullException.ThrowIfNull(addBodyMeasurement);

        DisplayName = displayName;
        PersonalDataSummary = personalDataSummary;
        ActivitySummary = activitySummary;
        WeightSummary = weightSummary;
        WeightSourceSummary = weightSourceSummary;
        BodyCompositionSummary = bodyCompositionSummary;
        MeasurementWarning = measurementWarning;
        this.editProfile = editProfile;
        this.addBodyMeasurement = addBodyMeasurement;
    }

    [RelayCommand]
    private void EditProfile() {
        editProfile();
    }

    [RelayCommand(CanExecute = nameof(CanAddBodyMeasurement))]
    private void AddBodyMeasurement() {
        addBodyMeasurement();
    }
}