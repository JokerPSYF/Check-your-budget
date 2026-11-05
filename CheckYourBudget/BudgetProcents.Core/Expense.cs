namespace BudgetProcents.Core;

/// <summary>What kind of expense something is. Each rule decides which of its targets a category counts towards.</summary>
public enum Category
{
    Needs,
    Investment,
    Wants,

    /// <summary>Short-term savings, such as an emergency fund.</summary>
    Savings,
}

/// <summary>One thing the salary goes to, for example 400 € of food in Needs.</summary>
public sealed record Expense(string Name, Category Category, decimal Amount);
