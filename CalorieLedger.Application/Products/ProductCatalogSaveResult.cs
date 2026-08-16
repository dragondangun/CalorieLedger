namespace CalorieLedger.Application.Products;

public sealed record ProductCatalogSaveResult(
    bool IsSuccess,
    IReadOnlyList<ProductCatalogValidationError> Errors
);
