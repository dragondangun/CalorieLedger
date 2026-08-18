using CalorieLedger.Application.Activities;

namespace CalorieLedger.Application.Tests.Activities;

public sealed class ActivityEditorServiceTests {
    [Fact]
    public void Save_ValidDraft_NormalizesAndPersistsActivity() {
        var currentDate = new DateOnly(2026, 8, 18);

        var store = new InMemoryActivityStore();

        var service = new ActivityEditorService(store);

        var id = Guid.NewGuid();

        var result = service.Save(
            new ActivityDraft(
                Id: id,
                Date: currentDate,
                Name: "  HEMA  ",
                BurnedCaloriesKcal: 350m,
                StartedAt: new TimeOnly(18, 30),
                Duration: TimeSpan.FromMinutes(75),
                Note: "  Интенсивная тренировка  "
            ),
            currentDate
        );

        Assert.True(result.IsSuccess);

        var saved = Assert.IsType<CalorieLedger.Domain.Activities.ActivityEntry>(store.Get(id));

        Assert.Equal(
            "HEMA",
            saved.Name
        );

        Assert.Equal(
            350m,
            saved.BurnedCaloriesKcal
        );

        Assert.Equal(
            TimeSpan.FromMinutes(75),
            saved.Duration
        );

        Assert.Equal(
            "Интенсивная тренировка",
            saved.Note
        );
    }

    [Fact]
    public void Save_InvalidDraft_ReturnsValidationErrorsWithoutPersistence() {
        var currentDate = new DateOnly(2026, 8, 18);

        var store = new InMemoryActivityStore();

        var service = new ActivityEditorService(store);

        var result = service.Save(
                new ActivityDraft(
                    Id: Guid.Empty,
                    Date: currentDate.AddDays(1),
                    Name: " ",
                    BurnedCaloriesKcal: null,
                    Duration: TimeSpan.Zero
                ),
                currentDate
            );

        Assert.False(result.IsSuccess);

        Assert.Contains(
            ActivityValidationError.MissingId,
            result.Errors
        );

        Assert.Contains(
            ActivityValidationError.FutureDate,
            result.Errors
        );

        Assert.Contains(
            ActivityValidationError.MissingName,
            result.Errors
        );

        Assert.Contains(
            ActivityValidationError.InvalidBurnedCalories,
            result.Errors
        );

        Assert.Contains(
            ActivityValidationError.InvalidDuration,
            result.Errors
        );

        Assert.Empty(
            store.Get(
                currentDate,
                currentDate.AddDays(
                    1
                )
            )
        );
    }

    [Fact]
    public void Load_SavedActivity_ReturnsEditableDraft() {
        var currentDate = new DateOnly(2026, 8, 18);

        var store = new InMemoryActivityStore();

        var service = new ActivityEditorService(store);

        var draft = service.CreateNew(currentDate) with {
            Name = "Ходьба",
            BurnedCaloriesKcal = 180m,
            Duration = TimeSpan.FromMinutes(45),
        };

        Assert.True(
            service.Save(
                draft,
                currentDate
            ).IsSuccess
        );

        var loaded = Assert.IsType<ActivityDraft>(
            service.Load(
                draft.Id
            )
        );

        Assert.Equal(
            draft.Id,
            loaded.Id
        );

        Assert.Equal(
            "Ходьба",
            loaded.Name
        );

        Assert.Equal(
            180m,
            loaded.BurnedCaloriesKcal
        );

        Assert.Equal(
            TimeSpan.FromMinutes(45),
            loaded.Duration
        );
    }
}
