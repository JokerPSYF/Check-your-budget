namespace CheckYourBudget.Core;

public static class BudgetCalculator
{
    /// <summary>Adds up the expenses per category and compares the totals to the rule's targets.</summary>
    public static BudgetSummary Summarize(decimal income, IEnumerable<Expense> expenses, BudgetRule rule)
    {
        List<Expense> all = [.. expenses];
        decimal Total(Category category) => all.Where(e => e.Category == category).Sum(e => e.Amount);

        decimal spent = all.Sum(e => e.Amount);

        return new BudgetSummary
        {
            Income = income,
            TotalSpent = spent,
            Residue = income - spent,

            Needs = ShareOf(income, Total(Category.Needs)),
            Investment = ShareOf(income, Total(Category.Investment)),
            Savings = ShareOf(income, Total(Category.Savings)),
            Wants = ShareOf(income, Total(Category.Wants)),

            Targets =
            [
                .. rule.Targets.Select(t => new TargetCheck(t,
                    Actual: ShareOf(income, t.Categories.Sum(c => Total(c))),
                    Goal: new Share(income * t.Percent / 100, t.Percent))),
            ],
        };
    }

    /// <summary>The amount and what percentage of the income it is. 0% when there is no income yet.</summary>
    public static Share ShareOf(decimal income, decimal amount)
        => new(amount, income == 0 ? 0 : amount / income * 100);
}
