namespace CheckYourBudget.Core.Tests;

[TestClass]
public sealed class BudgetCalculatorTests
{
    private static readonly Expense[] Month =
    [
        new("Food", Category.Needs, 400),
        new("Gas", Category.Needs, 150),
        new("Going out", Category.Wants, 100),
        new("Your future", Category.Investment, 300),
        new("Emergency fund", Category.Savings, 50),
    ];

    [TestMethod]
    public void Adds_up_each_category_and_what_is_left()
    {
        BudgetSummary s = BudgetCalculator.Summarize(2000, Month, BudgetRule.FiftyFortyTen);

        Assert.AreEqual(2000m, s.Income);
        Assert.AreEqual(1000m, s.TotalSpent);
        Assert.AreEqual(1000m, s.Residue);
        Assert.AreEqual(new Share(550, 27.5m), s.Needs);
        Assert.AreEqual(new Share(100, 5), s.Wants);
        Assert.AreEqual(new Share(300, 15), s.Investment);
        Assert.AreEqual(new Share(50, 2.5m), s.Savings);
        Assert.IsFalse(s.IsOverspent);
    }

    [TestMethod]
    public void No_income_gives_zero_percent_instead_of_dividing_by_zero()
    {
        BudgetSummary s = BudgetCalculator.Summarize(0, Month, BudgetRule.FiftyFortyTen);

        Assert.AreEqual(0m, s.Needs.Percent);
        Assert.AreEqual(-1000m, s.Residue);
        Assert.IsTrue(s.IsOverspent);
    }

    [TestMethod]
    public void Fifty_forty_ten_counts_savings_together_with_investment()
    {
        BudgetSummary s = BudgetCalculator.Summarize(2000, Month, BudgetRule.FiftyFortyTen);

        CollectionAssert.AreEqual(new[] { "Needs", "Investment + Savings", "Wants" }, s.Targets.Select(t => t.Target.Name).ToArray());
        TargetCheck investment = s.Targets[1];
        Assert.AreEqual(new Share(350, 17.5m), investment.Actual);
        Assert.AreEqual(new Share(800, 40), investment.Goal);
        Assert.IsFalse(investment.IsMet, "350 is less than the 800 goal");
    }

    [TestMethod]
    public void Fifty_fifteen_five_thirty_gives_savings_its_own_target()
    {
        BudgetSummary s = BudgetCalculator.Summarize(2000, Month, BudgetRule.FiftyFifteenFiveThirty);

        CollectionAssert.AreEqual(new[] { "Needs", "Investment", "Savings", "Wants" }, s.Targets.Select(t => t.Target.Name).ToArray());
        Assert.AreEqual(new Share(300, 15), s.Targets[1].Actual);
        Assert.IsTrue(s.Targets[1].IsMet, "300 is exactly the 15% goal");
        Assert.IsFalse(s.Targets[2].IsMet, "50 is under the 5% (100) goal");
        Assert.AreEqual(600m, s.Targets[3].Goal.Amount);
    }

    [TestMethod]
    [DataRow(1000, true)]
    [DataRow(1000.01, false)]
    public void A_limit_is_met_up_to_and_including_the_limit(double needs, bool met)
    {
        BudgetSummary s = BudgetCalculator.Summarize(2000, [new("Rent", Category.Needs, (decimal)needs)], BudgetRule.FiftyFortyTen);

        Assert.AreEqual(met, s.Targets[0].IsMet);
    }

    [TestMethod]
    [DataRow(800, true)]
    [DataRow(799.99, false)]
    public void A_goal_is_met_from_the_goal_upwards(double invested, bool met)
    {
        BudgetSummary s = BudgetCalculator.Summarize(2000, [new("ETF", Category.Investment, (decimal)invested)], BudgetRule.FiftyFortyTen);

        Assert.AreEqual(met, s.Targets[1].IsMet);
    }

    [TestMethod]
    public void A_custom_rule_uses_the_users_percentages()
    {
        BudgetRule rule = BudgetRule.Custom(new CustomSplit(60, 20, 5, 15));

        BudgetSummary s = BudgetCalculator.Summarize(2000, Month, rule);

        Assert.AreEqual(BudgetRule.CustomName, rule.Name);
        CollectionAssert.AreEqual(new[] { 1200m, 400m, 100m, 300m }, s.Targets.Select(t => t.Goal.Amount).ToArray());
    }

    [TestMethod]
    [DataRow(50, 20, 10, 20, true)]
    [DataRow(50, 20, 10, 15, false)]
    [DataRow(60, 30, 10, 10, false)]
    public void A_custom_split_is_complete_only_when_it_adds_up_to_100(int needs, int investment, int savings, int wants, bool complete)
    {
        Assert.AreEqual(complete, new CustomSplit(needs, investment, savings, wants).IsComplete);
    }
}
