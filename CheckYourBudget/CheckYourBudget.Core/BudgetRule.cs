using System.Text.Json.Serialization;

namespace CheckYourBudget.Core;

/// <summary>Whether a target is a limit to stay under or a goal to reach.</summary>
public enum TargetKind
{
    AtMost,
    AtLeast,
}

/// <summary>One part of a rule, for example "Needs: at most 50% of the income".</summary>
/// <param name="Categories">The categories whose expenses count towards this target.</param>
/// <param name="Examples">What usually goes here, shown next to the target.</param>
public sealed record RuleTarget(string Name, decimal Percent, TargetKind Kind, IReadOnlyList<Category> Categories, string Examples);

/// <summary>The percentages of a rule the user makes up themselves.</summary>
public sealed record CustomSplit(decimal Needs, decimal Investment, decimal Savings, decimal Wants)
{
    public static CustomSplit Default { get; } = new(50, 20, 10, 20);

    [JsonIgnore]
    public decimal Total => Needs + Investment + Savings + Wants;

    /// <summary>True when the parts add up to the whole income.</summary>
    [JsonIgnore]
    public bool IsComplete => Total == 100;

    public override string ToString() => $"{Needs:0.##}/{Investment:0.##}/{Savings:0.##}/{Wants:0.##}";
}

/// <summary>A way to split the income, such as 50/40/10.</summary>
public sealed record BudgetRule(string Name, IReadOnlyList<RuleTarget> Targets)
{
    public const string CustomName = "Custom";

    private const string NeedsExamples = "food, home, transport, health";
    private const string WantsExamples = "going out, entertainment, hobbies";
    private const string InvestmentExamples = "retirement, stocks/ETFs, property";
    private const string SavingsExamples = "emergency fund, short-term goals";

    /// <summary>50% needs, at least 40% investment (savings included), 10% wants.</summary>
    public static BudgetRule FiftyFortyTen { get; } = new("50/40/10",
    [
        new("Needs", 50, TargetKind.AtMost, [Category.Needs], NeedsExamples),
        new("Investment + Savings", 40, TargetKind.AtLeast, [Category.Investment, Category.Savings],
            "emergency fund, education, stocks/ETFs, property"),
        new("Wants", 10, TargetKind.AtMost, [Category.Wants], WantsExamples),
    ]);

    /// <summary>50% needs, at least 15% investment, at least 5% short-term savings, 30% wants.</summary>
    public static BudgetRule FiftyFifteenFiveThirty { get; } = new("50/15/5/30",
    [
        new("Needs", 50, TargetKind.AtMost, [Category.Needs], NeedsExamples),
        new("Investment", 15, TargetKind.AtLeast, [Category.Investment], InvestmentExamples),
        new("Savings", 5, TargetKind.AtLeast, [Category.Savings], SavingsExamples),
        new("Wants", 30, TargetKind.AtMost, [Category.Wants], WantsExamples),
    ]);

    // Declared after the rules it lists, so they are already set when this runs.
    public static IReadOnlyList<BudgetRule> BuiltIn { get; } = [FiftyFortyTen, FiftyFifteenFiveThirty];

    /// <summary>A rule with the user's own percentages, one target per category.</summary>
    public static BudgetRule Custom(CustomSplit split) => new(CustomName,
    [
        new("Needs", split.Needs, TargetKind.AtMost, [Category.Needs], NeedsExamples),
        new("Investment", split.Investment, TargetKind.AtLeast, [Category.Investment], InvestmentExamples),
        new("Savings", split.Savings, TargetKind.AtLeast, [Category.Savings], SavingsExamples),
        new("Wants", split.Wants, TargetKind.AtMost, [Category.Wants], WantsExamples),
    ]);

    /// <summary>The rule's name, so lists and screen readers show "50/40/10".</summary>
    public override string ToString() => Name;
}
