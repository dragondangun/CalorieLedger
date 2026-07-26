using System;
using CommunityToolkit.Mvvm.Input;

namespace CalorieLedger.ViewModels.Profile;

public sealed partial class UserNutritionProfileSummaryViewModel:ViewModelBase {
    private readonly Action editProfile;

    public string DisplayName { get; }

    public string PersonalDataSummary { get; }

    public string ActivitySummary { get; }

    public string WeightSummary { get; }

    public string BodyCompositionSummary { get; }

    public bool HasBodyCompositionSummary => !string.IsNullOrWhiteSpace(BodyCompositionSummary);

    public UserNutritionProfileSummaryViewModel(
        string displayName,
        string personalDataSummary,
        string activitySummary,
        string weightSummary,
        string bodyCompositionSummary,
        Action editProfile)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(editProfile);

        DisplayName = displayName;
        PersonalDataSummary = personalDataSummary;
        ActivitySummary = activitySummary;
        WeightSummary = weightSummary;
        BodyCompositionSummary = bodyCompositionSummary;
        this.editProfile = editProfile;
    }

    [RelayCommand]
    private void EditProfile() {
        editProfile();
    }
}