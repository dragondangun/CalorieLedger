using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Common;
using CalorieLedger.ViewModels.Profile;

namespace CalorieLedger.Tests.ViewModels.Profile;

public sealed class UserNutritionProfileSummaryViewModelFactoryTests {
    [Fact]
    public void Create_FormatsProfileUsingRussianCulture() {
        var profile = CreateProfile();
        var editInvoked = false;

        var viewModel = UserNutritionProfileSummaryViewModelFactory.Create(
            profile,
            latestMeasurement: null,
            effectiveMeasurement: null,
            currentDate: new DateOnly(2026, 7, 26),
            editProfile: () => editInvoked = true,
            addBodyMeasurement: () => { }
        );

        Assert.Equal("Test user", viewModel.DisplayName);
        Assert.Equal(
            "Женский пол · 27 лет · 184,0 см",
            viewModel.PersonalDataSummary
        );

        Assert.Equal(
            "Активность: лёгкая",
            viewModel.ActivitySummary
        );

        Assert.Equal(
            "Вес: 70,5 кг",
            viewModel.WeightSummary
        );

        Assert.Contains(
            "жир 20,0%",
            viewModel.BodyCompositionSummary
        );

        Assert.Contains(
            "мышцы 35,0 кг",
            viewModel.BodyCompositionSummary
        );

        Assert.Contains(
            "кости 3,2 кг",
            viewModel.BodyCompositionSummary
        );

        viewModel.EditProfileCommand.Execute(null);

        Assert.True(editInvoked);
    }

