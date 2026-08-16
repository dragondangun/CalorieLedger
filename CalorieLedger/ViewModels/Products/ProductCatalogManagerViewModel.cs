using CalorieLedger.Application.Products;
using CalorieLedger.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace CalorieLedger.ViewModels.Products;

public partial class ProductCatalogManagerViewModel:ViewModelBase {
    private readonly ProductCatalogService productCatalogService;
    private readonly Action onClosed;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private ProductCatalogEditorViewModel? editor;

    public ObservableCollection<ProductCatalogListItemViewModel> Products { get; } = [];

    public bool IsEditorOpen => Editor is not null;

    public bool IsListVisible => Editor is null;

    public bool HasProducts => Products.Count > 0;

    public bool HasNoProducts => Products.Count == 0;

    public ProductCatalogManagerViewModel(
        ProductCatalogService productCatalogService,
        Action onClosed
    ) {
        ArgumentNullException.ThrowIfNull(productCatalogService);
        ArgumentNullException.ThrowIfNull(onClosed);

        this.productCatalogService = productCatalogService;

        this.onClosed = onClosed;

        RefreshProducts();
    }

    [RelayCommand]
    private void AddProduct() {
        OpenEditor(
            productCatalogService.CreateNew(),
            isNew: true
        );
    }

    [RelayCommand]
    private void Close() {
        onClosed();
    }

    partial void OnSearchQueryChanged(string value) {
        RefreshProducts();
    }

    partial void OnEditorChanged(ProductCatalogEditorViewModel? value) {
        OnPropertyChanged(nameof(IsEditorOpen));

        OnPropertyChanged(nameof(IsListVisible));
    }

    private void EditProduct(Guid id) {
        var draft = productCatalogService.Load(id);

        if(draft is null) {
            RefreshProducts();
            return;
        }

        OpenEditor(
            draft,
            isNew: false
        );
    }

    private void DeleteProduct(Guid id) {
        productCatalogService.Delete(id);

        RefreshProducts();
    }

    private void OpenEditor(ProductCatalogDraft draft, bool isNew) {
        Editor = new ProductCatalogEditorViewModel(
            productCatalogService: productCatalogService,
            draft: draft,
            isNew: isNew,
            onSaved: OnEditorSaved,
            onCancelled: CloseEditor
        );
    }

    private void OnEditorSaved() {
        Editor = null;

        RefreshProducts();
    }

    private void CloseEditor() {
        Editor = null;
    }

    private void RefreshProducts() {
        Products.Clear();

        foreach(var product in productCatalogService.Search(SearchQuery)) {
            Products.Add(
                new ProductCatalogListItemViewModel(
                    item: product,
                    onEdit: EditProduct,
                    onDelete: DeleteProduct
                )
            );
        }

        OnPropertyChanged(nameof(HasProducts));

        OnPropertyChanged(nameof(HasNoProducts));
    }
}
