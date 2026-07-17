using System.Linq;

namespace CalorieLedger.Application.Profiles;

public sealed class BodyMeasurementEditorService {
    private readonly BodyMeasurementHistoryService
        _historyService;

    public BodyMeasurementEditorService(
        BodyMeasurementHistoryService historyService) {
        ArgumentNullException.ThrowIfNull(
            historyService);

        _historyService = historyService;
    }

    public BodyMeasurementDraft CreateNew(
        DateOnly currentDate) {
        return new BodyMeasurementDraft(
            Id: Guid.NewGuid(),
            Date: currentDate);
    }

    public BodyMeasurementDraft? Load(
        Guid id) {
        if(id == Guid.Empty) {
            return null;
        }

        var entry =
            _historyService
                .GetAll()
                .FirstOrDefault(
                    existing =>
                        existing.Id == id);

        return entry is null
            ? null
            : BodyMeasurementDraftMapper
                .FromEntry(entry);
    }

    public BodyMeasurementSaveResult Save(
        BodyMeasurementDraft draft,
        DateOnly currentDate) {
        ArgumentNullException.ThrowIfNull(draft);

        var entry =
            BodyMeasurementDraftMapper
                .ToEntry(draft);

        return _historyService.Save(
            entry,
            currentDate);
    }

    public bool Delete(
        Guid id) {
        return _historyService.Delete(id);
    }
}