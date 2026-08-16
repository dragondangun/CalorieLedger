using CalorieLedger.Domain.Nutrition;
using CalorieLedger.Domain.Products;

namespace CalorieLedger.Application.Products;

public sealed class ProductCatalogService {
    private readonly IProductCatalogStore productCatalogStore;

    public ProductCatalogService(IProductCatalogStore productCatalogStore) {
        ArgumentNullException.ThrowIfNull(productCatalogStore);

        this.productCatalogStore = productCatalogStore;
    }

    public ProductCatalogDraft CreateNew() {
        return new ProductCatalogDraft(
            Id: Guid.NewGuid(),
            Name: string.Empty,
            NutritionBasis: NutritionBasis.Per100Grams,
            CaloriesKcal: null,
            ProteinG: null,
            FatG: null,
            CarbsG: null
        );
    }

    public ProductCatalogDraft? Load(Guid id) {
        var item = productCatalogStore.Get(id);

        if(item is null) {
            return null;
        }

        return new ProductCatalogDraft(
            Id: item.Id,
            Name: item.Name,
            NutritionBasis: item.Nutrition.Basis,
            CaloriesKcal: item.Nutrition.CaloriesKcal,
            ProteinG: item.Nutrition.ProteinG,
            FatG: item.Nutrition.FatG,
            CarbsG: item.Nutrition.CarbsG,
            Brand: item.Brand,
            Barcode: item.Barcode
        );
    }

    public IReadOnlyList<ProductCatalogItem> Search(string? query) {
        var items = productCatalogStore.GetAll();

        if(string.IsNullOrWhiteSpace(query)) {
            return items;
        }

        var normalizedQuery = query.Trim();

        return [
            .. items.Where(
                item =>
                    Contains(
                        item.Name,
                        normalizedQuery
                    )
                    || Contains(
                        item.Brand,
                        normalizedQuery
                    )
                    || Contains(
                        item.Barcode,
                        normalizedQuery
                    )
            ),
        ];
    }

    public ProductCatalogSaveResult Save(ProductCatalogDraft draft) {
        ArgumentNullException.ThrowIfNull(draft);

        var errors = Validate(draft);

        if(errors.Count > 0) {
            return new ProductCatalogSaveResult(
                IsSuccess: false,
                Errors: errors
            );
        }

        productCatalogStore.Save(
            new ProductCatalogItem(
                Id: draft.Id,
                Name: draft.Name.Trim(),
                Nutrition: new NutritionFacts(
                    Basis: draft.NutritionBasis,
                    CaloriesKcal: draft.CaloriesKcal,
                    ProteinG: draft.ProteinG,
                    FatG: draft.FatG,
                    CarbsG: draft.CarbsG
                ),
                Brand: NormalizeOptionalText(draft.Brand),
                Barcode: NormalizeOptionalText(draft.Barcode)
            )
        );

        return new ProductCatalogSaveResult(
            IsSuccess: true,
            Errors: []
        );
    }

    public bool Delete(Guid id) {
        return productCatalogStore.Delete(id);
    }

    private static IReadOnlyList<ProductCatalogValidationError> Validate(ProductCatalogDraft draft) {
        var errors = new List<ProductCatalogValidationError>();

        if(draft.Id == Guid.Empty) {
            errors.Add(ProductCatalogValidationError.MissingId);
        }

        if(string.IsNullOrWhiteSpace(draft.Name)) {
            errors.Add(ProductCatalogValidationError.MissingName);
        }

        if(!IsValidNutritionBasis(draft.NutritionBasis)) {
            errors.Add(ProductCatalogValidationError.InvalidNutritionBasis);
        }

        if(draft.CaloriesKcal < 0m) {
            errors.Add(ProductCatalogValidationError.InvalidCalories);
        }

        if(draft.ProteinG < 0m) {
            errors.Add(ProductCatalogValidationError.InvalidProtein);
        }

        if(draft.FatG < 0m) {
            errors.Add(ProductCatalogValidationError.InvalidFat);
        }

        if(draft.CarbsG < 0m) {
            errors.Add(ProductCatalogValidationError.InvalidCarbs);
        }

        return errors;
    }

    private static bool IsValidNutritionBasis(NutritionBasis nutritionBasis) {
        return nutritionBasis is
            NutritionBasis.Per100Grams
            or NutritionBasis.Per100Milliliters
            or NutritionBasis.PerItem
            or NutritionBasis.Total;
    }

    private static bool Contains(string? value, string query) {
        return value is not null && value.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizeOptionalText(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
