namespace CheckYourBudget.Core;

/// <summary>
/// What kind of expense something is. Each rule decides which of its targets a category counts towards.
/// Listed in the order the app shows them.
/// </summary>
public enum Category
{
    Needs,
    Wants,
    Investment,

    /// <summary>Short-term savings, such as an emergency fund.</summary>
    Savings,
}

/// <summary>One thing the income goes to, for example 400 € of food in Needs.</summary>
public sealed record Expense(string Name, Category Category, decimal Amount);
