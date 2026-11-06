using System.Windows.Input;
using CheckYourBudget.App.Services;
using CheckYourBudget.Core;

namespace CheckYourBudget.App.ViewModels;

/// <summary>One rule to pick from.</summary>
public sealed record RuleOption(string Name, string Title, string Description);

/// <summary>Picking the rule, and the percentages of the custom rule.</summary>
public sealed class RuleViewModel : ObservableObject
{
    private readonly BudgetSession session;
    private string needs;
    private string investment;
    private string savings;
    private string wants;

    public RuleViewModel(BudgetSession session)
    {
        this.session = session;
        CustomSplit split = session.Book.CustomSplit;
        needs = $"{split.Needs:0.##}";
        investment = $"{split.Investment:0.##}";
        savings = $"{split.Savings:0.##}";
        wants = $"{split.Wants:0.##}";
        SelectCommand = new Command<string>(name => SelectedRule = name);
    }

    public IReadOnlyList<RuleOption> Options { get; } =
    [
        new("50/40/10", "50/40/10",
            "Up to 50% on needs, at least 40% into investment and savings together, up to 10% on wants. " +
            "Puts a lot aside, for building up money fast."),
        new("50/15/5/30", "50/15/5/30",
            "Up to 50% on needs, at least 15% investment, at least 5% short-term savings, up to 30% on wants. " +
            "More room for the fun things."),
        new(BudgetRule.CustomName, "Custom", "Your own percentages for needs, investment, savings and wants."),
    ];

    public ICommand SelectCommand { get; }

    public string SelectedRule
    {
        get => session.Book.Rule;
        set
        {
            if (value is null || value == session.Book.Rule)
            {
                return;
            }

            session.SetRule(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCustom));
        }
    }

    public bool IsCustom => session.Book.Rule == BudgetRule.CustomName;

    public string NeedsText { get => needs; set { if (SetProperty(ref needs, value)) OnSplitChanged(); } }
    public string InvestmentText { get => investment; set { if (SetProperty(ref investment, value)) OnSplitChanged(); } }
    public string SavingsText { get => savings; set { if (SetProperty(ref savings, value)) OnSplitChanged(); } }
    public string WantsText { get => wants; set { if (SetProperty(ref wants, value)) OnSplitChanged(); } }

    public string TotalText
    {
        get
        {
            if (Split() is not { } split)
            {
                return "Enter a number from 0 to 100 in each box.";
            }

            return split.IsComplete ? "Adds up to 100%." : $"Adds up to {split.Total:0.##}%. Make it 100% so the whole income is planned.";
        }
    }

    public Color TotalColor => Split() is { IsComplete: true } ? AppColors.Ok : AppColors.Warning;

    private void OnSplitChanged()
    {
        if (Split() is { } split)
        {
            session.SetCustomSplit(split);
        }

        OnPropertyChanged(nameof(TotalText));
        OnPropertyChanged(nameof(TotalColor));
    }

    // The four percentages, or null while one of them isn't a number from 0 to 100.
    private CustomSplit? Split()
    {
        decimal?[] parts = [Percent(needs), Percent(investment), Percent(savings), Percent(wants)];
        return parts.All(p => p is not null)
            ? new CustomSplit(parts[0]!.Value, parts[1]!.Value, parts[2]!.Value, parts[3]!.Value)
            : null;

        static decimal? Percent(string text)
            => !string.IsNullOrWhiteSpace(text) && Money.TryParse(text, out decimal value) && value <= 100 ? value : null;
    }
}
