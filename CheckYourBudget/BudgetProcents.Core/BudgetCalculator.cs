using System.Collections.Generic;
using System.Linq;

namespace BudgetProcents.Core;

public static class BudgetCalculator
{
    public const string Currency = "€";

    /// <summary>Adds up the expenses per category and compares the totals to the rule's targets.</summary>
    public static BudgetSummary Summarize(decimal salary, IEnumerable<Expense> expenses, BudgetRule rule)
    {
        List<Expense> all = [.. expenses];
        decimal Total(Category category) => all.Where(e => e.Category == category).Sum(e => e.Amount);

        decimal spent = all.Sum(e => e.Amount);

        return new BudgetSummary
        {
            TotalSpent = spent,
            Residue = salary - spent,

            Needs = ShareOf(salary, Total(Category.Needs)),
            Investment = ShareOf(salary, Total(Category.Investment)),
            Savings = ShareOf(salary, Total(Category.Savings)),
            Wants = ShareOf(salary, Total(Category.Wants)),

            Targets =
            [
                .. rule.Targets.Select(t => new TargetCheck(t,
                    Actual: ShareOf(salary, t.Categories.Sum(c => Total(c))),
                    Goal: new Share(salary * t.Percent / 100, t.Percent))),
            ],
        };
    }

    /// <summary>The amount and what percentage of the salary it is. 0% when there is no salary yet.</summary>
    public static Share ShareOf(decimal salary, decimal amount)
        => new(amount, salary == 0 ? 0 : amount / salary * 100);
}
