using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public interface IUserNutritionProfileWriter {
    void UpdateProfile(UserNutritionProfile profile);
}