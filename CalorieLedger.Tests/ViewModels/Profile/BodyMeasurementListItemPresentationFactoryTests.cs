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
}