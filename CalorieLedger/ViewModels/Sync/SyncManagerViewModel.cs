using CalorieLedger.Application.Sync;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalorieLedger.ViewModels.Sync;

public sealed partial class SyncManagerViewModel:ViewModelBase {
    private readonly SyncSnapshotService syncSnapshotService;
    private readonly Action onClosed;
    private readonly Action? onApplied;
    private SyncSnapshot? pendingSnapshot;

    [ObservableProperty]
    private string exportText = string.Empty;

    [ObservableProperty]
    private string importText = string.Empty;

    [ObservableProperty]
    private string previewSummary = string.Empty;

    [ObservableProperty]
    private string actionSummary = string.Empty;

    [ObservableProperty]
    private bool hasPreview;

    public string DeviceIdSummary => syncSnapshotService.DeviceIdentity.Id.ToString("D");

    public bool CanApply => pendingSnapshot is not null;

    public SyncManagerViewModel(
        SyncSnapshotService syncSnapshotService,
        Action onClosed,
        Action? onApplied = null
    ) {
        ArgumentNullException.ThrowIfNull(syncSnapshotService);
        ArgumentNullException.ThrowIfNull(onClosed);

        this.syncSnapshotService = syncSnapshotService;
        this.onClosed = onClosed;
        this.onApplied = onApplied;

        RegenerateExport();
    }

    [RelayCommand]
    private void RefreshExport() {
        RegenerateExport();
        ActionSummary = "Снимок этого устройства обновлён.";
    }

    [RelayCommand]
    private void ValidateImport() {
        ClearPendingPreview();

        var result = syncSnapshotService.Parse(ImportText);

        if(!result.IsSuccess || result.Snapshot is null) {
            ActionSummary = FormatErrors(result.Errors);
            return;
        }

        pendingSnapshot = result.Snapshot;
        var preview = syncSnapshotService.Preview(result.Snapshot);
        PreviewSummary = FormatPreview(preview);
        HasPreview = true;
        ActionSummary = preview.HasChanges
            ? "Снимок проверен. Изменения можно применить."
            : "Снимок проверен. Новых изменений нет.";

        OnPropertyChanged(nameof(CanApply));
        ApplyImportCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private void ApplyImport() {
        if(pendingSnapshot is null) {
            return;
        }

        var result = syncSnapshotService.Apply(pendingSnapshot);
        pendingSnapshot = null;
        HasPreview = false;
        PreviewSummary = string.Empty;
        OnPropertyChanged(nameof(CanApply));
        ApplyImportCommand.NotifyCanExecuteChanged();
        RegenerateExport();
        ActionSummary = FormatApplyResult(result);
        onApplied?.Invoke();
    }

    [RelayCommand]
    private void Close() {
        onClosed();
    }

    partial void OnImportTextChanged(string value) {
        ClearPendingPreview();
        ActionSummary = string.Empty;
    }

    private void RegenerateExport() {
        ExportText = syncSnapshotService.CreateExport();
    }

    private void ClearPendingPreview() {
        pendingSnapshot = null;
        HasPreview = false;
        PreviewSummary = string.Empty;
        OnPropertyChanged(nameof(CanApply));
        ApplyImportCommand.NotifyCanExecuteChanged();
    }

    private static string FormatPreview(SyncSnapshotPreview preview) {
        var text = "Холодильник: "
            + $"новых {preview.FridgeAdded}, изменений {preview.FridgeUpdated}, "
            + $"без изменений {preview.FridgeUnchanged}. "
            + "Незавершённые приготовления: "
            + $"новых {preview.CookingSessionsAdded}, изменений {preview.CookingSessionsUpdated}, "
            + $"без изменений {preview.CookingSessionsUnchanged}.";

        if(preview.CompletedCookingSessionConflicts > 0) {
            text += " "
                + $"Пропущено уже завершённых приготовлений: {preview.CompletedCookingSessionConflicts}.";
        }

        return text;
    }

    private static string FormatApplyResult(SyncSnapshotApplyResult result) {
        var text = "Синхронизация применена: "
            + $"холодильник +{result.FridgeAdded}/~{result.FridgeUpdated}, "
            + $"приготовления +{result.CookingSessionsAdded}/~{result.CookingSessionsUpdated}.";

        if(result.CompletedCookingSessionConflicts > 0) {
            text += " "
                + $"Уже завершённых приготовлений не изменено: {result.CompletedCookingSessionConflicts}.";
        }

        return text;
    }

    private static string FormatErrors(IReadOnlyList<SyncSnapshotParseError> errors) {
        if(errors.Count == 0) {
            return "Не удалось проверить снимок синхронизации.";
        }

        var descriptions = errors
            .Distinct()
            .Select(FormatError);

        return "Снимок не принят: " + string.Join("; ", descriptions) + ".";
    }

    private static string FormatError(SyncSnapshotParseError error) {
        return error switch {
            SyncSnapshotParseError.EmptyInput => "вставьте JSON второго устройства",
            SyncSnapshotParseError.InvalidJson => "некорректный JSON",
            SyncSnapshotParseError.UnsupportedProtocol => "неподдерживаемая версия протокола",
            SyncSnapshotParseError.MissingSnapshotId => "не указан идентификатор снимка",
            SyncSnapshotParseError.MissingSourceDeviceId => "не указан идентификатор устройства",
            SyncSnapshotParseError.OwnDeviceSnapshot => "это снимок текущего устройства",
            SyncSnapshotParseError.DuplicateFridgeItem => "дублируются позиции холодильника",
            SyncSnapshotParseError.DuplicateCookingSession => "дублируются приготовления",
            SyncSnapshotParseError.InvalidFridgeItem => "есть некорректная позиция холодильника",
            SyncSnapshotParseError.InvalidCookingSession => "есть некорректное приготовление",
            _ => "неизвестная ошибка",
        };
    }
}
