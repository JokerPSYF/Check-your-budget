using System.Windows.Input;
using BudgetProcents.Core;

namespace BudgetProcents.Desktop.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    // The items a first run starts with. After that, the user's own list is loaded from disk.
    private static readonly SavedBudget Defaults = new(
        Salary: "",
        Needs: Items("Internet and TV", "Home stuff", "Cat", "Electricity and water",
                     "Gas", "Car", "Food", "Clothes", "Haircut", "Health"),
        Wants: Items("Going out", "Cinema", "Hobby", "Games"),
        Investment: Items("Your future"),
        Savings: Items("Emergency fund"),
        Rule: BudgetRule.FiftyFortyTen.Name);

    private readonly bool loaded;
    private BudgetRule rule;
    private string spentText = "";
    private string leftOverText = "";
    private bool isOverspent;
    private IReadOnlyList<RuleRow> targets = [];
    private IReadOnlyList<BreakdownSection> breakdown = [];

    public MainViewModel()
    {
        Needs = new("Needs", "Add need", Category.Needs, OnChanged);
        Wants = new("Wants", "Add want", Category.Wants, OnChanged);
        Investment = new("Investment", "Add investment", Category.Investment, OnChanged);
        Savings = new("Savings", "Add saving", Category.Savings, OnChanged);
        Sections = [Needs, Wants, Investment, Savings];

        SavedBudget saved = BudgetStore.Load() ?? Defaults;
        Salary.Text = saved.Salary ?? "";
        // A section missing from the file (one saved before Savings existed) starts with the default items.
        Fill(Needs, saved.Needs ?? Defaults.Needs);
        Fill(Wants, saved.Wants ?? Defaults.Wants);
        Fill(Investment, saved.Investment ?? Defaults.Investment);
        Fill(Savings, saved.Savings ?? Defaults.Savings);
        rule = BudgetRule.All.FirstOrDefault(r => r.Name == saved.Rule) ?? BudgetRule.FiftyFortyTen;

        Salary.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AmountField.Text))
            {
                OnChanged();
            }
        };

        ClearCommand = new RelayCommand(ClearAmounts);

        loaded = true;
        Recalculate();
    }

    public AmountField Salary { get; } = new();

    public CategorySection Needs { get; }
    public CategorySection Wants { get; }
    public CategorySection Investment { get; }
    public CategorySection Savings { get; }
    public IReadOnlyList<CategorySection> Sections { get; }

    public IReadOnlyList<BudgetRule> RuleOptions => BudgetRule.All;

    /// <summary>The rule the spending is compared to. Changing it updates the results and is saved.</summary>
    public BudgetRule Rule
    {
        get => rule;
        set
        {
            if (value is not null && SetProperty(ref rule, value))
            {
                OnChanged();
            }
        }
    }

    public ICommand ClearCommand { get; }

    public string SpentText { get => spentText; private set => SetProperty(ref spentText, value); }
    public string LeftOverText { get => leftOverText; private set => SetProperty(ref leftOverText, value); }
    public bool IsOverspent { get => isOverspent; private set => SetProperty(ref isOverspent, value); }
    public IReadOnlyList<RuleRow> Targets { get => targets; private set => SetProperty(ref targets, value); }
    public IReadOnlyList<BreakdownSection> Breakdown { get => breakdown; private set => SetProperty(ref breakdown, value); }

    // Any change (typing, renaming, adding, removing, picking a rule) updates the results and saves right away.
    private void OnChanged()
    {
        if (!loaded)
        {
            return;
        }

        Recalculate();
        BudgetStore.Save(new SavedBudget(
            Salary.Text, Saved(Needs), Saved(Wants), Saved(Investment), Saved(Savings), Rule.Name));
    }

    private void ClearAmounts()
    {
        Salary.Text = "";
        foreach (ExpenseItem item in Sections.SelectMany(s => s.Items))
        {
            item.Text = "";
        }
    }

    private void Recalculate()
    {
        decimal salary = Salary.Amount;
        BudgetSummary r = BudgetCalculator.Summarize(salary,
            Sections.SelectMany(s => s.Items.Select(i => new Expense(i.DisplayName, s.Category, i.Amount))),
            Rule);

        SpentText = Format.Money(r.TotalSpent);
        LeftOverText = Format.Money(r.Residue);
        IsOverspent = r.IsOverspent;

        Targets = [.. r.Targets.Select(t => new RuleRow(t))];

        Breakdown =
        [
            .. Sections.Select(s => new BreakdownSection(s.Title,
                [.. s.Items.Select(i => new ItemRow(i.DisplayName, BudgetCalculator.ShareOf(salary, i.Amount)))])),
        ];
    }

    private static void Fill(CategorySection section, List<SavedItem>? items)
    {
        foreach (SavedItem item in items ?? [])
        {
            section.Add(item.Name ?? "", item.Amount ?? "");
        }
    }

    private static List<SavedItem> Saved(CategorySection section)
        => [.. section.Items.Select(i => new SavedItem(i.Name, i.Text))];

    private static List<SavedItem> Items(params string[] names)
        => [.. names.Select(n => new SavedItem(n, ""))];
}
