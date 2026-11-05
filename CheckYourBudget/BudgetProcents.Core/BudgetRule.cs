using System.Collections.Generic;

namespace BudgetProcents.Core;

/// <summary>Whether a target is a limit to stay under or a goal to reach.</summary>
public enum TargetKind
{
    AtMost,
    AtLeast,
}

/// <summary>One part of a rule, for example "Needs: at most 50% of the salary".</summary>
/// <param name="Categories">The categories whose expenses count towards this target.</param>
/// <param name="Examples">What usually goes here, shown next to the target.</param>
public sealed record RuleTarget(string Name, decimal Percent, TargetKind Kind, IReadOnlyList<Category> Categories, string Examples);

/// <summary>A way to split the salary, such as 50/40/10.</summary>
public sealed record BudgetRule(string Name, IReadOnlyList<RuleTarget> Targets)
{
    /// <summary>50% needs, at least 40% investment (savings included), 10% wants.</summary>
    public static BudgetRule FiftyFortyTen { get; } = new("50/40/10",
    [
        new("Needs", 50, TargetKind.AtMost, [Category.Needs], "food, home, transport, health"),
        new("Investment + Savings", 40, TargetKind.AtLeast, [Category.Investment, Category.Savings],
            "emergency fund, education, stocks/ETFs, property"),
        new("Wants", 10, TargetKind.AtMost, [Category.Wants], "going out, entertainment, hobbies"),
    ]);

    /// <summary>50% needs, at least 15% investment, at least 5% short-term savings, 30% wants.</summary>
    public static BudgetRule FiftyFifteenFiveThirty { get; } = new("50/15/5/30",
    [
        new("Needs", 50, TargetKind.AtMost, [Category.Needs], "food, home, transport, health"),
        new("Investment", 15, TargetKind.AtLeast, [Category.Investment], "retirement, stocks/ETFs, property"),
        new("Savings", 5, TargetKind.AtLeast, [Category.Savings], "emergency fund, short-term goals"),
        new("Wants", 30, TargetKind.AtMost, [Category.Wants], "going out, entertainment, hobbies"),
    ]);

    // Declared after the rules it lists, so they are already set when this runs.
    public static IReadOnlyList<BudgetRule> All { get; } = [FiftyFortyTen, FiftyFifteenFiveThirty];

    /// <summary>The rule's name, so lists and screen readers show "50/40/10".</summary>
    public override string ToString() => Name;
}
