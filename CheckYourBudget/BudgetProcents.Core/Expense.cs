namespace BudgetProcents.Core;

/// <summary>The part of the 50/40/10 rule an expense counts towards.</summary>
public enum Category
{
    Needs,
    Investment,
    Wants,
}

/// <summary>One thing the salary goes to, for example 400 € of food in Needs.</summary>
public sealed record Expense(string Name, Category Category, decimal Amount);
