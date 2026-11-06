namespace CheckYourBudget.Core.Tests;

[TestClass]
public sealed class YearMonthTests
{
    [TestMethod]
    public void Moves_across_the_new_year_both_ways()
    {
        Assert.AreEqual(new YearMonth(2027, 1), new YearMonth(2026, 12).Next());
        Assert.AreEqual(new YearMonth(2025, 12), new YearMonth(2026, 1).Previous());
        Assert.AreEqual(new YearMonth(2026, 6), new YearMonth(2026, 5).Next());
    }

    [TestMethod]
    public void Is_saved_as_year_dash_month_and_read_back()
    {
        Assert.AreEqual("2026-03", new YearMonth(2026, 3).ToString());
        Assert.IsTrue(YearMonth.TryParse("2026-03", out YearMonth month));
        Assert.AreEqual(new YearMonth(2026, 3), month);
        Assert.IsFalse(YearMonth.TryParse("March 2026", out _));
        Assert.IsFalse(YearMonth.TryParse("2026-13", out _));
    }

    [TestMethod]
    public void Shows_the_month_name_in_english()
    {
        Assert.AreEqual("November 2026", new YearMonth(2026, 11).Title);
        Assert.AreEqual("Nov 2026", new YearMonth(2026, 11).ShortTitle);
    }

    [TestMethod]
    public void Sorts_by_year_then_month()
    {
        Assert.IsTrue(new YearMonth(2025, 12) < new YearMonth(2026, 1));
        Assert.IsTrue(new YearMonth(2026, 2) > new YearMonth(2026, 1));
        Assert.IsTrue(new YearMonth(2026, 2) >= new YearMonth(2026, 2));
    }
}
