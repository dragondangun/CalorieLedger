using CalorieLedger.Domain.Products;

namespace CalorieLedger.Application.Products;

public sealed class InMemoryProductCatalogStore:IProductCatalogStore {
    private readonly List<ProductCatalogItem> items = [];

    public IReadOnlyList<ProductCatalogItem> GetAll() {
        return [
            .. items
                .OrderBy(item => item.Name)
                .ThenBy(item => item.Brand)
                .ThenBy(item => item.Id),
        ];
    }

    public ProductCatalogItem? Get(Guid id) {
        return items.FirstOrDefault(item => item.Id == id);
    }

    public void Save(ProductCatalogItem item) {
        ArgumentNullException.ThrowIfNull(item);

        var existingIndex = items.FindIndex(existing => existing.Id == item.Id);

        if(existingIndex >= 0) {
            items[existingIndex] = item;

            return;
        }

        items.Add(item);
    }

    public bool Delete(Guid id) {
        return items.RemoveAll(item => item.Id == id) > 0;
    }
}
