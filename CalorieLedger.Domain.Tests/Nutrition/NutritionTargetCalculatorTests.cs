using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Tests.Nutrition;

public sealed class NutritionTargetCalculatorTests {
    [Fact]
    public void Calculate_MaintenanceGoal_ReturnsPositiveTargets() {
        var profile = new UserNutritionProfile(
            Id: Guid.NewGuid(),
            DisplayName: "Test user",
            Body: new BodyProfile(
                Sex: BiologicalSex.Male,
                AgeYears: 30,
                HeightCm: 180m,
                WeightKg: 80m,
                BodyFatPercent: null,
                BoneMassKg: null),
            LifestyleActivityLevel: LifestyleActivityLevel.Sedentary,
            Goal: new NutritionGoal(
                GoalType: WeightGoalType.Maintain));

        var target = NutritionTargetCalculator.Calculate(profile);

        Assert.True(target.CaloriesKcal > 0m);
        Assert.True(target.ProteinG > 0m);
        Assert.True(target.FatG > 0m);
        Assert.True(target.CarbsG > 0m);
    }

    [Fact]
    public void Calculate_LoseWeightGoal_ReturnsLowerCaloriesThanMaintenance() {
        var body = new BodyProfile(
            Sex: BiologicalSex.Female,
            AgeYears: 30,
            HeightCm: 165m,
            WeightKg: 70m,
            BodyFatPercent: null,
            BoneMassKg: null);

        var maintenanceProfile = new UserNutritionProfile(
            Id: Guid.NewGuid(),
            DisplayName: "Maintenance user",
            Body: body,
            LifestyleActivityLevel: LifestyleActivityLevel.LightlyActive,
            Goal: new NutritionGoal(
                GoalType: WeightGoalType.Maintain));

        var losingProfile = maintenanceProfile with
        {
            Goal = new NutritionGoal(
                GoalType: WeightGoalType.LoseWeight,
                DesiredWeightChangeKgPerWeek: 0.5m)
        };

        var maintenanceTarget = NutritionTargetCalculator.Calculate(maintenanceProfile);
        var losingTarget = NutritionTargetCalculator.Calculate(losingProfile);

        Assert.True(losingTarget.CaloriesKcal < maintenanceTarget.CaloriesKcal);
    }

    [Fact]
    public void Calculate_GainWeightGoal_ReturnsHigherCaloriesThanMaintenance() {
        var body = new BodyProfile(
            Sex: BiologicalSex.Male,
            AgeYears: 25,
            HeightCm: 175m,
            WeightKg: 70m,
            BodyFatPercent: 18m,
            BoneMassKg: null);

        var maintenanceProfile = new UserNutritionProfile(
            Id: Guid.NewGuid(),
            DisplayName: "Maintenance user",
            Body: body,
            LifestyleActivityLevel: LifestyleActivityLevel.ModeratelyActive,
            Goal: new NutritionGoal(
                GoalType: WeightGoalType.Maintain));

        var gainingProfile = maintenanceProfile with
        {
            Goal = new NutritionGoal(
                GoalType: WeightGoalType.GainWeight,
                DesiredWeightChangeKgPerWeek: 0.25m)
        };

        var maintenanceTarget = NutritionTargetCalculator.Calculate(maintenanceProfile);
        var gainingTarget = NutritionTargetCalculator.Calculate(gainingProfile);

        Assert.True(gainingTarget.CaloriesKcal > maintenanceTarget.CaloriesKcal);
    }
}