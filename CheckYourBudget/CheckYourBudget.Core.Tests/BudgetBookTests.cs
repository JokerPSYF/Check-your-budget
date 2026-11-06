namespace CheckYourBudget.Core.Tests;

[TestClass]
public sealed class BudgetBookTests
{
    private static readonly YearMonth September = new(2026, 9);
    private static readonly YearMonth October = new(2026, 10);
    private static readonly YearMonth November = new(2026, 11);

    private static MonthBudget Filled(YearMonth month, string food) => new()
    {
        Month = month,
        Income = [new("Salary", "2000")],
        Needs = [new("Food", food)],
    };

    [TestMethod]
    public void A_first_month_starts_with_the_default_items()
    {
        MonthBudget draft = new BudgetBook().Draft(November);

        Assert.AreEqual(November, draft.Month);
        Assert.AreEqual("Salary", draft.Income.Single().Name);
        Assert.IsTrue(draft.Needs.Count > 0);
        Assert.AreEqual("Emergency fund", draft.Savings.Single().Name);
        Assert.IsNull(new BudgetBook().DraftSource(November));
    }

    [TestMethod]
    public void A_new_month_copies_the_closest_earlier_month_with_its_amounts()
    {
        BudgetBook book = new();
        book.Add(Filled(September, "300"));
        book.Add(Filled(October, "400"));

        MonthBudget draft = book.Draft(November);

        Assert.AreEqual(November, draft.Month);
        Assert.AreEqual("400", draft.Needs.Single().Amount);
        Assert.AreEqual(October, book.DraftSource(November));
    }

    [TestMethod]
    public void A_month_before_all_others_copies_the_earliest_one()
    {
        BudgetBook book = new();
        book.Add(Filled(October, "400"));
        book.Add(Filled(November, "500"));

        Assert.AreEqual("400", book.Draft(September).Needs.Single().Amount);
    }

    [TestMethod]
    public void A_draft_is_not_kept_until_it_is_added()
    {
        BudgetBook book = new();
        book.Add(Filled(October, "400"));

        MonthBudget draft = book.Draft(November);
        draft.Needs.Add(new("Gas", "50"));

        Assert.IsNull(book.Find(November));
        Assert.AreEqual(1, book.Find(October)!.Needs.Count, "changing the copy must not change the month it came from");

        book.Add(draft);
        Assert.AreSame(draft, book.Find(November));
    }

    [TestMethod]
    public void Adding_a_month_again_replaces_it_and_keeps_months_in_order()
    {
        BudgetBook book = new();
        book.Add(Filled(November, "500"));
        book.Add(Filled(September, "300"));
        book.Add(Filled(November, "550"));

        CollectionAssert.AreEqual(new[] { September, November }, book.Months.Select(m => m.Month).ToArray());
        Assert.AreEqual("550", book.Find(November)!.Needs.Single().Amount);
    }

    [TestMethod]
    public void The_chosen_rule_is_found_by_name()
    {
        Assert.AreSame(BudgetRule.FiftyFortyTen, new BudgetBook().CurrentRule());
        Assert.AreSame(BudgetRule.FiftyFifteenFiveThirty, new BudgetBook { Rule = "50/15/5/30" }.CurrentRule());
        Assert.AreSame(BudgetRule.FiftyFortyTen, new BudgetBook { Rule = "no such rule" }.CurrentRule());

        BudgetRule custom = new BudgetBook { Rule = BudgetRule.CustomName, CustomSplit = new(60, 20, 5, 15) }.CurrentRule();
        CollectionAssert.AreEqual(new[] { 60m, 20m, 5m, 15m }, custom.Targets.Select(t => t.Percent).ToArray());
    }

    [TestMethod]
    public void A_month_adds_up_its_income_and_ignores_amounts_that_are_not_numbers()
    {
        MonthBudget month = new()
        {
            Month = November,
            Income = [new("Salary", "2000"), new("Side job", "300,50"), new("Typo", "abc")],
            Needs = [new("Food", "400")],
            Savings = [new("Emergency fund", "100")],
        };

        BudgetSummary s = month.Summarize(BudgetRule.FiftyFortyTen);

        Assert.AreEqual(2300.50m, s.Income);
        Assert.AreEqual(500m, s.TotalSpent);
    }
}
