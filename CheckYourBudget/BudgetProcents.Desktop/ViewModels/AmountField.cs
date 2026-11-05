using System.Globalization;

namespace BudgetProcents.Desktop.ViewModels;

/// <summary>One money input box: what the user typed, and the amount it means.</summary>
public sealed class AmountField(string label) : ObservableObject
{
    private string text = "";

    public string Label { get; } = label;

    public string Text
    {
        get => text;
        set
        {
            if (SetProperty(ref text, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    /// <summary>The typed amount, or 0 while the box is empty or invalid.</summary>
    public decimal Amount => TryParse(text, out decimal amount) ? amount : 0;

    /// <summary>True when the box has text that isn't a positive number.</summary>
    public bool HasError => !TryParse(text, out _);

    // Accepts "12,50" and "12.50" whatever the Windows number format is. No minus sign, no letters.
    private static bool TryParse(string text, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        CultureInfo culture = CultureInfo.CurrentCulture;
        string separator = culture.NumberFormat.NumberDecimalSeparator;
        string normalized = text.Trim().Replace(".", separator).Replace(",", separator);

        return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, culture, out amount);
    }
}
