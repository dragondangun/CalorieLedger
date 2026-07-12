using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public interface IUserNutritionProfileStore
    :IUserNutritionProfileProvider {
    void UpdateGoal(NutritionGoal goal);
}