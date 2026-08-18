namespace CalorieLedger.Application.Activities;

public sealed record ActivityPreset(
    string Code,
    string Name,
    decimal MetValue,
    bool IsBuiltIn = false
) {
    public string Summary => $"{Name} · {MetValue:0.#} MET";
}
