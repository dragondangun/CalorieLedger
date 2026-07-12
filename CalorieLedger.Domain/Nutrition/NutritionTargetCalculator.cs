using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Domain.Nutrition;

public static class NutritionTargetCalculator {
    public static DailyNutritionTarget Calculate(UserNutritionProfile profile) {
        ArgumentNullException.ThrowIfNull(profile);

        var maintenanceCalories = CalculateMaintenanceCalories(profile);

        var goalAdjustment = CalculateGoalAdjustment(
            profile.Goal,
            maintenanceCalories);

        var targetCalories = Math.Round(
            maintenanceCalories + goalAdjustment,
            0);

        if(targetCalories <= 0m) {
            throw new InvalidOperationException(
                "The selected energy strategy produces a non-positive calorie target.");
        }

        var proteinTarget = CalculateProteinTarget(
            profile.Body,
            profile.Goal);
        var fatTarget = CalculateFatTarget(targetCalories);
        var carbsTarget = CalculateCarbsTarget(
            targetCalories,
            proteinTarget,
            fatTarget);

        return new DailyNutritionTarget(
            CaloriesKcal: targetCalories,
            ProteinG: Math.Round(proteinTarget, 0),
            FatG: Math.Round(fatTarget, 0),
            CarbsG: Math.Round(carbsTarget, 0));
    }

    private static decimal CalculateBmr(BodyProfile body) {
        if(body.BodyFatPercent is > 0m and < 100m) {
            var leanBodyMassKg = body.WeightKg * (1m - body.BodyFatPercent.Value / 100m);
            return 370m + 21.6m * leanBodyMassKg;
        }

        return body.Sex switch
        {
            BiologicalSex.Male =>
                10m * body.WeightKg + 6.25m * body.HeightCm - 5m * body.AgeYears + 5m,

            BiologicalSex.Female =>
                10m * body.WeightKg + 6.25m * body.HeightCm - 5m * body.AgeYears - 161m,

            BiologicalSex.Unknown =>
                10m * body.WeightKg + 6.25m * body.HeightCm - 5m * body.AgeYears - 78m,

            _ => throw new ArgumentOutOfRangeException(nameof(body.Sex), body.Sex, null)
        };
    }

    private static decimal GetActivityMultiplier(LifestyleActivityLevel activityLevel) {
        return activityLevel switch
        {
            LifestyleActivityLevel.Sedentary => 1.2m,
            LifestyleActivityLevel.LightlyActive => 1.375m,
            LifestyleActivityLevel.ModeratelyActive => 1.55m,
            LifestyleActivityLevel.VeryActive => 1.725m,
            LifestyleActivityLevel.ExtremelyActive => 1.9m,
            _ => throw new ArgumentOutOfRangeException(nameof(activityLevel), activityLevel, null)
        };
    }

    private static decimal CalculateGoalAdjustment(
        NutritionGoal goal,
        decimal maintenanceCalories) {
        if(goal.Strategy is null) {
            throw new InvalidOperationException(
                "Nutrition goal must have an energy strategy.");
        }

        var calculation =
        EnergyStrategyCalculator.Calculate(
            strategy: goal.Strategy,
            goalType: goal.GoalType,
            maintenanceCaloriesKcal:
                maintenanceCalories);

        return calculation.DailyEnergyAdjustmentKcal;
    }

    private static decimal CalculateProteinTarget(
        BodyProfile body,
        NutritionGoal goal) {
        var multiplier = goal.GoalType switch
        {
            WeightGoalType.LoseWeight => 1.8m,
            WeightGoalType.Maintain => 1.6m,
            WeightGoalType.GainWeight => 1.8m,
            _ => throw new ArgumentOutOfRangeException(nameof(goal.GoalType), goal.GoalType, null)
        };

        return body.WeightKg * multiplier;
    }

    private static decimal CalculateFatTarget(decimal targetCalories) {
        var fatCalories = targetCalories * 0.3m;
        return fatCalories / 9m;
    }

    private static decimal CalculateCarbsTarget(
        decimal targetCalories,
        decimal proteinG,
        decimal fatG) {
        var proteinCalories = proteinG * 4m;
        var fatCalories = fatG * 9m;
        var carbsCalories = targetCalories - proteinCalories - fatCalories;

        return carbsCalories <= 0m
            ? 0m
            : carbsCalories / 4m;
    }

    public static decimal CalculateMaintenanceCalories(UserNutritionProfile profile) {
        var bmr = CalculateBmr(profile.Body);

        var activityMultiplier =
        GetActivityMultiplier(
            profile.LifestyleActivityLevel);

        return bmr * activityMultiplier;
    }
}