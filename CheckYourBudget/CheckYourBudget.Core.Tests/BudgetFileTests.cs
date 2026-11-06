namespace CheckYourBudget.Core.Tests;

[TestClass]
public sealed class BudgetFileTests
{
    private string folder = "";

    private string BookPath => Path.Combine(folder, BudgetFile.FileName);
    private string OldPath => Path.Combine(folder, BudgetFile.OldFileName);

    [TestInitialize]
    public void CreateFolder()
    {
        folder = Path.Combine(Path.GetTempPath(), "CheckYourBudget.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
    }

    [TestCleanup]
    public void DeleteFolder() => Directory.Delete(folder, recursive: true);

    private static BudgetBook SampleBook()
    {
        BudgetBook book = new() { Rule = BudgetRule.CustomName, CustomSplit = new(60, 20, 5, 15) };
        book.Add(new MonthBudget
        {
            Month = new(2026, 10),
            Income = [new("Salary", "2000"), new("Side job", "150,50")],
            Needs = [new("Food", "400")],
            Wants = [new("Cinema", "20")],
            Investment = [new("ETF", "300")],
            Savings = [new("Emergency fund", "")],
        });
        book.Add(MonthBudget.Defaults(new(2026, 11)));
        return book;
    }

    [TestMethod]
    public void Starts_with_an_empty_book_when_nothing_is_saved()
    {
        LoadResult result = new BudgetFile(folder).Load();

        Assert.IsNull(result.Problem);
        Assert.AreEqual(0, result.Book.Months.Count);
        Assert.AreEqual(BudgetRule.FiftyFortyTen.Name, result.Book.Rule);
    }

    [TestMethod]
    public void Saves_and_loads_months_items_and_the_rule()
    {
        Assert.IsTrue(new BudgetFile(folder).Save(SampleBook()));

        LoadResult result = new BudgetFile(folder).Load();

        Assert.IsNull(result.Problem);
        BudgetBook book = result.Book;
        Assert.AreEqual(BudgetRule.CustomName, book.Rule);
        Assert.AreEqual(new CustomSplit(60, 20, 5, 15), book.CustomSplit);
        CollectionAssert.AreEqual(new[] { new YearMonth(2026, 10), new YearMonth(2026, 11) }, book.Months.Select(m => m.Month).ToArray());
        CollectionAssert.AreEqual(SampleBook().Months[0].Income, book.Months[0].Income);
        Assert.AreEqual("400", book.Months[0].Needs.Single().Amount);
        Assert.AreEqual(2150.50m, book.Months[0].TotalIncome());
    }

    [TestMethod]
    public void Saving_leaves_only_the_budget_file_behind()
    {
        new BudgetFile(folder).Save(SampleBook());

        CollectionAssert.AreEqual(new[] { BudgetFile.FileName }, Directory.GetFiles(folder).Select(Path.GetFileName).ToArray());
        StringAssert.Contains(File.ReadAllText(BookPath), "\"month\": \"2026-10\"");
    }

    [TestMethod]
    [DataRow("{ \"months\": [ { \"month\": ")]
    [DataRow("")]
    [DataRow("null")]
    [DataRow("{ \"months\": [ { \"month\": \"not a month\" } ] }")]
    public void An_unreadable_file_is_kept_aside_and_never_overwritten(string broken)
    {
        File.WriteAllText(BookPath, broken);
        BudgetFile file = new(folder);

        LoadResult result = file.Load();

        Assert.IsNotNull(result.Problem);
        Assert.AreEqual(0, result.Book.Months.Count);
        string kept = Directory.GetFiles(folder, "budget-book.unreadable-*.json").Single();
        StringAssert.Contains(result.Problem, Path.GetFileName(kept));
        Assert.AreEqual(broken, File.ReadAllText(kept));

        // The app carries on and saves to a new file; the kept file stays as it was.
        Assert.IsTrue(file.Save(SampleBook()));
        Assert.AreEqual(broken, File.ReadAllText(kept));
        Assert.IsNull(new BudgetFile(folder).Load().Problem);
    }

    [TestMethod]
    public void A_file_from_a_newer_version_is_left_alone()
    {
        string newer = "{ \"version\": 99, \"months\": [] }";
        File.WriteAllText(BookPath, newer);
        BudgetFile file = new(folder);

        LoadResult result = file.Load();

        Assert.IsNotNull(result.Problem);
        Assert.IsFalse(file.Save(SampleBook()));
        Assert.AreEqual(newer, File.ReadAllText(BookPath));
    }

    [TestMethod]
    [OSCondition(OperatingSystems.Windows)]
    public void A_file_that_cannot_be_opened_is_not_overwritten()
    {
        new BudgetFile(folder).Save(SampleBook());
        string before = File.ReadAllText(BookPath);
        BudgetFile file = new(folder);

        LoadResult result;
        using (new FileStream(BookPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            result = file.Load();
        }

        Assert.IsNotNull(result.Problem);
        Assert.IsFalse(file.Save(new BudgetBook()));
        Assert.AreEqual(before, File.ReadAllText(BookPath));
    }

    [TestMethod]
    public void Nulls_in_a_hand_edited_file_become_empty_lists_and_names()
    {
        File.WriteAllText(BookPath, """
            { "version": 2, "rule": null, "months": [
              { "month": "2026-11", "income": null, "needs": [ { "name": null, "amount": "5" }, null ] } ] }
            """);

        LoadResult result = new BudgetFile(folder).Load();

        Assert.IsNull(result.Problem);
        MonthBudget month = result.Book.Months.Single();
        Assert.AreEqual(0, month.Income.Count);
        Assert.AreEqual(new BudgetItem("", "5"), month.Needs.Single());
        Assert.AreEqual(0, month.Savings.Count);
        Assert.AreEqual(BudgetRule.FiftyFortyTen.Name, result.Book.Rule);
    }

    [TestMethod]
    public void The_old_windows_file_is_imported_as_this_month_and_left_untouched()
    {
        string old = """
            {
              "salary": "2000",
              "needs": [ { "name": "Food", "amount": "400" }, { "name": "Cat", "amount": "50" } ],
              "wants": [ { "name": "Going out", "amount": "100" } ],
              "investment": [ { "name": "Your future", "amount": "300" } ],
              "rule": "50/15/5/30"
            }
            """;
        File.WriteAllText(OldPath, old);
        BudgetFile file = new(folder);

        LoadResult result = file.Load();

        Assert.IsNull(result.Problem);
        MonthBudget month = result.Book.Months.Single();
        Assert.AreEqual(YearMonth.Current, month.Month);
        Assert.AreEqual(new BudgetItem("Salary", "2000"), month.Income.Single());
        CollectionAssert.AreEqual(new[] { "Food", "Cat" }, month.Needs.Select(i => i.Name).ToArray());
        Assert.AreEqual("Emergency fund", month.Savings.Single().Name, "files older than Savings get the default item");
        Assert.AreEqual("50/15/5/30", result.Book.Rule);

        Assert.IsTrue(file.Save(result.Book));
        Assert.AreEqual(old, File.ReadAllText(OldPath));
        Assert.AreEqual(1, new BudgetFile(folder).Load().Book.Months.Count, "the saved book is used from now on");
    }

    [TestMethod]
    public void A_broken_old_file_is_ignored()
    {
        File.WriteAllText(OldPath, "{ broken");

        LoadResult result = new BudgetFile(folder).Load();

        Assert.IsNull(result.Problem);
        Assert.AreEqual(0, result.Book.Months.Count);
        Assert.AreEqual("{ broken", File.ReadAllText(OldPath));
    }
}
