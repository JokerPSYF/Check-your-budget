using System.Windows.Input;
using CheckYourBudget.App.Charts;
using CheckYourBudget.App.Services;
using CheckYourBudget.Core;
using Share = CheckYourBudget.Core.Share;

namespace CheckYourBudget.App.ViewModels;

/// <summary>One month: its income and expenses, and how they compare to the chosen rule.</summary>
public sealed class MonthViewModel : ObservableObject
{
    private readonly BudgetSession session;
    private MonthBudget budget;
    private bool isDraft;
    private bool loading;

    private string incomeText = "";
    private string spentText = "";
    private string leftOverText = "";
    private string headerText = "";
    private Color leftOverColor = Colors.Gray;
    private IReadOnlyList<TargetRow> targets = [];
    private IReadOnlyList<LegendItem> legend = [];
    private IDrawable chart = new DonutChart([], "", "");
    private string chartDescription = "";

    public MonthViewModel(BudgetSession session)
    {
        this.session = session;

        Income = new("Income", "Add income", category: null, OnChanged);
        Needs = new("Needs", "Add need", Category.Needs, OnChanged);
        Wants = new("Wants", "Add want", Category.Wants, OnChanged);
        Investment = new("Investment", "Add investment", Category.Investment, OnChanged);
        Savings = new("Savings", "Add saving", Category.Savings, OnChanged);
        Sections = [Income, Needs, Wants, Investment, Savings];

        PreviousCommand = new RelayCommand(() => Show(Month.Previous()));
        NextCommand = new RelayCommand(() => Show(Month.Next()));
        ThisMonthCommand = new RelayCommand(() => Show(YearMonth.Current));
        ClearCommand = new AsyncCommand(ClearAmountsAsync);
        DeleteCommand = new AsyncCommand(DeleteMonthAsync);
        DismissProblemCommand = new RelayCommand(session.DismissProblem);
        ChangeRuleCommand = new AsyncCommand(() => Shell.Current.GoToAsync("//rule"));

        session.RuleChanged += Recalculate;
        session.ProblemChanged += () =>
        {
            OnPropertyChanged(nameof(Problem));
            OnPropertyChanged(nameof(HasProblem));
        };

        // A new month starts as a copy of the last one, so opening the app on the 1st shows last month's items.
        budget = null!;
        Show(YearMonth.Current);
    }

    public YearMonth Month { get; private set; }
    public string MonthTitle => Month.Title;
    public bool IsCurrentMonth => Month == YearMonth.Current;

    public SectionViewModel Income { get; }
    public SectionViewModel Needs { get; }
    public SectionViewModel Wants { get; }
    public SectionViewModel Investment { get; }
    public SectionViewModel Savings { get; }
    public IReadOnlyList<SectionViewModel> Sections { get; }

    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand ThisMonthCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand DismissProblemCommand { get; }
    public ICommand ChangeRuleCommand { get; }

    public string? Problem => session.Problem;
    public bool HasProblem => session.Problem is not null;

    /// <summary>True while the month is only shown, not kept: it is kept from its first change.</summary>
    public bool IsDraft { get => isDraft; private set { if (SetProperty(ref isDraft, value)) OnPropertyChanged(nameof(DraftNote)); } }

    public string DraftNote => session.Book.DraftSource(Month) is { } source
        ? $"Not saved yet. This month starts as a copy of {source.Title}. Change anything to keep it."
        : "Not saved yet. Fill in your income and expenses to start your first month.";

    public string IncomeText { get => incomeText; private set => SetProperty(ref incomeText, value); }
    public string SpentText { get => spentText; private set => SetProperty(ref spentText, value); }
    public string LeftOverText { get => leftOverText; private set => SetProperty(ref leftOverText, value); }
    public Color LeftOverColor { get => leftOverColor; private set => SetProperty(ref leftOverColor, value); }

    /// <summary>The line under the month name, always in view: what is left, or how much it is over.</summary>
    public string HeaderText { get => headerText; private set => SetProperty(ref headerText, value); }

    public string RuleTitle => session.Rule.Name == BudgetRule.CustomName
        ? $"Rule: custom {session.Book.CustomSplit}"
        : $"Rule {session.Rule.Name}";

    public IReadOnlyList<TargetRow> Targets { get => targets; private set => SetProperty(ref targets, value); }
    public IReadOnlyList<LegendItem> Legend { get => legend; private set => SetProperty(ref legend, value); }
    public IDrawable Chart { get => chart; private set => SetProperty(ref chart, value); }
    public string ChartDescription { get => chartDescription; private set => SetProperty(ref chartDescription, value); }

