using BudgetProcents.Core;

namespace BudgetProcents.Desktop.ViewModels;

public enum RuleStatus { Ok, Over, Under }

/// <summary>One line of the 50/40/10 rule: actual spending vs. the target.</summary>
/// <remarks>
/// A class, not a record: when a recalculated row equals the previous one, WPF reuses the old row's
/// accessibility entry, which no longer points at anything on screen, so screen readers skip that row.
/// </remarks>
public sealed class RuleRow(string name, Share actual, decimal targetPercent, string description, RuleStatus status)
{
    public string Name { get; } = name;
    public string Description { get; } = description;
    public RuleStatus Status { get; } = status;
    public string AmountText { get; } = Format.Money(actual.Amount);
    public string PercentText { get; } = Format.Percent(actual.Percent);
    public double Percent { get; } = (double)actual.Percent;
    public double Maximum { get; } = (double)targetPercent;
}

/// <summary>One section of the "By item" list, for example all the Needs items.</summary>
public sealed record BreakdownSection(string Title, IReadOnlyList<ItemRow> Rows);

/// <summary>One item in the "By item" list: its amount and share of the salary.</summary>
public sealed record ItemRow(string Name, Share Share)
{
    public string AmountText => Format.Money(Share.Amount);
    public string PercentText => Format.Percent(Share.Percent);
}

internal static class Format
{
    public static string Money(decimal value) => $"{value:F2} {BudgetCalculator.Currency}";
    public static string Percent(decimal value) => $"{value:F2}%";
}
