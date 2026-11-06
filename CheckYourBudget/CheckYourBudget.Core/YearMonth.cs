using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheckYourBudget.Core;

/// <summary>A calendar month, such as November 2026. Saved as "2026-11".</summary>
[JsonConverter(typeof(YearMonthJsonConverter))]
public readonly record struct YearMonth(int Year, int Month) : IComparable<YearMonth>
{
    public static YearMonth Of(DateTime date) => new(date.Year, date.Month);

    public static YearMonth Current => Of(DateTime.Today);

    public YearMonth Next() => Month == 12 ? new(Year + 1, 1) : new(Year, Month + 1);

    public YearMonth Previous() => Month == 1 ? new(Year - 1, 12) : new(Year, Month - 1);

    /// <summary>For example "November 2026".</summary>
    public string Title => new DateTime(Year, Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>For example "Nov 2026", for charts and narrow places.</summary>
    public string ShortTitle => new DateTime(Year, Month, 1).ToString("MMM yyyy", CultureInfo.InvariantCulture);

    public int CompareTo(YearMonth other) => (Year, Month).CompareTo((other.Year, other.Month));

    public static bool operator <(YearMonth a, YearMonth b) => a.CompareTo(b) < 0;
    public static bool operator >(YearMonth a, YearMonth b) => a.CompareTo(b) > 0;
    public static bool operator <=(YearMonth a, YearMonth b) => a.CompareTo(b) <= 0;
    public static bool operator >=(YearMonth a, YearMonth b) => a.CompareTo(b) >= 0;

    public override string ToString() => $"{Year:D4}-{Month:D2}";

    public static bool TryParse(string? text, out YearMonth month)
    {
        month = default;
        if (DateTime.TryParseExact(text, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            month = Of(date);
            return true;
        }

        return false;
    }
}

internal sealed class YearMonthJsonConverter : JsonConverter<YearMonth>
{
    public override YearMonth Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => YearMonth.TryParse(reader.GetString(), out YearMonth month)
            ? month
            : throw new JsonException($"\"{reader.GetString()}\" is not a month like \"2026-11\".");

    public override void Write(Utf8JsonWriter writer, YearMonth value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString());
}
