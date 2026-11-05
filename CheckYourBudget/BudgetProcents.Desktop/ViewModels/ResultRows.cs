using BudgetProcents.Core;

namespace BudgetProcents.Desktop.ViewModels;

public enum RuleStatus { Ok, Over, Under }

/// <summary>One target of the chosen rule: actual spending vs. the target.</summary>
/// <remarks>
/// A class, not a record: when a recalculated row equals the previous one, WPF reuses the old row's
/// accessibility entry, which no longer points at anything on screen, so screen readers skip that row.
/// </remarks>
public sealed class RuleRow(TargetCheck check)
{
    public string Name { get; } = check.Target.Name;

    public string Description { get; } =
        $"{(check.Target.Kind == TargetKind.AtMost ? "Up to" : "At least")} {check.Target.Percent}% " +
        $"({Format.Money(check.Goal.Amount)}): {check.Target.Examples}";

    public RuleStatus Status { get; } =
        check.IsMet ? RuleStatus.Ok
        : check.Target.Kind == TargetKind.AtMost ? RuleStatus.Over
        : RuleStatus.Under;

    public string AmountText { get; } = Format.Money(check.Actual.Amount);
    public string PercentText { get; } = Format.Percent(check.Actual.Percent);
    public double Percent { get; } = (double)check.Actual.Percent;
    public double Maximum { get; } = (double)check.Target.Percent;
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
