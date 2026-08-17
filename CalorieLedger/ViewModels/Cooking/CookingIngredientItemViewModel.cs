using CalorieLedger.Domain.Common;
using CalorieLedger.Domain.Cooking;
using CalorieLedger.Domain.Nutrition;
using CalorieLedger.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Globalization;

namespace CalorieLedger.ViewModels.Cooking;

public partial class CookingIngredientItemViewModel:ViewModelBase {
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    private readonly NutritionFacts nutrition;
    private readonly FoodUnit unit;
    private readonly Action<Guid, decimal> updateQuantity;
    private readonly Action<Guid> remove;

    public Guid Id { get; }

    public string Name { get; }
    public string SourceSummary { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NutritionSummary))]
    private decimal quantityValue;

    public string QuantityUnitSummary => unit switch {
        FoodUnit.Gram => "г",
        FoodUnit.Milliliter => "мл",
        FoodUnit.Piece => "шт.",
        FoodUnit.Portion => "порц.",
        _ => unit.ToString()
    };

    public string NutritionSummary {
        get {
            var totals = NutritionCalculator.CalculateTotal(
                nutrition,
                new FoodQuantity(
                    QuantityValue,
                    unit
                )
            );

            return $"{FormatValue(totals.CaloriesKcal)} ккал · Б: {FormatValue(totals.ProteinG)} г · Ж: {FormatValue(totals.FatG)} г · У: {FormatValue(totals.CarbsG)} г";
        }
    }

    public CookingIngredientItemViewModel(
        Guid id,
        string name,
        FoodQuantity quantity,
        NutritionFacts nutrition,
        CookingIngredientSource source,
        Action<Guid, decimal> updateQuantity,
        Action<Guid> remove
    ) {
        ArgumentNullException.ThrowIfNull(nutrition);
        ArgumentNullException.ThrowIfNull(updateQuantity);
        ArgumentNullException.ThrowIfNull(remove);

        Id = id;
        Name = name;

        quantityValue = quantity.Value;

        unit = quantity.Unit;

        this.nutrition = nutrition;
        this.updateQuantity = updateQuantity;
        this.remove = remove;

        SourceSummary = source switch {
            CookingIngredientSource.Manual => "вручную",
            CookingIngredientSource.ProductCatalog => "из каталога",
            CookingIngredientSource.FridgeItem => "из холодильника",
            CookingIngredientSource.Recipe => "из рецепта",
            _ => source.ToString()
        };
    }

    partial void OnQuantityValueChanged(decimal value) {
        updateQuantity(Id, value);
    }

    [RelayCommand]
    private void Remove() {
        remove(Id);
    }

    private static string FormatValue(decimal? value) {
        return value is null ? "—" : value.Value.ToString("0.##", RussianCulture);
    }
}