    /// <summary>Shows a month: the one kept for it, or a draft copied from the closest month.</summary>
    public void Show(YearMonth month)
    {
        MonthBudget? kept = session.Book.Find(month);
        budget = kept ?? session.Book.Draft(month);
        Month = month;
        IsDraft = kept is null;

        loading = true;
        Income.Load(budget.Income);
        foreach (SectionViewModel section in Sections.Where(s => s.Category is not null))
        {
            section.Load(budget.ItemsOf(section.Category!.Value));
        }

        loading = false;

        OnPropertyChanged(nameof(Month));
        OnPropertyChanged(nameof(MonthTitle));
        OnPropertyChanged(nameof(IsCurrentMonth));
        OnPropertyChanged(nameof(DraftNote));
        Recalculate();
    }

    /// <summary>Colors are picked in code for the charts, so they are picked again when the theme changes.</summary>
    public void OnThemeChanged() => Show(Month);

    // Any change (typing, renaming, adding, removing) updates the results and saves right away.
    private void OnChanged()
    {
        if (loading)
        {
            return;
        }

        budget.Income = Income.ToItems();
        budget.Needs = Needs.ToItems();
        budget.Wants = Wants.ToItems();
        budget.Investment = Investment.ToItems();
        budget.Savings = Savings.ToItems();

        if (IsDraft)
        {
            session.Book.Add(budget);
            IsDraft = false;
        }

        session.Save();
        Recalculate();
    }

    private void Recalculate()
    {
        BudgetSummary s = budget.Summarize(session.Rule);

        IncomeText = Money.Format(s.Income);
        SpentText = Money.Format(s.TotalSpent);
        LeftOverText = Money.Format(s.Residue);
        LeftOverColor = s.IsOverspent ? AppColors.Danger : AppColors.Text;
        HeaderText = s.IsOverspent
            ? $"{Money.Format(-s.Residue)} over your income"
            : $"{Money.Format(s.Residue)} left over";

        foreach (SectionViewModel section in Sections)
        {
            section.TotalText = Money.Format(section.Total);
            foreach (ItemViewModel item in section.Items)
            {
                item.PercentText = Money.FormatPercent(BudgetCalculator.ShareOf(s.Income, item.Amount).Percent);
            }
        }

        OnPropertyChanged(nameof(RuleTitle));
        Targets = [.. s.Targets.Select(t => new TargetRow(t, hasIncome: s.Income > 0))];

        List<(string Name, Share Share, Color Color)> parts =
        [
            .. Enum.GetValues<Category>().Select(c => (c.ToString(), s.ShareOf(c), AppColors.Of(c))),
        ];
        if (s.Residue > 0)
        {
            parts.Add(("Left over", BudgetCalculator.ShareOf(s.Income, s.Residue), AppColors.LeftOver));
        }

        Legend = [.. parts.Select(p => new LegendItem(p.Color, p.Name, Money.Format(p.Share.Amount), Money.FormatPercent(p.Share.Percent)))];

        decimal spentPercent = BudgetCalculator.ShareOf(s.Income, s.TotalSpent).Percent;
        Chart = new DonutChart(
            [.. parts.Select(p => new Slice(p.Share.Amount, p.Color))],
            s.Income > 0 ? $"{spentPercent:0}%" : "–",
            s.Income > 0 ? "spent" : "no income");
        ChartDescription = s.Income > 0
            ? $"Chart: {spentPercent:0}% of the income is spent. " + string.Join(", ", Legend.Select(l => l.Description))
            : "Chart: no income entered yet";
    }

    private async Task ClearAmountsAsync()
    {
        bool clear = await Shell.Current.CurrentPage.DisplayAlertAsync(
            "Clear amounts?", $"This empties every amount of {Month.Title}. The item names stay.", "Clear", "Cancel");
        if (!clear)
        {
            return;
        }

        loading = true;
        foreach (ItemViewModel item in Sections.SelectMany(s => s.Items))
        {
            item.Text = "";
        }

        loading = false;
        OnChanged();
    }

    private async Task DeleteMonthAsync()
    {
        bool delete = await Shell.Current.CurrentPage.DisplayAlertAsync(
            "Delete this month?", $"{Month.Title} and everything in it is removed from History.", "Delete", "Cancel");
        if (!delete)
        {
            return;
        }

        session.Book.Months.Remove(budget);
        session.Save();
        Show(Month);
    }
}
