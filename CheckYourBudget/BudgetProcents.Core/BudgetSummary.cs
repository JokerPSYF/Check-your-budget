namespace BudgetProcents.Core;

/// <summary>An amount of money and the percentage of the salary it represents.</summary>
public readonly record struct Share(decimal Amount, decimal Percent);

/// <summary>Total spending, what is left, and how each category compares to the 50/40/10 rule.</summary>
public sealed record BudgetSummary
{
    public required decimal TotalSpent { get; init; }
    public required decimal Residue { get; init; }

    public required Share Needs { get; init; }
    public required Share Investment { get; init; }
    public required Share Wants { get; init; }

    public required Share NeedsTarget { get; init; }
    public required Share InvestmentTarget { get; init; }
    public required Share WantsTarget { get; init; }

    public bool NeedsOverTarget => Needs.Amount > NeedsTarget.Amount;
    public bool WantsOverTarget => Wants.Amount > WantsTarget.Amount;
    public bool InvestmentUnderTarget => Investment.Amount < InvestmentTarget.Amount;
    public bool IsOverspent => Residue < 0;
}
