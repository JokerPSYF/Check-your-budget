using CheckYourBudget.App.Services;
using CheckYourBudget.Core;

namespace CheckYourBudget.App.ViewModels;

/// <summary>One target of the chosen rule: what was spent on it next to the target.</summary>
/// <remarks>
/// A class, not a record: when a recalculated row equals the previous one, the old row's accessibility entry
/// can be reused, and it no longer points at anything on screen, so screen readers would skip that row.
/// </remarks>
public sealed class TargetRow(TargetCheck check, bool hasIncome)
{
    private bool IsOver => hasIncome && !check.IsMet && check.Target.Kind == TargetKind.AtMost;
    private bool IsUnder => hasIncome && !check.IsMet && check.Target.Kind == TargetKind.AtLeast;

    public string Name { get; } = check.Target.Name;

    public string Description { get; } =
        $"{(check.Target.Kind == TargetKind.AtMost ? "Up to" : "At least")} {check.Target.Percent:0.##}% " +
        $"({Money.Format(check.Goal.Amount)}): {check.Target.Examples}";

    public string AmountText { get; } = $"{Money.Format(check.Actual.Amount)} · {Money.FormatPercent(check.Actual.Percent)}";

    /// <summary>How full the bar is: the actual share as a part of the target, at most 1.</summary>
    public double Progress { get; } = check.Target.Percent == 0
        ? check.Actual.Amount > 0 ? 1 : 0
        : (double)Math.Min(1, check.Actual.Percent / check.Target.Percent);

    // Without an income every goal is 0 €, so "met" would mean nothing yet.
    public string StatusText { get; } = check switch
    {
        _ when !hasIncome => "",
        { IsMet: true, Target.Kind: TargetKind.AtMost } => $"{Money.Format(check.Goal.Amount - check.Actual.Amount)} left",
        { IsMet: true } => "Goal reached",
        { Target.Kind: TargetKind.AtMost } => $"{Money.Format(check.Actual.Amount - check.Goal.Amount)} over",
        _ => $"{Money.Format(check.Goal.Amount - check.Actual.Amount)} to go",
    };

    public Color StatusColor => IsOver ? AppColors.Danger : IsUnder ? AppColors.Warning : AppColors.Ok;
}

/// <summary>One line of the chart legend: a category's color, name, amount and share.</summary>
public sealed record LegendItem(Color Color, string Name, string AmountText, string PercentText)
{
    public string Description => $"{Name}: {AmountText}, {PercentText}";
}
