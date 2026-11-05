using System.Windows.Input;
using BudgetProcents.Core;

namespace BudgetProcents.Desktop.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    // The items a first run starts with. After that, the user's own list is loaded from disk.
    private static readonly SavedBudget Defaults = new(
        Salary: "",
        Needs: Items("Internet and TV", "Home stuff", "Dad", "Cat", "Electricity and water",
                     "Gas", "Car", "Food", "Clothes", "Haircut", "Health"),
        Wants: Items("Going out", "Cinema", "Hobby", "Games"),
        Investment: Items("Your future"));

    private readonly bool loaded;
    private string spentText = "";
    private string leftOverText = "";
    private bool isOverspent;
    private IReadOnlyList<RuleRow> rules = [];
    private IReadOnlyList<BreakdownSection> breakdown = [];

    public MainViewModel()
    {
        Needs = new("Needs", "Add need", Category.Needs, OnChanged);
        Wants = new("Wants", "Add want", Category.Wants, OnChanged);
        Investment = new("Investment", "Add investment", Category.Investment, OnChanged);
        Sections = [Needs, Wants, Investment];

        SavedBudget saved = BudgetStore.Load() ?? Defaults;
        Salary.Text = saved.Salary ?? "";
        Fill(Needs, saved.Needs);
        Fill(Wants, saved.Wants);
        Fill(Investment, saved.Investment);

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
    public IReadOnlyList<CategorySection> Sections { get; }

    public ICommand ClearCommand { get; }

    public string SpentText { get => spentText; private set => SetProperty(ref spentText, value); }
    public string LeftOverText { get => leftOverText; private set => SetProperty(ref leftOverText, value); }
    public bool IsOverspent { get => isOverspent; private set => SetProperty(ref isOverspent, value); }
    public IReadOnlyList<RuleRow> Rules { get => rules; private set => SetProperty(ref rules, value); }
    public IReadOnlyList<BreakdownSection> Breakdown { get => breakdown; private set => SetProperty(ref breakdown, value); }

    // Any change (typing, renaming, adding, removing) updates the results and saves right away.
    private void OnChanged()
    {
        if (!loaded)
        {
            return;
        }

        Recalculate();
        BudgetStore.Save(new SavedBudget(Salary.Text, Saved(Needs), Saved(Wants), Saved(Investment)));
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
            Sections.SelectMany(s => s.Items.Select(i => new Expense(i.DisplayName, s.Category, i.Amount))));

        SpentText = Format.Money(r.TotalSpent);
        LeftOverText = Format.Money(r.Residue);
        IsOverspent = r.IsOverspent;

        Rules =
        [
            new("Needs", r.Needs, BudgetCalculator.NeedsTargetPercent,
                $"Up to 50% ({Format.Money(r.NeedsTarget.Amount)}): food, home, transport, health",
                r.NeedsOverTarget ? RuleStatus.Over : RuleStatus.Ok),
            new("Investment", r.Investment, BudgetCalculator.InvestmentTargetPercent,
                $"At least 40% ({Format.Money(r.InvestmentTarget.Amount)}): emergency fund, education, stocks/ETFs, property",
                r.InvestmentUnderTarget ? RuleStatus.Under : RuleStatus.Ok),
            new("Wants", r.Wants, BudgetCalculator.WantsTargetPercent,
                $"Up to 10% ({Format.Money(r.WantsTarget.Amount)}): going out, entertainment, hobbies",
                r.WantsOverTarget ? RuleStatus.Over : RuleStatus.Ok),
        ];

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
