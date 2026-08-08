using System.Linq;

namespace CalorieLedger.Application.Profiles;

public sealed class BodyMeasurementEditorService {
    private readonly BodyMeasurementHistoryService historyService;

    public BodyMeasurementEditorService(BodyMeasurementHistoryService historyService) {
        ArgumentNullException.ThrowIfNull(historyService);

        this.historyService = historyService;
    }

    public BodyMeasurementDraft CreateNew(DateOnly currentDate) {
        return new BodyMeasurementDraft(
            Id: Guid.NewGuid(),
            Date: currentDate
        );
    }

    public BodyMeasurementDraft? Load(Guid id) {
        if(id == Guid.Empty) {
            return null;
        }

        var entry =
            historyService
                .GetAll()
                .FirstOrDefault(
                    existing =>
                        existing.Id == id);

        return entry is null
            ? null
            : BodyMeasurementDraftMapper
                .FromEntry(entry);
    }

    public BodyCompositionConsistencyResult CalculateCompositionPreview(BodyMeasurementDraft draft) {
        ArgumentNullException.ThrowIfNull(draft);

        var entry = BodyMeasurementDraftMapper.ToEntry(draft);

        return BodyCompositionConsistencyCalculator.Evaluate(entry);
    }

    public BodyMeasurementSaveResult Save(
        BodyMeasurementDraft draft,
        DateOnly currentDate) {
        ArgumentNullException.ThrowIfNull(draft);

        var entry =
            BodyMeasurementDraftMapper
                .ToEntry(draft);

        return historyService.Save(
            entry,
            currentDate);
    }

    public bool Delete(
        Guid id) {
        return historyService.Delete(id);
    }
}
