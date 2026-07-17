using CalorieLedger.Domain.Profile;

namespace CalorieLedger.Application.Profiles;

public interface IBodyMeasurementStore {
    IReadOnlyList<BodyMeasurementEntry> GetAll();

    void Save(BodyMeasurementEntry entry);

    bool Delete(Guid id);
}