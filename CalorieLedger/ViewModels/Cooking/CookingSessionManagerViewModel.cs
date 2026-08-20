using CalorieLedger.Application.Cooking;
using CalorieLedger.Application.Fridge;
using CalorieLedger.Application.Products;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CalorieLedger.ViewModels.Cooking;

public partial class CookingSessionManagerViewModel:ViewModelBase {
    private readonly CookingSessionService cookingSessionService;
    private readonly CookingNutritionLlmService cookingNutritionLlmService;
    private readonly CookingExecutionService cookingExecutionService;
    private readonly ProductCatalogService productCatalogService;
    private readonly FridgeInventoryService fridgeInventoryService;
    private readonly DateOnly currentDate;
    private readonly Action onClosed;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    [ObservableProperty]
    private string actionSummary = string.Empty;

    [ObservableProperty]
    private CookingSessionEditorViewModel? editor;

    public ObservableCollection<CookingSessionListItemViewModel> Sessions { get; } = [];

    public bool IsEditorOpen => Editor is not null;

    public bool IsListVisible => Editor is null;

    public bool HasSessions => Sessions.Count > 0;

    public bool HasNoSessions => Sessions.Count == 0;

    public CookingSessionManagerViewModel(
        CookingSessionService cookingSessionService,
        CookingNutritionLlmService cookingNutritionLlmService,
        CookingExecutionService cookingExecutionService,
        ProductCatalogService productCatalogService,
        FridgeInventoryService fridgeInventoryService,
        DateOnly currentDate,
        Action onClosed
    ) {
        ArgumentNullException.ThrowIfNull(cookingSessionService);
        ArgumentNullException.ThrowIfNull(cookingNutritionLlmService);
        ArgumentNullException.ThrowIfNull(cookingExecutionService);
        ArgumentNullException.ThrowIfNull(productCatalogService);
        ArgumentNullException.ThrowIfNull(fridgeInventoryService);
        ArgumentNullException.ThrowIfNull(onClosed);

        this.cookingSessionService = cookingSessionService;

        this.cookingNutritionLlmService = cookingNutritionLlmService;

        this.cookingExecutionService = cookingExecutionService;

        this.productCatalogService = productCatalogService;

        this.fridgeInventoryService = fridgeInventoryService;

        this.currentDate = currentDate;

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

    private void CookSession(Guid id) {
        var result = cookingExecutionService.Execute(id, currentDate);

        ActionSummary = result.IsSuccess
            ? $"«{result.Batch!.Name}» приготовлено. {result.Batch.OutputWeightG:0.##} г добавлено в холодильник."
            : FormatExecutionErrors(result.Errors);

        RefreshSessions();
    }

    private void EditSession(Guid id) {
        if(cookingExecutionService.HasCompletedSession(id)) {
            ActionSummary = "Завершённое приготовление нельзя редактировать.";

            return;
        }

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
        if(cookingExecutionService.HasCompletedSession(id)) {
            ActionSummary = "Завершённое приготовление нельзя удалить.";

            return;
        }

        cookingSessionService.Delete(id);

        RefreshSessions();
    }

    private void OpenEditor(CookingSessionDraft draft, bool isNew) {
        Editor = new CookingSessionEditorViewModel(
            cookingSessionService: cookingSessionService,
            cookingNutritionLlmService: cookingNutritionLlmService,
            productCatalogService: productCatalogService,
            fridgeInventoryService: fridgeInventoryService,
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
                    isCompleted: cookingExecutionService.HasCompletedSession(session.Id),
                    cook: CookSession,
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

    private static string FormatExecutionErrors(IReadOnlyList<CookingExecutionError> errors) {
        return string.Join(
            " ",
            errors.Select(FormatExecutionError)
        );
    }

    private static string FormatExecutionError(CookingExecutionError error) {
        return error switch {
            CookingExecutionError.MissingSession => "Приготовление больше не существует.",
            CookingExecutionError.AlreadyCompleted => "Это приготовление уже было завершено.",
            CookingExecutionError.InvalidSession => "Параметры приготовления некорректны.",
            CookingExecutionError.MissingFridgeSource => "У одного из ингредиентов потеряна ссылка на холодильник.",
            CookingExecutionError.MissingFridgeItem => "Один из выбранных остатков больше не существует.",
            CookingExecutionError.IncompatibleFridgeQuantity => "Единица измерения остатка изменилась.",
            CookingExecutionError.InsufficientFridgeQuantity => "Для приготовления недостаточно продуктов в холодильнике.",
            _ => throw new ArgumentOutOfRangeException(
                nameof(error),
                error,
                null
            )
        };
    }
}
