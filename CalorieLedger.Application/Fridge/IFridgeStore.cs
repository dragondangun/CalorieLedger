using CalorieLedger.Domain.Fridge;

namespace CalorieLedger.Application.Fridge;

public interface IFridgeStore {
    IReadOnlyList<FridgeItem> GetAll();

    FridgeItem? Get(Guid id);
    void Save(FridgeItem item);
    bool Delete(Guid id);
}
