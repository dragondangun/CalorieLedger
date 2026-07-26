using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Profile;

namespace CalorieLedger.Tests.ViewModels.Profile;

public sealed class UserNutritionProfileSummaryViewModelFactoryTests {
    [Fact]
    public void Create_FormatsProfileUsingRussianCulture() {
        var profile = CreateProfile();
        var editInvoked = false;

        var viewModel = UserNutritionProfileSummaryViewModelFactory.Create(
            profile,
            editProfile: () => editInvoked = true
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
            "Актуальный вес: 70,5 кг",
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
    public void Create_FormatsAgeEndingCorrectly(
        int ageYears,
        string expectedAge)
    {
        var profile = CreateProfile() with{
            Body = CreateProfile().Body with {
                AgeYears = ageYears,
            },
        };

        var viewModel = UserNutritionProfileSummaryViewModelFactory.Create(
            profile,
            editProfile: () => { }
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
}