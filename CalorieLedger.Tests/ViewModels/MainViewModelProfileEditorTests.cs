using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels;
using CalorieLedger.ViewModels.Profile;

namespace CalorieLedger.Tests.ViewModels;

public sealed class MainViewModelProfileEditorTests {
    [Fact]
    public void EditProfileCommand_OpensEditor() {
        var viewModel = new MainViewModel(
            new InMemoryBodyMeasurementStore()
        );

        viewModel.EditProfileCommand.Execute(null);

        Assert.True(viewModel.IsProfileEditorOpen);
        Assert.False(viewModel.IsTodayDashboardVisible);
        Assert.NotNull(viewModel.ProfileEditor);
    }

    [Fact]
    public void CancelProfileEditing_ReturnsToDashboard() {
        var viewModel = new MainViewModel(
            new InMemoryBodyMeasurementStore()
        );

        viewModel.EditProfileCommand.Execute(null);

        var editor = Assert.IsType<UserNutritionProfileEditorViewModel>(
            viewModel.ProfileEditor
        );

        editor.CancelCommand.Execute(null);

        Assert.False(viewModel.IsProfileEditorOpen);
        Assert.True(viewModel.IsTodayDashboardVisible);
        Assert.Null(viewModel.ProfileEditor);
    }

    [Fact]
    public void SaveProfile_ClosesEditorAndRefreshesDashboard() {
        var viewModel = new MainViewModel(
            new InMemoryBodyMeasurementStore()
        );

        var previousToday = viewModel.Today;

        viewModel.EditProfileCommand.Execute(null);

        var editor = Assert.IsType<UserNutritionProfileEditorViewModel>(
            viewModel.ProfileEditor
        );

        editor.AgeYears = 31;
        editor.HeightCm = 181m;

        editor.SelectedActivityLevelOption = editor.ActivityLevelOptions.Single(
            option => option.Value == LifestyleActivityLevel.ModeratelyActive
        );

        editor.SaveCommand.Execute(null);

        Assert.False(viewModel.IsProfileEditorOpen);
        Assert.True(viewModel.IsTodayDashboardVisible);
        Assert.Null(viewModel.ProfileEditor);
        Assert.NotSame(previousToday, viewModel.Today);

        Assert.Contains(
            "Профиль сохранён",
            viewModel.Today.GoalActionSelectionSummary
        );
    }
}