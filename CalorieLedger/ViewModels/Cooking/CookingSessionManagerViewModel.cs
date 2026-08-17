using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Products;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace CalorieLedger.ViewModels.Cooking;

public partial class CookingSessionManagerViewModel:ViewModelBase {
    private readonly CookingSessionService cookingSessionService;
    private readonly ProductCatalogService productCatalogService;
    private readonly Action<Guid> logFood;
    private readonly Action onClosed;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private CookingSessionEditorViewModel? editor;

    public ObservableCollection<CookingSessionListItemViewModel> Sessions { get; } = [];

    public bool IsEditorOpen => Editor is not null;
    public bool IsListVisible => Editor is null;
    public bool HasSessions => Sessions.Count > 0;
    public bool HasNoSessions => Sessions.Count == 0;

    public CookingSessionManagerViewModel(
        CookingSessionService cookingSessionService,
        ProductCatalogService productCatalogService,
        Action<Guid> logFood,
        Action onClosed
    ) {
        ArgumentNullException.ThrowIfNull(cookingSessionService);
        ArgumentNullException.ThrowIfNull(productCatalogService);
        ArgumentNullException.ThrowIfNull(logFood);
        ArgumentNullException.ThrowIfNull(onClosed);

        this.cookingSessionService = cookingSessionService;

        this.productCatalogService = productCatalogService;

        this.logFood = logFood;

        this.onClosed = onClosed;

        RefreshSessions();
    }

    [RelayCommand]
    private void AddSession() {
        OpenEditor(
            cookingSessionService.CreateNew(),
            isNew: true
        );
    }

    [RelayCommand]
    private void Close() {
        onClosed();
    }

    partial void OnSearchQueryChanged(string value) {
        RefreshSessions();
    }

    private void EditSession(Guid id) {
        var draft = cookingSessionService.Load(id);

        if(draft is null) {
            RefreshSessions();
            return;
        }

        OpenEditor(
            draft,
            isNew: false
        );
    }

    private void DeleteSession(Guid id) {
        cookingSessionService.Delete(id);

        RefreshSessions();
    }

    private void OpenEditor(CookingSessionDraft draft, bool isNew) {
        Editor = new CookingSessionEditorViewModel(
            cookingSessionService: cookingSessionService,
            productCatalogService: productCatalogService,
            draft: draft,
            isNew: isNew,
            onSaved: OnEditorSaved,
            onCancelled: CloseEditor
        );
    }

    private void OnEditorSaved() {
        Editor = null;

        RefreshSessions();
    }

    private void CloseEditor() {
        Editor = null;
    }

    private void RefreshSessions() {
        Sessions.Clear();

        foreach(var session in cookingSessionService.Search(SearchQuery)) {
            Sessions.Add(
                new CookingSessionListItemViewModel(
                    session: session,
                    nutrition: cookingSessionService.CalculatePreview(session),
                    logFood: logFood,
                    edit: EditSession,
                    delete: DeleteSession
                )
            );
        }

        OnPropertyChanged(nameof(HasSessions));
        OnPropertyChanged(nameof(HasNoSessions));
    }

    partial void OnEditorChanged(CookingSessionEditorViewModel? value) {
        OnPropertyChanged(nameof(IsEditorOpen));
        OnPropertyChanged(nameof(IsListVisible));
    }
}
