using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Profile;

namespace CalorieLedger.Tests.ViewModels.Profile;

public sealed class
    BodyMeasurementListItemViewModelTests {
    [Fact]
    public void Constructor_CreatesDisplaySummaries() {
        var entry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(
                2026,
                7,
                18),
            WeightKg: 80m,
            BodyFatPercent: 20m,
            BoneMassKg: 3.2m,
            MuscleMassKg: 35m,
            MusclePercent: 43.75m);

        var viewModel = new BodyMeasurementListItemViewModel(
            entry,
            onEdit: _ => { },
            onDelete: _ => { });

        Assert.Equal(
            "18.07.2026",
            viewModel.DateSummary);

        Assert.Equal(
            "80,0 кг",
            viewModel.WeightSummary);

        Assert.Contains(
            "жир 20,0%",
            viewModel.AdditionalValuesSummary);

        Assert.Contains(
            "кости 3,2 кг",
            viewModel.AdditionalValuesSummary);

        Assert.Contains(
            "мышцы 35,0 кг",
            viewModel.AdditionalValuesSummary);

        Assert.Contains(
            "мышцы 43,8%",
            viewModel.AdditionalValuesSummary);

        Assert.True(viewModel.HasAdditionalValues);
    }

    [Fact]
    public void EditCommand_PassesMeasurementId() {
        var entry = CreateEntry();

        Guid? passedId = null;

        var viewModel = new BodyMeasurementListItemViewModel(
            entry,
            onEdit: id => passedId = id,
            onDelete: _ => { });

        viewModel.EditCommand.Execute(null);

        Assert.Equal(
            entry.Id,
            passedId);
    }

    [Fact]
    public void DeleteCommand_PassesMeasurementId() {
        var entry = CreateEntry();

        Guid? passedId = null;

        var viewModel = new BodyMeasurementListItemViewModel(
            entry,
            onEdit: _ => { },
            onDelete: id => passedId = id);

        viewModel.DeleteCommand.Execute(null);

        Assert.Equal(
            entry.Id,
            passedId);
    }

    private static BodyMeasurementEntry
        CreateEntry() {
        return new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(
                2026,
                7,
                18),
            WeightKg: 80m);
    }
}