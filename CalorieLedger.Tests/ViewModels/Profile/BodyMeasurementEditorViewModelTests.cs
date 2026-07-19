using CalorieLedger.Application.Profiles;
using CalorieLedger.ViewModels.Profile;

namespace CalorieLedger.Tests.ViewModels.Profile;

public sealed class
    BodyMeasurementEditorViewModelTests {
    [Fact]
    public void Constructor_LoadsDraftValues() {
        var editorService = CreateEditorService();

        var date = new DateOnly(
            2026,
            7,
            18);

        var draft = new BodyMeasurementDraft(
            Id: Guid.NewGuid(),
            Date: date,
            WeightKg: 80m,
            BodyFatPercent: 20m,
            BoneMassKg: 3.2m,
            MuscleMassKg: 35m,
            MusclePercent: 43.75m);

        var viewModel = new BodyMeasurementEditorViewModel(
            editorService,
            draft,
            currentDate: date,
            onSaved: () => { },
            onCancelled: () => { });

        Assert.Equal(
            date,
            DateOnly.FromDateTime(
                viewModel
                    .MeasurementDate!
                    .Value
                    .DateTime));

        Assert.Equal(80m, viewModel.WeightKg);

        Assert.Equal(
            20m,
            viewModel.BodyFatPercent);

        Assert.Equal(
            3.2m,
            viewModel.BoneMassKg);

        Assert.Equal(
            35m,
            viewModel.MuscleMassKg);

        Assert.Equal(
            43.75m,
            viewModel.MusclePercent);
    }

    [Fact]
    public void SaveCommand_ValidValues_InvokesSavedCallback() {
        var editorService = CreateEditorService();

        var currentDate = new DateOnly(
            2026,
            7,
            18);

        var callbackInvoked = false;

        var viewModel = new BodyMeasurementEditorViewModel(
            editorService,
            editorService.CreateNew(currentDate),
            currentDate,
            onSaved: () => callbackInvoked = true,
            onCancelled: () => { });

        viewModel.WeightKg = 80m;
        viewModel.BodyFatPercent = 20m;

        viewModel.SaveCommand.Execute(null);

        Assert.True(callbackInvoked);
        Assert.False(viewModel.HasValidationErrors);

        Assert.Empty(viewModel.ValidationMessages);
    }

    [Fact]
    public void SaveCommand_MissingWeight_ShowsValidationError() {
        var editorService = CreateEditorService();

        var currentDate = new DateOnly(
            2026,
            7,
            18);

        var callbackInvoked = false;

        var viewModel = new BodyMeasurementEditorViewModel(
            editorService,
            editorService.CreateNew(currentDate),
            currentDate,
            onSaved: () => callbackInvoked = true,
            onCancelled: () => { });

        viewModel.SaveCommand.Execute(null);

        Assert.False(callbackInvoked);
        Assert.True(viewModel.HasValidationErrors);

        Assert.Contains(
            "Укажите положительное значение веса.",
            viewModel.ValidationMessages);
    }

    [Fact]
    public void CancelCommand_InvokesCancellationCallback() {
        var editorService = CreateEditorService();

        var currentDate = new DateOnly(
            2026,
            7,
            18);

        var callbackInvoked = false;

        var viewModel = new BodyMeasurementEditorViewModel(
            editorService,
            editorService.CreateNew(currentDate),
            currentDate,
            onSaved: () => { },
            onCancelled: () => callbackInvoked = true);

        viewModel.CancelCommand.Execute(null);

        Assert.True(callbackInvoked);
    }

    private static BodyMeasurementEditorService
        CreateEditorService() {
        var store = new InMemoryBodyMeasurementStore();

        var historyService = new BodyMeasurementHistoryService(store);

        return new BodyMeasurementEditorService(historyService);
    }

    [Fact]
    public void MuscleMassChanged_CalculatesMusclePercent() {
        var editorService = CreateEditorService();

        var currentDate = new DateOnly(
            2026,
            7,
            19);

        var viewModel = new BodyMeasurementEditorViewModel(
            editorService,
            editorService.CreateNew(currentDate),
            currentDate,
            onSaved: () => { },
            onCancelled: () => { });

        viewModel.WeightKg = 80m;
        viewModel.MuscleMassKg = 35m;

        Assert.Equal(
            43.75m,
            viewModel.MusclePercent);
    }

    [Fact]
    public void MusclePercentChanged_CalculatesMuscleMass() {
        var editorService = CreateEditorService();

        var currentDate = new DateOnly(
            2026,
            7,
            19);

        var viewModel = new BodyMeasurementEditorViewModel(
            editorService,
            editorService.CreateNew(currentDate),
            currentDate,
            onSaved: () => { },
            onCancelled: () => { });

        viewModel.WeightKg = 80m;
        viewModel.MusclePercent = 40m;

        Assert.Equal(
            32m,
            viewModel.MuscleMassKg);
    }

    [Fact]
    public void WeightChanged_AfterMuscleMassInput_RecalculatesPercent() {
        var editorService = CreateEditorService();

        var currentDate = new DateOnly(
            2026,
            7,
            19);

        var viewModel = new BodyMeasurementEditorViewModel(
            editorService,
            editorService.CreateNew(currentDate),
            currentDate,
            onSaved: () => { },
            onCancelled: () => { });

        viewModel.WeightKg = 80m;
        viewModel.MuscleMassKg = 35m;

        viewModel.WeightKg = 70m;

        Assert.Equal(
            50m,
            viewModel.MusclePercent);
    }

    [Fact]
    public void WeightChanged_AfterMusclePercentInput_RecalculatesMass() {
        var editorService = CreateEditorService();

        var currentDate = new DateOnly(
            2026,
            7,
            19);

        var viewModel = new BodyMeasurementEditorViewModel(
            editorService,
            editorService.CreateNew( currentDate),
            currentDate,
            onSaved: () => { },
            onCancelled: () => { });

        viewModel.WeightKg = 80m;
        viewModel.MusclePercent = 40m;

        viewModel.WeightKg = 75m;

        Assert.Equal(
            30m,
            viewModel.MuscleMassKg);
    }

    [Fact]
    public void CompositionValuesChanged_UpdatesPreview() {
        var editorService = CreateEditorService();

        var currentDate = new DateOnly(
            2026,
            7,
            19);

        var viewModel = new BodyMeasurementEditorViewModel(
            editorService,
            editorService.CreateNew(currentDate),
            currentDate,
            onSaved: () => { },
            onCancelled: () => { });

        viewModel.WeightKg = 80m;
        viewModel.BodyFatPercent = 20m;
        viewModel.BoneMassKg = 3.2m;
        viewModel.MuscleMassKg = 35m;

        Assert.True(
            viewModel.HasCompositionPreview);

        Assert.False(
            viewModel.HasCompositionWarning);

        Assert.Contains(
            "Жировая масса: 16,00 кг",
            viewModel.CompositionPreviewSummary);

        Assert.Contains(
            "Безжировая масса: 64,00 кг",
            viewModel.CompositionPreviewSummary);

        Assert.Contains(
            "Доля костной массы: 4,00%",
            viewModel.CompositionPreviewSummary);

        Assert.Contains(
            "Остальная масса: 25,80 кг",
            viewModel.CompositionPreviewSummary);
    }

    [Fact]
    public void InconsistentComposition_ShowsWarning() {
        var editorService = CreateEditorService();

        var currentDate = new DateOnly(
            2026,
            7,
            19);

        var viewModel = new BodyMeasurementEditorViewModel(
            editorService,
            editorService.CreateNew(currentDate),
            currentDate,
            onSaved: () => { },
            onCancelled: () => { });

        viewModel.WeightKg = 80m;
        viewModel.BodyFatPercent = 30m;
        viewModel.BoneMassKg = 3m;
        viewModel.MuscleMassKg = 55m;

        Assert.True(viewModel.HasCompositionPreview);

        Assert.True(viewModel.HasCompositionWarning);
    }
}