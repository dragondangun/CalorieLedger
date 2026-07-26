using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public sealed class InMemoryUserNutritionProfileStore:IUserNutritionProfileStore, IUserNutritionProfileWriter {
    private UserNutritionProfile currentProfile;

    public InMemoryUserNutritionProfileStore(UserNutritionProfile initialProfile) {
        ArgumentNullException.ThrowIfNull(initialProfile);

        currentProfile = initialProfile;
    }

    public UserNutritionProfile GetCurrentProfile() {
        return currentProfile;
    }

    public void UpdateGoal(NutritionGoal goal) {
        ArgumentNullException.ThrowIfNull(goal);

        currentProfile = currentProfile with {
            Goal = goal,
        };
    }

    public void UpdateProfile(UserNutritionProfile profile) {
        ArgumentNullException.ThrowIfNull(profile);

        currentProfile = profile;
    }
}