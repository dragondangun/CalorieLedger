using CalorieLedger.Application.Profiles;
using CalorieLedger.Domain.Profile;
using CalorieLedger.Persistence;

namespace CalorieLedger.Tests.Persistence;

public sealed class JsonUserNutritionProfileStoreTests:IDisposable {
    private readonly string directoryPath;
    private readonly string filePath;

    public JsonUserNutritionProfileStoreTests() {
        directoryPath = Path.Combine(
            Path.GetTempPath(),
            "CalorieLedger.Tests",
            Guid.NewGuid().ToString("N")
        );

        filePath = Path.Combine(
            directoryPath,
            "user-profile.json"
        );
    }

    [Fact]
    public void GetCurrentProfile_MissingFile_ReturnsFallbackProfile() {
        var fallbackProfile = CreateProfile();

        var store = new JsonUserNutritionProfileStore(
            filePath,
            new TestProfileProvider(fallbackProfile)
        );

        var result = store.GetCurrentProfile();

        Assert.Equal(
            fallbackProfile,
            result
        );
    }

    [Fact]
    public void UpdateGoal_PersistsGoalBetweenStoreInstances() {
        var fallbackProfile = CreateProfile();

        var updatedGoal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            TargetBodyFatPercent: 15m,
            Strategy: EnergyStrategy.FromBalancePercent(17m)
        );

        var firstStore = new JsonUserNutritionProfileStore(
            filePath,
            new TestProfileProvider(fallbackProfile)
        );

        firstStore.UpdateGoal(updatedGoal);

        var secondStore = new JsonUserNutritionProfileStore(
            filePath,
            new TestProfileProvider(CreateDifferentProfile())
        );

        var result = secondStore.GetCurrentProfile();

        Assert.Equal(
            updatedGoal,
            result.Goal
        );
    }

    [Fact]
    public void UpdateGoal_PreservesNonGoalProfileValues() {
        var fallbackProfile = CreateProfile();

        var store = new JsonUserNutritionProfileStore(
            filePath,
            new TestProfileProvider(fallbackProfile)
        );

        var updatedGoal = new NutritionGoal(
            GoalType: WeightGoalType.GainWeight,
            TargetWeightKg: 85m,
            Strategy: EnergyStrategy.FromBalancePercent(8m)
        );

        store.UpdateGoal(updatedGoal);

        var result = store.GetCurrentProfile();

        Assert.Equal(
            fallbackProfile.Id,
            result.Id
        );

        Assert.Equal(
            fallbackProfile.DisplayName,
            result.DisplayName
        );

        Assert.Equal(
            fallbackProfile.Body,
            result.Body
        );

        Assert.Equal(
            fallbackProfile.LifestyleActivityLevel,
            result.LifestyleActivityLevel
        );

        Assert.Equal(
            updatedGoal,
            result.Goal
        );
    }

    [Fact]
    public void GetCurrentProfile_CorruptedFile_PreservesFileAndReturnsFallback() {
        Directory.CreateDirectory(directoryPath);

        File.WriteAllText(
            filePath,
            "{ invalid json"
        );

        var fallbackProfile = CreateProfile();

        var store = new JsonUserNutritionProfileStore(
            filePath,
            new TestProfileProvider(fallbackProfile)
        );

        var result = store.GetCurrentProfile();

        Assert.Equal(
            fallbackProfile,
            result
        );

        Assert.False(File.Exists(filePath));

        var preservedFiles = Directory.GetFiles(
            directoryPath,
            "user-profile.json.corrupt-*"
        );

        Assert.Single(preservedFiles);
    }

    [Fact]
    public void UpdateProfile_PersistsCompleteProfile() {
        var fallbackProfile = CreateProfile();

        var firstStore =
        new JsonUserNutritionProfileStore(
            filePath,
            new TestProfileProvider(fallbackProfile)
        );

        var updatedProfile =
        fallbackProfile with
        {
            DisplayName = "Updated user",
            Body =
                new BodyProfile(
                    Sex: BiologicalSex.Female,
                    AgeYears: 27,
                    HeightCm: 184m,
                    WeightKg: 70m,
                    BodyFatPercent: 18m,
                    BoneMassKg: 3.1m,
                    MuscleMassKg: 34m,
                    MusclePercent: 48.57m
                ),
            LifestyleActivityLevel =
                LifestyleActivityLevel.ModeratelyActive,
        };

        firstStore.UpdateProfile(updatedProfile);

        var secondStore =
        new JsonUserNutritionProfileStore(
            filePath,
            new TestProfileProvider(
                CreateDifferentProfile()
            )
        );

        Assert.Equal(
            updatedProfile,
            secondStore.GetCurrentProfile()
        );
    }

    [Fact]
    public void UpdateGoal_AfterProfileUpdate_PreservesProfileFields() {
        var fallbackProfile = CreateProfile();

        var store = new JsonUserNutritionProfileStore(
            filePath,
            new TestProfileProvider(fallbackProfile)
        );

        var updatedProfile = fallbackProfile with{
            DisplayName = "Updated user",
            LifestyleActivityLevel = LifestyleActivityLevel.VeryActive,
        };

        store.UpdateProfile(updatedProfile);

        var updatedGoal = new NutritionGoal(
            GoalType: WeightGoalType.LoseWeight,
            TargetWeightKg: 75m,
            Strategy: EnergyStrategy.FromBalancePercent(17m)
        );

        store.UpdateGoal(updatedGoal);

        var result = store.GetCurrentProfile();

        Assert.Equal(
            "Updated user",
            result.DisplayName
        );

        Assert.Equal(
            LifestyleActivityLevel.VeryActive,
            result.LifestyleActivityLevel
        );

        Assert.Equal(
            updatedProfile.Body,
            result.Body
        );

        Assert.Equal(
            updatedGoal,
            result.Goal
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
                WeightKg: 80m,
                BodyFatPercent: 20m,
                BoneMassKg: 3.2m,
                MuscleMassKg: 35m,
                MusclePercent: 43.75m
            ),
            LifestyleActivityLevel: LifestyleActivityLevel.Sedentary,
            Goal: new NutritionGoal(
                GoalType: WeightGoalType.Maintain,
                Strategy:
                    EnergyStrategy.FromBalancePercent(0m)
            )
        );
    }

    private static UserNutritionProfile CreateDifferentProfile() {
        return new UserNutritionProfile(
            Id: Guid.NewGuid(),
            DisplayName: "Other user",
            Body: new BodyProfile(
                Sex: BiologicalSex.Male,
                AgeYears: 40,
                HeightCm: 175m,
                WeightKg: 90m
            ),
            LifestyleActivityLevel: LifestyleActivityLevel.VeryActive,
            Goal: new NutritionGoal(
                GoalType: WeightGoalType.Maintain,
                Strategy:
                    EnergyStrategy.FromBalancePercent(0m)
            )
        );
    }

    public void Dispose() {
        if(Directory.Exists(directoryPath)) {
            Directory.Delete(
                directoryPath,
                recursive: true
            );
        }
    }

    private sealed class TestProfileProvider:IUserNutritionProfileProvider {
        private readonly UserNutritionProfile profile;

        public TestProfileProvider(UserNutritionProfile profile) {
            this.profile = profile;
        }

        public UserNutritionProfile GetCurrentProfile() {
            return profile;
        }
    }
}
