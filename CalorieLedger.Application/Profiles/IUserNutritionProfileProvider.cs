using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public interface IUserNutritionProfileProvider {
    UserNutritionProfile GetCurrentProfile();
}