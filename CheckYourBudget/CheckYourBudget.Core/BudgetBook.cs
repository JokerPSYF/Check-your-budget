using System.Text.Json.Serialization;

namespace CheckYourBudget.Core;

/// <summary>One line of a month: an income or an expense, with its amount as the user typed it.</summary>
public sealed record BudgetItem(string Name, string Amount)
{
    [JsonIgnore]
    public decimal Value => Money.Parse(Amount);
}

/// <summary>One month's income and expenses.</summary>
public sealed class MonthBudget
{
    public YearMonth Month { get; set; }
    public List<BudgetItem> Income { get; set; } = [];
    public List<BudgetItem> Needs { get; set; } = [];
    public List<BudgetItem> Wants { get; set; } = [];
    public List<BudgetItem> Investment { get; set; } = [];
    public List<BudgetItem> Savings { get; set; } = [];

    /// <summary>The items a first month starts with.</summary>
    public static MonthBudget Defaults(YearMonth month) => new()
    {
        Month = month,
        Income = Items("Salary"),
        Needs = Items("Rent or mortgage", "Internet and TV", "Electricity and water", "Home stuff", "Food",
                      "Transport", "Clothes", "Haircut", "Health"),
        Wants = Items("Going out", "Cinema", "Hobby", "Games"),
        Investment = Items("Your future"),
        Savings = Items("Emergency fund"),
    };

    public List<BudgetItem> ItemsOf(Category category) => category switch
    {
        Category.Needs => Needs,
        Category.Wants => Wants,
        Category.Investment => Investment,
        Category.Savings => Savings,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, null),
    };

    public decimal TotalIncome() => Income.Sum(i => i.Value);

    public IEnumerable<Expense> Expenses() =>
        Enum.GetValues<Category>().SelectMany(c => ItemsOf(c).Select(i => new Expense(i.Name, c, i.Value)));

    public BudgetSummary Summarize(BudgetRule rule) => BudgetCalculator.Summarize(TotalIncome(), Expenses(), rule);

    /// <summary>The same items and amounts, for another month.</summary>
    public MonthBudget CopyAs(YearMonth month) => new()
    {
        Month = month,
        Income = [.. Income],
        Needs = [.. Needs],
        Wants = [.. Wants],
        Investment = [.. Investment],
        Savings = [.. Savings],
    };

    // A file edited by hand can have nulls where lists and names should be.
    internal void Normalize()
    {
        Income = Clean(Income);
        Needs = Clean(Needs);
        Wants = Clean(Wants);
        Investment = Clean(Investment);
        Savings = Clean(Savings);

        static List<BudgetItem> Clean(List<BudgetItem>? items)
            => [.. (items ?? []).OfType<BudgetItem>().Select(i => new BudgetItem(i.Name ?? "", i.Amount ?? ""))];
    }

    private static List<BudgetItem> Items(params string[] names) => [.. names.Select(n => new BudgetItem(n, ""))];
}

/// <summary>Everything the app keeps: every month's budget and the chosen rule.</summary>
public sealed class BudgetBook
{
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;

    /// <summary>The chosen rule's name: one of the built-in rules, or <see cref="BudgetRule.CustomName"/>.</summary>
    public string Rule { get; set; } = BudgetRule.FiftyFortyTen.Name;

    public CustomSplit CustomSplit { get; set; } = CustomSplit.Default;

    /// <summary>The months that have been filled in, oldest first.</summary>
    public List<MonthBudget> Months { get; set; } = [];

    public BudgetRule CurrentRule()
        => Rule == BudgetRule.CustomName
            ? BudgetRule.Custom(CustomSplit)
            : BudgetRule.BuiltIn.FirstOrDefault(r => r.Name == Rule) ?? BudgetRule.FiftyFortyTen;

    public MonthBudget? Find(YearMonth month) => Months.FirstOrDefault(m => m.Month == month);

    /// <summary>
    /// A month that isn't in the book yet: a copy of the closest earlier month, or the closest later one,
    /// or the default items when the book is empty. It is only kept once it is passed to <see cref="Add"/>.
    /// </summary>
    public MonthBudget Draft(YearMonth month)
        => SourceFor(month)?.CopyAs(month) ?? MonthBudget.Defaults(month);

    /// <summary>The month a draft would be copied from, or null when it would start from the default items.</summary>
    public YearMonth? DraftSource(YearMonth month) => SourceFor(month)?.Month;

    private MonthBudget? SourceFor(YearMonth month)
        => Months.Where(m => m.Month < month).MaxBy(m => m.Month)
           ?? Months.Where(m => m.Month > month).MinBy(m => m.Month);

    /// <summary>Keeps a month, replacing the one already kept for the same month.</summary>
    public void Add(MonthBudget month)
    {
        Months.RemoveAll(m => m.Month == month.Month);
        Months.Add(month);
        Months.Sort((a, b) => a.Month.CompareTo(b.Month));
    }

    internal void Normalize()
    {
        Rule ??= BudgetRule.FiftyFortyTen.Name;
        CustomSplit ??= CustomSplit.Default;
        Months = [.. (Months ?? []).OfType<MonthBudget>().GroupBy(m => m.Month).Select(g => g.Last()).OrderBy(m => m.Month)];
        foreach (MonthBudget month in Months)
        {
            month.Normalize();
        }
    }
}
