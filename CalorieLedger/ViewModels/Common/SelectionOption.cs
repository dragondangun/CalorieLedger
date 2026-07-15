namespace CalorieLedger.ViewModels.Common;

public sealed record SelectionOption<T>(
    T Value,
    string DisplayName);