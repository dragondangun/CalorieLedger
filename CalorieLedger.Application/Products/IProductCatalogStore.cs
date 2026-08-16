using CalorieLedger.Domain.Products;

namespace CalorieLedger.Application.Products;

public interface IProductCatalogStore {
    IReadOnlyList<ProductCatalogItem> GetAll();

    ProductCatalogItem? Get(Guid id);

    void Save(ProductCatalogItem item);

    bool Delete(Guid id);
}
