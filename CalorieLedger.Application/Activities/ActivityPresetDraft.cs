namespace CalorieLedger.Application.Activities;

public sealed record ActivityPresetDraft(
    string Code,
    string Name,
    decimal? MetValue
);
