namespace CheckYourBudget.Core;

/// <summary>An amount of money and the percentage of the income it represents.</summary>
public readonly record struct Share(decimal Amount, decimal Percent);

/// <summary>One target of a rule next to what was actually spent on it.</summary>
public sealed record TargetCheck(RuleTarget Target, Share Actual, Share Goal)
{
    public bool IsMet => Target.Kind == TargetKind.AtMost
        ? Actual.Amount <= Goal.Amount
        : Actual.Amount >= Goal.Amount;
}

/// <summary>Income, total spending, what is left, each category's share, and how it compares to the chosen rule.</summary>
public sealed record BudgetSummary
{
    public required decimal Income { get; init; }
    public required decimal TotalSpent { get; init; }
    public required decimal Residue { get; init; }

    public required Share Needs { get; init; }
    public required Share Investment { get; init; }
    public required Share Savings { get; init; }
    public required Share Wants { get; init; }

    /// <summary>The rule's targets, in the rule's order.</summary>
    public required IReadOnlyList<TargetCheck> Targets { get; init; }

    public bool IsOverspent => Residue < 0;

    public Share ShareOf(Category category) => category switch
    {
        Category.Needs => Needs,
        Category.Investment => Investment,
        Category.Savings => Savings,
        Category.Wants => Wants,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
    };
}
