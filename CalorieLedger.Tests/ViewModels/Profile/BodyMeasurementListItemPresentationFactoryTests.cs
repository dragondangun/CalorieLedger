using CalorieLedger.Domain.Profile;
using CalorieLedger.ViewModels.Profile;

namespace CalorieLedger.Tests.ViewModels.Profile;

public sealed class BodyMeasurementListItemPresentationFactoryTests {
    [Fact]
    public void Create_FormatsMeasurementPresentation() {
        var currentDate = new DateOnly(2026, 8, 6);

        var entry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate,
            WeightKg: 80m,
            BodyFatPercent: 20m,
            BoneMassKg: 3.2m,
            MuscleMassKg: 35m,
            MusclePercent: 43.75m
        );

        var presentation = BodyMeasurementListItemPresentationFactory.Create(
            entry: entry,
            isLatest: true,
            currentDate: currentDate
        );

        Assert.Equal(
            "06.08.2026",
            presentation.DateSummary
        );

        Assert.Equal(
            "80,0 кг",
            presentation.WeightSummary
        );

        Assert.Contains(
            "доля мышц 43,8%",
            presentation.AdditionalValuesSummary
        );

        Assert.Equal(
            string.Empty,
            presentation.DataCompletenessText
        );

        Assert.Equal(
            "Последнее · сегодня",
            presentation.LatestBadgeText
        );

        Assert.False(presentation.IsLatestMeasurementStale);
    }

    [Fact]
    public void Create_WithPreviousMeasurement_FormatsChanges() {
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

        var presentation = BodyMeasurementListItemPresentationFactory.Create(
            entry: currentMeasurement,
            previousMeasurement: previousMeasurement
        );

        Assert.Equal(
            "За 6 дней: вес −0,5 кг · жир −0,3 п.п. · мышцы +0,2 кг",
            presentation.ChangesSummary
        );
    }

    [Fact]
    public void Create_WithoutMuscleMass_UsesMusclePercentageChange() {
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

        var presentation = BodyMeasurementListItemPresentationFactory.Create(
            entry: currentMeasurement,
            previousMeasurement: previousMeasurement
        );

        Assert.Contains(
            "доля мышц +0,5 п.п.",
            presentation.ChangesSummary
        );
    }

    [Fact]
    public void Create_WeightOnly_ReportsWeightOnly() {
        var entry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 8, 4),
            WeightKg: 80m
        );

        var presentation = BodyMeasurementListItemPresentationFactory.Create(entry);

        Assert.Equal(
            "Указан только вес",
            presentation.DataCompletenessText
        );
    }

    [Fact]
    public void Create_PartialBodyComposition_ListsMissingValues() {
        var entry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 8, 4),
            WeightKg: 80m,
            BodyFatPercent: 20m
        );

        var presentation = BodyMeasurementListItemPresentationFactory.Create(entry);

        Assert.Equal(
            "Не указаны: кости, мышцы",
            presentation.DataCompletenessText
        );
    }

    [Fact]
    public void Create_OneMissingValue_UsesSingularText() {
        var entry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 8, 4),
            WeightKg: 80m,
            BoneMassKg: 3.2m,
            MuscleMassKg: 35m
        );

        var presentation = BodyMeasurementListItemPresentationFactory.Create(entry);

        Assert.Equal(
            "Не указано: жир",
            presentation.DataCompletenessText
        );
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Create_OneMuscleRepresentation_CanBeComplete(bool useMass) {
        var entry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 8, 4),
            WeightKg: 80m,
            BodyFatPercent: 20m,
            BoneMassKg: 3.2m,
            MuscleMassKg: useMass ? 35m : null,
            MusclePercent: useMass ? null : 43.75m
        );

        var presentation = BodyMeasurementListItemPresentationFactory.Create(entry);

        Assert.Equal(
            string.Empty,
            presentation.DataCompletenessText
        );
    }

    [Theory]
    [InlineData(0, "Последнее · сегодня")]
    [InlineData(1, "Последнее · вчера")]
    [InlineData(2, "Последнее · 2 дня назад")]
    [InlineData(5, "Последнее · 5 дней назад")]
    [InlineData(21, "Последнее · 21 день назад")]
    public void Create_LatestMeasurement_FormatsFreshness(
        int dayCount,
        string expectedText)
    {
        var currentDate = new DateOnly(2026, 8, 4);

        var entry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate.AddDays(-dayCount),
            WeightKg: 80m
        );

        var presentation = BodyMeasurementListItemPresentationFactory.Create(
            entry: entry,
            isLatest: true,
            currentDate: currentDate
        );

        Assert.Equal(
            expectedText,
            presentation.LatestBadgeText
        );
    }

    [Theory]
    [InlineData(14, false)]
    [InlineData(15, true)]
    public void Create_LatestMeasurement_DetectsStaleData(
        int dayCount,
        bool expectedIsStale)
    {
        var currentDate = new DateOnly(2026, 8, 4);

        var entry = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: currentDate.AddDays(-dayCount),
            WeightKg: 80m
        );

        var presentation = BodyMeasurementListItemPresentationFactory.Create(
            entry: entry,
            isLatest: true,
            currentDate: currentDate
        );

        Assert.Equal(
            expectedIsStale,
            presentation.IsLatestMeasurementStale
        );
    }

    [Fact]
    public void Create_UnchangedMeasurement_ReportsNoChanges() {
        var previousMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 7, 19),
            WeightKg: 80m,
            BodyFatPercent: 20m,
            BoneMassKg: 3.2m,
            MuscleMassKg: 35m,
            MusclePercent: 43.75m
        );

        var currentMeasurement = previousMeasurement with {
            Id = Guid.NewGuid(),
            Date = new DateOnly(2026, 7, 26),
        };

        var presentation = BodyMeasurementListItemPresentationFactory.Create(
            entry: currentMeasurement,
            previousMeasurement: previousMeasurement
        );

        Assert.Equal(
            "За 7 дней: без изменений",
            presentation.ChangesSummary
        );
    }

    [Fact]
    public void Create_ChangeBelowDisplayPrecision_ReportsNoChanges() {
        var previousMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 8, 1),
            WeightKg: 80m
        );

        var currentMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 8, 2),
            WeightKg: 80.04m
        );

        var presentation = BodyMeasurementListItemPresentationFactory.Create(
            entry: currentMeasurement,
            previousMeasurement: previousMeasurement
        );

        Assert.Equal(
            "За 1 день: без изменений",
            presentation.ChangesSummary
        );
    }

    [Fact]
    public void Create_ChangeAtRoundingBoundary_ShowsRoundedChange() {
        var previousMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 8, 1),
            WeightKg: 80m
        );

        var currentMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 8, 2),
            WeightKg: 80.05m
        );

        var presentation = BodyMeasurementListItemPresentationFactory.Create(
            entry: currentMeasurement,
            previousMeasurement: previousMeasurement
        );

        Assert.Equal(
            "За 1 день: вес +0,1 кг",
            presentation.ChangesSummary
        );
    }

    [Fact]
    public void Create_NegativeChangeAtRoundingBoundary_ShowsRoundedChange() {
        var previousMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 8, 1),
            WeightKg: 80m
        );

        var currentMeasurement = new BodyMeasurementEntry(
            Id: Guid.NewGuid(),
            Date: new DateOnly(2026, 8, 2),
            WeightKg: 79.95m
        );

        var presentation = BodyMeasurementListItemPresentationFactory.Create(
            entry: currentMeasurement,
            previousMeasurement: previousMeasurement
        );

        Assert.Equal(
            "За 1 день: вес −0,1 кг",
            presentation.ChangesSummary
        );
    }
}