    [Theory]
    [InlineData(21, "21 год")]
    [InlineData(22, "22 года")]
    [InlineData(25, "25 лет")]
    [InlineData(111, "111 лет")]
    public void Create_FormatsAgeEndingCorrectly(int ageYears, string expectedAge) {
        var profile = CreateProfile() with{
            Body = CreateProfile().Body with {
                AgeYears = ageYears,
            },
        };

        var viewModel = UserNutritionProfileSummaryViewModelFactory.Create(
            profile,
            latestMeasurement: null,
            effectiveMeasurement: null,
            currentDate: new DateOnly(2026, 7, 26),
            editProfile: () => { },
            addBodyMeasurement: () => { }
        );

        Assert.Contains(
            expectedAge,
            viewModel.PersonalDataSummary
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
                WeightKg: 70.5m,
                BodyFatPercent: 20m,
                BoneMassKg: 3.2m,
                MuscleMassKg: 35m,
                MusclePercent: 49.65m
            ),
            LifestyleActivityLevel: LifestyleActivityLevel.LightlyActive,
            Goal: new NutritionGoal(
                GoalType: WeightGoalType.Maintain,
                Strategy: EnergyStrategy.FromBalancePercent(0m)
            )
        );
    }

    [Fact]
    public void Create_WithoutMeasurements_ShowsProfileSourceAndWarning() {
        var viewModel =
        UserNutritionProfileSummaryViewModelFactory.Create(
            profile: CreateProfile(),
            latestMeasurement: null,
            effectiveMeasurement: null,
            currentDate: new DateOnly(2026, 7, 26),
            editProfile: () => { },
            addBodyMeasurement: () => { }
        );

        Assert.Equal(
            "Источник веса: исходные данные профиля",
            viewModel.WeightSourceSummary
        );

        Assert.True(viewModel.HasMeasurementWarning);

        Assert.Contains(
            "Добавьте измерение тела",
            viewModel.MeasurementWarning
        );
    }

    [Fact]
    public void Create_WithStaleMeasurement_ShowsAgeWarning() {
        var measurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 6, 30),
            WeightKg: 70.5m
        );

        var viewModel = UserNutritionProfileSummaryViewModelFactory.Create(
            profile: CreateProfile(),
            latestMeasurement: measurement,
            effectiveMeasurement: measurement,
            currentDate: new DateOnly(2026, 7, 26),
            editProfile: () => { },
            addBodyMeasurement: () => { }
        );

        Assert.True(viewModel.HasMeasurementWarning);

        Assert.Contains(
            "26 дней",
            viewModel.MeasurementWarning
        );
    }

    [Fact]
    public void Create_WithoutMeasurements_AllowsAddingMeasurement() {
        var addMeasurementInvoked = false;

        var viewModel = UserNutritionProfileSummaryViewModelFactory.Create(
            profile: CreateProfile(),
            latestMeasurement: null,
            effectiveMeasurement: null,
            currentDate: new DateOnly(2026, 7, 26),
            editProfile: () => { },
            addBodyMeasurement: () => addMeasurementInvoked = true
        );

        Assert.True(viewModel.CanAddBodyMeasurement);
        Assert.True(viewModel.AddBodyMeasurementCommand.CanExecute(null));

        viewModel.AddBodyMeasurementCommand.Execute(null);

        Assert.True(addMeasurementInvoked);
    }

    [Fact]
    public void Create_WithRecentMeasurement_DisablesAddingFromWarning() {
        var measurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 7, 25),
            WeightKg: 70m
        );

        var viewModel = UserNutritionProfileSummaryViewModelFactory.Create(
            profile: CreateProfile(),
            latestMeasurement: measurement,
            effectiveMeasurement: measurement,
            currentDate: new DateOnly(2026, 7, 26),
            editProfile: () => { },
            addBodyMeasurement: () => { }
        );

        Assert.False(viewModel.HasMeasurementWarning);
        Assert.False(viewModel.CanAddBodyMeasurement);
        Assert.False(viewModel.AddBodyMeasurementCommand.CanExecute(null));
    }

    [Fact]
    public void Create_MeasurementExactlyAtFreshnessBoundary_HasNoWarning() {
        var currentDate = new DateOnly(2026, 8, 8);

        var measurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate.AddDays(-BodyMeasurementFreshnessPolicy.WarningDayCount),
            WeightKg: 80m
        );

        var profile = CreateProfile();

        var summary = UserNutritionProfileSummaryViewModelFactory.Create(
            profile: profile,
            latestMeasurement: measurement,
            effectiveMeasurement: measurement,
            currentDate: currentDate,
            editProfile: () => { },
            addBodyMeasurement: () => { }
        );

        Assert.Equal(
            string.Empty,
            summary.MeasurementWarning
        );
    }

    [Fact]
    public void Create_StaleMeasurement_HasWarning() {
        var currentDate = new DateOnly(2026, 8, 8);

        var measurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate.AddDays(
                -BodyMeasurementFreshnessPolicy.WarningDayCount - 1
            ),
            WeightKg: 80m
        );

        var profile = CreateProfile();

        var summary = UserNutritionProfileSummaryViewModelFactory.Create(
            profile: profile,
            latestMeasurement: measurement,
            effectiveMeasurement: measurement,
            currentDate: currentDate,
            editProfile: () => { },
            addBodyMeasurement: () => { }
        );

        Assert.Equal(
            $"Последнему измерению {RussianDayCountFormatter.Format(BodyMeasurementFreshnessPolicy.WarningDayCount + 1)}. Добавьте новое измерение, чтобы расчёты использовали свежие данные.",
            summary.MeasurementWarning
        );
    }

    [Fact]
    public void Create_WithRecentMeasurement_ShowsDateWithoutWarning() {
        var measurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 7, 20),
            WeightKg: 70.5m
        );

        var viewModel =
        UserNutritionProfileSummaryViewModelFactory.Create(
            profile: CreateProfile(),
            latestMeasurement: measurement,
            effectiveMeasurement: measurement,
            currentDate: new DateOnly(2026, 7, 26),
            editProfile: () => { },
            addBodyMeasurement: () => { }
        );

        Assert.Equal(
            "Источник веса: измерение от 20.07.2026",
            viewModel.WeightSourceSummary
        );

        Assert.False(viewModel.HasMeasurementWarning);
        Assert.Equal(string.Empty, viewModel.MeasurementWarning);
    }

    [Fact]
    public void Create_FutureMeasurement_HasDateWarning() {
        var currentDate = new DateOnly(2026, 8, 8);

        var measurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate.AddDays(1),
            WeightKg: 80m
        );

        var profile = CreateProfile();

        var summary = UserNutritionProfileSummaryViewModelFactory.Create(
            profile: profile,
            latestMeasurement: measurement,
            effectiveMeasurement: null,
            currentDate: currentDate,
            editProfile: () => { },
            addBodyMeasurement: () => { }
        );

        Assert.Equal(
            "Источник веса: исходные данные профиля",
            summary.WeightSourceSummary
        );

        Assert.Equal(
            "В истории есть измерение с будущей датой. Проверьте дату измерения.",
            summary.MeasurementWarning
        );
    }

    [Fact]
    public void Create_FutureLatestWithEarlierEffectiveMeasurement_ShowsEffectiveSourceAndFutureWarning() {
        var currentDate = new DateOnly(2026, 8, 8);

        var effectiveMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate.AddDays(-1),
            WeightKg: 79m
        );

        var futureMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate.AddDays(1),
            WeightKg: 80m
        );

        var summary = UserNutritionProfileSummaryViewModelFactory.Create(
            profile: CreateProfile(),
            latestMeasurement: futureMeasurement,
            effectiveMeasurement: effectiveMeasurement,
            currentDate: currentDate,
            editProfile: () => { },
            addBodyMeasurement: () => { }
        );

        Assert.Equal(
            "Источник веса: измерение от 07.08.2026",
            summary.WeightSourceSummary
        );

        Assert.Equal(
            "В истории есть измерение с будущей датой. Проверьте дату измерения.",
            summary.MeasurementWarning
        );
    }
}
