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
    public void ConfirmDeleteCommand_PassesMeasurementId() {
        Guid? deletedId = null;
        var entry = CreateEntry();

        var viewModel = new BodyMeasurementListItemViewModel(
            entry,
            onEdit: _ => { },
            onDelete: id => deletedId = id
        );

        viewModel.DeleteCommand.Execute(null);

        Assert.Null(deletedId);
        Assert.True(viewModel.IsDeleteConfirmationVisible);

        viewModel.ConfirmDeleteCommand.Execute(null);

        Assert.Equal(
            entry.Id,
            deletedId
        );

        Assert.False(viewModel.IsDeleteConfirmationVisible);
    }

    [Fact]
    public void DeleteCommand_ShowsConfirmationWithoutDeleting() {
        var deletedId = Guid.Empty;
        var entry = CreateEntry();

        var viewModel = new BodyMeasurementListItemViewModel(
            entry,
            onEdit: _ => { },
            onDelete: id => deletedId = id
        );

        viewModel.DeleteCommand.Execute(null);

        Assert.True(viewModel.IsDeleteConfirmationVisible);
        Assert.False(viewModel.ArePrimaryActionsVisible);
        Assert.Equal(Guid.Empty, deletedId);
    }

    [Fact]
    public void ConfirmDeleteCommand_DeletesMeasurement() {
        var deletedId = Guid.Empty;
        var entry = CreateEntry();

        var viewModel = new BodyMeasurementListItemViewModel(
            entry,
            onEdit: _ => { },
            onDelete: id => deletedId = id
        );

        viewModel.DeleteCommand.Execute(null);
        viewModel.ConfirmDeleteCommand.Execute(null);

        Assert.False(viewModel.IsDeleteConfirmationVisible);
        Assert.True(viewModel.ArePrimaryActionsVisible);
        Assert.Equal(entry.Id, deletedId);
    }

    [Fact]
    public void CancelDeleteCommand_HidesConfirmationWithoutDeleting() {
        var deleteInvoked = false;

        var viewModel = new BodyMeasurementListItemViewModel(
            CreateEntry(),
            onEdit: _ => { },
            onDelete: _ => deleteInvoked = true
        );

        viewModel.DeleteCommand.Execute(null);
        viewModel.CancelDeleteCommand.Execute(null);

        Assert.False(viewModel.IsDeleteConfirmationVisible);
        Assert.True(viewModel.ArePrimaryActionsVisible);
        Assert.False(deleteInvoked);
    }

    [Fact]
    public void InitialState_ShowsPrimaryActions() {
        var viewModel = new BodyMeasurementListItemViewModel(
            CreateEntry(),
            onEdit: _ => { },
            onDelete: _ => { }
        );

        Assert.True(viewModel.ArePrimaryActionsVisible);
        Assert.False(viewModel.IsDeleteConfirmationVisible);
    }

    [Fact]
    public void DeleteConfirmation_TogglesVisibleActionGroups() {
        var viewModel = new BodyMeasurementListItemViewModel(
            CreateEntry(),
            onEdit: _ => { },
            onDelete: _ => { }
        );

        viewModel.DeleteCommand.Execute(null);

        Assert.False(viewModel.ArePrimaryActionsVisible);
        Assert.True(viewModel.IsDeleteConfirmationVisible);

        viewModel.CancelDeleteCommand.Execute(null);

        Assert.True(viewModel.ArePrimaryActionsVisible);
        Assert.False(viewModel.IsDeleteConfirmationVisible);
    }

    [Fact]
    public void Constructor_WithoutPreviousMeasurement_HidesChanges() {
        var viewModel =
        new BodyMeasurementListItemViewModel(
            entry: CreateEntry(),
            onEdit: _ => { },
            onDelete: _ => { }
        );

        Assert.False(viewModel.HasChangesSummary);
        Assert.Equal(string.Empty, viewModel.ChangesSummary);
    }

    [Fact]
    public void Constructor_WithPreviousMeasurement_FormatsChanges() {
        var previousMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 7, 20),
            WeightKg: 80m,
            BodyFatPercent: 20m,
            BoneMassKg: 3.2m,
            MuscleMassKg: 35m,
            MusclePercent: 43.75m
        );

        var currentMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 7, 26),
            WeightKg: 79.5m,
            BodyFatPercent: 19.7m,
            BoneMassKg: 3.2m,
            MuscleMassKg: 35.2m,
            MusclePercent: 44.28m
        );

        var viewModel = new BodyMeasurementListItemViewModel(
            entry: currentMeasurement,
            onEdit: _ => { },
            onDelete: _ => { },
            previousMeasurement: previousMeasurement
        );

        Assert.True(viewModel.HasChangesSummary);

        Assert.Equal(
            "За 6 дней: вес −0,5 кг · жир −0,3 п.п. · мышцы +0,2 кг",
            viewModel.ChangesSummary
        );
    }

    [Fact]
    public void Constructor_WithoutMuscleMass_UsesMusclePercentageChange() {
        var previousMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 7, 20),
            WeightKg: 80m,
            MusclePercent: 43.5m
        );

        var currentMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 7, 26),
            WeightKg: 79.5m,
            MusclePercent: 44m
        );

        var viewModel = new BodyMeasurementListItemViewModel(
            entry: currentMeasurement,
            onEdit: _ => { },
            onDelete: _ => { },
            previousMeasurement: previousMeasurement
        );

        Assert.Contains(
            "мышцы +0,5 п.п.",
            viewModel.ChangesSummary
        );
    }

    [Fact]
    public void Constructor_UnchangedMeasurement_ReportsNoChanges() {
        var previousMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 7, 19),
            WeightKg: 80m,
            BodyFatPercent: 20m,
            BoneMassKg: 3.2m,
            MuscleMassKg: 35m,
            MusclePercent: 43.75m
        );

        var currentMeasurement = previousMeasurement with
        {
            Id = Guid.NewGuid(),
            Date = new DateOnly(2026, 7, 26),
        };

        var viewModel = new BodyMeasurementListItemViewModel(
            entry: currentMeasurement,
            onEdit: _ => { },
            onDelete: _ => { },
            previousMeasurement: previousMeasurement
        );

        Assert.True(viewModel.HasChangesSummary);

        Assert.Equal(
            "За 7 дней: без изменений",
            viewModel.ChangesSummary
        );
    }

    [Theory]
    [InlineData(1, "За 1 день")]
    [InlineData(2, "За 2 дня")]
    [InlineData(5, "За 5 дней")]
    [InlineData(11, "За 11 дней")]
    [InlineData(21, "За 21 день")]
    [InlineData(24, "За 24 дня")]
    public void Constructor_FormatsMeasurementInterval(int dayCount, string expectedPrefix) {
        var previousMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 7, 1),
            WeightKg: 80m
        );

        var currentMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: previousMeasurement.Date.AddDays(dayCount),
            WeightKg: 79.5m
        );

        var viewModel = new BodyMeasurementListItemViewModel(
            entry: currentMeasurement,
            onEdit: _ => { },
            onDelete: _ => { },
            previousMeasurement: previousMeasurement
        );

        Assert.StartsWith(
            expectedPrefix,
            viewModel.ChangesSummary
        );
    }

    [Fact]
    public void Constructor_DefaultMeasurement_IsNotLatest() {
        var viewModel = new BodyMeasurementListItemViewModel(
            entry: CreateEntry(),
            onEdit: _ => { },
            onDelete: _ => { }
        );

        Assert.False(viewModel.IsLatest);
    }

    [Fact]
    public void Constructor_LatestMeasurement_IsMarkedAsLatest() {
        var viewModel = new BodyMeasurementListItemViewModel(
            entry: CreateEntry(),
            onEdit: _ => { },
            onDelete: _ => { },
            isLatest: true
        );

        Assert.True(viewModel.IsLatest);
    }

    private static BodyMeasurementEntry CreateEntry() {
        return new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(
                2026,
                7,
                18),
            WeightKg: 80m);
    }
}