using BudgetProcents.Core;

namespace BudgetProcents.Desktop.ViewModels;

public enum RuleStatus { Ok, Over, Under }

/// <summary>One line of the 50/40/10 rule: actual spending vs. the target.</summary>
public sealed record RuleRow(string Name, Share Actual, decimal TargetPercent, string Description, RuleStatus Status)
{
    public string AmountText => Format.Money(Actual.Amount);
    public string PercentText => Format.Percent(Actual.Percent);
    public double Percent => (double)Actual.Percent;
    public double Maximum => (double)TargetPercent;
}

/// <summary>One spending group in the "By group" list.</summary>
public sealed record GroupRow(string Name, string? Details, Share Share)
{
    public string AmountText => Format.Money(Share.Amount);
    public string PercentText => Format.Percent(Share.Percent);
}

internal static class Format
{
    public static string Money(decimal value) => $"{value:F2} {BudgetCalculator.Currency}";
    public static string Percent(decimal value) => $"{value:F2}%";
}
