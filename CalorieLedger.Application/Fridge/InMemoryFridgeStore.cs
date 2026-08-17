using CalorieLedger.Domain.Fridge;

namespace CalorieLedger.Application.Fridge;

public sealed class InMemoryFridgeStore:IFridgeStore {
    private readonly List<FridgeItem> items = [];

    public IReadOnlyList<FridgeItem> GetAll() {
        return [
            .. items
                .OrderBy(item => item.ExpirationDate is null)
                .ThenBy(item => item.ExpirationDate)
                .ThenBy(item => item.Name)
                .ThenBy(item => item.Id),
        ];
    }

    public FridgeItem? Get(Guid id) {
        return items.FirstOrDefault(item => item.Id == id);
    }

    public void Save(FridgeItem item) {
        ArgumentNullException.ThrowIfNull(item);

        SaveMany([item]);
    }

    public void SaveMany(IReadOnlyCollection<FridgeItem> newItems) {
        ArgumentNullException.ThrowIfNull(newItems);

        foreach(var item in newItems) {
            ArgumentNullException.ThrowIfNull(item);

            var index = items.FindIndex(existing => existing.Id == item.Id);

            if(index >= 0) {
                items[index] = item;
            }
            else {
                items.Add(item);
            }
        }
    }

    public bool Delete(Guid id) {
        return items.RemoveAll(item => item.Id == id) > 0;
    }
}
