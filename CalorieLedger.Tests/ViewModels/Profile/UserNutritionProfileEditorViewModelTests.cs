using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Profile;

namespace CalorieLedger.Tests.ViewModels.Profile;

public sealed class UserNutritionProfileEditorViewModelTests {
    [Fact]
    public void Constructor_LoadsDraftAndLocalizedOptions() {
        var profile = CreateProfile();
        var store = new InMemoryUserNutritionProfileStore(profile);
        var service = CreateEditorService(store);

        var viewModel = new UserNutritionProfileEditorViewModel(
            editorService: service,
            draft: service.LoadCurrentProfile(),
            onSaved: () => { },
            onCancelled: () => { }
        );

        Assert.Equal(profile.DisplayName, viewModel.DisplayName);
        Assert.Equal(profile.Body.AgeYears, viewModel.AgeYears);
        Assert.Equal(profile.Body.HeightCm, viewModel.HeightCm);

        Assert.Equal(
            BiologicalSex.Female,
            viewModel.SelectedSexOption.Value
        );

        Assert.Equal(
            "Женский",
            viewModel.SelectedSexOption.Title
        );

        Assert.Equal(
            LifestyleActivityLevel.LightlyActive,
            viewModel.SelectedActivityLevelOption.Value
        );

        Assert.Equal(
            "Лёгкая активность",
            viewModel.SelectedActivityLevelOption.Title
        );
    }

    [Fact]
    public void SaveCommand_ValidProfile_UpdatesStoreAndInvokesCallback() {
        var originalProfile = CreateProfile();
        var store = new InMemoryUserNutritionProfileStore(originalProfile);
        var service = CreateEditorService(store);
        var callbackInvoked = false;

        var viewModel = new UserNutritionProfileEditorViewModel(
            editorService: service,
            draft: service.LoadCurrentProfile(),
            onSaved: () => callbackInvoked = true,
            onCancelled: () => { }
        );

        viewModel.DisplayName = " Updated user ";
        viewModel.AgeYears = 28;
        viewModel.HeightCm = 183.5m;

        viewModel.SelectedSexOption = viewModel.SexOptions.Single(
            option => option.Value == BiologicalSex.Male
        );

        viewModel.SelectedActivityLevelOption = viewModel.ActivityLevelOptions.Single(
            option => option.Value == LifestyleActivityLevel.ModeratelyActive
        );

        viewModel.SaveCommand.Execute(null);

        var savedProfile = store.GetCurrentProfile();

        Assert.True(callbackInvoked);
        Assert.False(viewModel.HasValidationErrors);
        Assert.Equal("Updated user", savedProfile.DisplayName);
        Assert.Equal(BiologicalSex.Male, savedProfile.Body.Sex);
        Assert.Equal(28, savedProfile.Body.AgeYears);
        Assert.Equal(183.5m, savedProfile.Body.HeightCm);

        Assert.Equal(
            LifestyleActivityLevel.ModeratelyActive,
            savedProfile.LifestyleActivityLevel
        );

        Assert.Equal(
            originalProfile.Body.WeightKg,
            savedProfile.Body.WeightKg
        );

        Assert.Equal(
            originalProfile.Goal,
            savedProfile.Goal
        );
    }

    [Fact]
    public void SaveCommand_InvalidProfile_ShowsErrors() {
        var profile = CreateProfile();
        var store = new InMemoryUserNutritionProfileStore(profile);
        var service = CreateEditorService(store);
        var callbackInvoked = false;

        var viewModel = new UserNutritionProfileEditorViewModel(
            editorService: service,
            draft: service.LoadCurrentProfile(),
            onSaved: () => callbackInvoked = true,
            onCancelled: () => { }
        );

        viewModel.DisplayName = " ";
        viewModel.AgeYears = 0;
        viewModel.HeightCm = 300m;

        viewModel.SaveCommand.Execute(null);

        Assert.False(callbackInvoked);
        Assert.True(viewModel.HasValidationErrors);

        Assert.Contains(
            "Введите имя пользователя.",
            viewModel.ValidationMessages
        );

        Assert.Contains(
            "Возраст должен находиться в диапазоне от 1 до 120 лет.",
            viewModel.ValidationMessages
        );

        Assert.Contains(
            "Рост должен находиться в диапазоне от 50 до 250 см.",
            viewModel.ValidationMessages
        );

        Assert.Equal(profile, store.GetCurrentProfile());
    }

    [Fact]
    public void CancelCommand_InvokesCallback() {
        var store = new InMemoryUserNutritionProfileStore(
            CreateProfile()
        );

        var service = CreateEditorService(store);
        var callbackInvoked = false;

        var viewModel = new UserNutritionProfileEditorViewModel(
            editorService: service,
            draft: service.LoadCurrentProfile(),
            onSaved: () => { },
            onCancelled: () => callbackInvoked = true
        );

        viewModel.CancelCommand.Execute(null);

        Assert.True(callbackInvoked);
    }

    private static UserNutritionProfileEditorService CreateEditorService(InMemoryUserNutritionProfileStore store) {
        return new UserNutritionProfileEditorService(
            profileStore: store,
            profileWriter: store
        );
    }

    private static UserNutritionProfile CreateProfile() {
        return new UserNutritionProfile(
            Id: Guid.NewGuid(),
            DisplayName: "Test user",
            Body: new BodyProfile(
                Sex: BiologicalSex.Female,
                AgeYears: 27,
                HeightCm: 184m,
                WeightKg: 70m,
                BodyFatPercent: 20m,
                BoneMassKg: 3.2m,
                MuscleMassKg: 35m,
                MusclePercent: 50m
            ),
            LifestyleActivityLevel: LifestyleActivityLevel.LightlyActive,
            Goal: new NutritionGoal(
                GoalType: WeightGoalType.Maintain,
                Strategy: EnergyStrategy.FromBalancePercent(0m)
            )
        );
    }
}