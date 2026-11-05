using System.Collections.Generic;
using System.Linq;

namespace BudgetProcents.Core;

public static class BudgetCalculator
{
    public const string Currency = "€";

    // Rule 50/40/10: needs / investment / wants, as a percentage of the salary.
    public const decimal NeedsTargetPercent = 50m;
    public const decimal InvestmentTargetPercent = 40m;
    public const decimal WantsTargetPercent = 10m;

    /// <summary>Adds up the expenses per category and compares each total to the 50/40/10 rule.</summary>
    public static BudgetSummary Summarize(decimal salary, IEnumerable<Expense> expenses)
    {
        List<Expense> all = [.. expenses];
        decimal Total(Category category) => all.Where(e => e.Category == category).Sum(e => e.Amount);

        decimal needs = Total(Category.Needs);
        decimal investment = Total(Category.Investment);
        decimal wants = Total(Category.Wants);
        decimal spent = needs + investment + wants;

        Share Target(decimal percent) => new(salary * percent / 100, percent);

        return new BudgetSummary
        {
            TotalSpent = spent,
            Residue = salary - spent,

            Needs = ShareOf(salary, needs),
            Investment = ShareOf(salary, investment),
            Wants = ShareOf(salary, wants),

            NeedsTarget = Target(NeedsTargetPercent),
            InvestmentTarget = Target(InvestmentTargetPercent),
            WantsTarget = Target(WantsTargetPercent),
        };
    }

    /// <summary>The amount and what percentage of the salary it is. 0% when there is no salary yet.</summary>
    public static Share ShareOf(decimal salary, decimal amount)
        => new(amount, salary == 0 ? 0 : amount / salary * 100);
}
