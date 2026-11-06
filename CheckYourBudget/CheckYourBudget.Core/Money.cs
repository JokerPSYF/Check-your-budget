using System.Globalization;

namespace CheckYourBudget.Core;

/// <summary>Reading amounts the way people type them, and showing them.</summary>
public static class Money
{
    public const string Currency = "€";

    /// <summary>
    /// Reads "12,50" and "12.50" alike, whatever the device's number format is. An empty box means 0.
    /// No minus sign, no letters, no thousands separators.
    /// </summary>
    public static bool TryParse(string? text, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        string normalized = text.Trim().Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount);
    }

    /// <summary>The typed amount, or 0 when the text isn't a valid amount.</summary>
    public static decimal Parse(string? text) => TryParse(text, out decimal amount) ? amount : 0;

    /// <summary>For example "1 234,50 €" (the separators follow the device's settings).</summary>
    public static string Format(decimal value) => $"{value:N2} {Currency}";

    public static string FormatPercent(decimal value) => $"{value:F2}%";
}
