using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using CheckYourBudget.App.Services;
using CheckYourBudget.Core;

namespace CheckYourBudget.App.ViewModels;

/// <summary>One input section of a month (Income, Needs, Wants, Investment or Savings) with its own items.</summary>
public sealed class SectionViewModel : ObservableObject
{
    private readonly Action changed;
    private string totalText = "";
    private bool hasError;

    public SectionViewModel(string title, string addLabel, Category? category, Action changed)
    {
        Title = title;
        AddLabel = addLabel;
        Category = category;
        this.changed = changed;

        Items.CollectionChanged += (_, _) => changed();
        AddCommand = new RelayCommand(() => Add(new BudgetItem("New item", ""), isNew: true));
    }

    public string Title { get; }
    public string AddLabel { get; }

    /// <summary>The expense category, or null for the Income section.</summary>
    public Category? Category { get; }

    public bool IsIncome => Category is null;

    public Color Color => Category is { } c ? AppColors.Of(c) : AppColors.Get("Primary");

    public ObservableCollection<ItemViewModel> Items { get; } = [];
    public ICommand AddCommand { get; }

    public string TotalText { get => totalText; set => SetProperty(ref totalText, value); }

    /// <summary>True when one of the amounts isn't a valid number.</summary>
    public bool HasError { get => hasError; private set => SetProperty(ref hasError, value); }

    public decimal Total => Items.Sum(i => i.Amount);

    /// <summary>Replaces the items, for example when another month is shown.</summary>
    public void Load(IEnumerable<BudgetItem> items)
    {
        foreach (ItemViewModel item in Items)
        {
            item.PropertyChanged -= OnItemChanged;
        }

        Items.Clear();
        foreach (BudgetItem item in items)
        {
            Add(item);
        }

        HasError = Items.Any(i => i.HasError);
        OnPropertyChanged(nameof(Color));
    }

    public List<BudgetItem> ToItems() => [.. Items.Select(i => new BudgetItem(i.Name, i.Text))];

    private void Add(BudgetItem source, bool isNew = false)
    {
        ItemViewModel item = new(source.Name, source.Amount, showsPercent: !IsIncome, Remove) { IsNew = isNew };
        item.PropertyChanged += OnItemChanged;
        Items.Add(item);
    }

    private void Remove(ItemViewModel item)
    {
        item.PropertyChanged -= OnItemChanged;
        Items.Remove(item);
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Only what the user typed matters; the other properties change along with it.
        if (e.PropertyName is nameof(ItemViewModel.Name) or nameof(ItemViewModel.Text))
        {
            HasError = Items.Any(i => i.HasError);
            changed();
        }
    }
}

/// <summary>One row the user can rename, fill in and remove, for example "Food" in Needs.</summary>
public sealed class ItemViewModel : ObservableObject
{
    private string name;
    private string text;
    private string percentText = "";

    public ItemViewModel(string name, string text, bool showsPercent, Action<ItemViewModel> remove)
    {
        this.name = name;
        this.text = text;
        ShowPercent = showsPercent;
        RemoveCommand = new RelayCommand(() => remove(this));
    }

    public string Name
    {
        get => name;
        set
        {
            if (SetProperty(ref name, value))
            {
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(AmountDescription));
                OnPropertyChanged(nameof(RemoveDescription));
            }
        }
    }

    /// <summary>The amount as typed.</summary>
    public string Text
    {
        get => text;
        set
        {
            if (SetProperty(ref text, value))
            {
                OnPropertyChanged(nameof(HasError));
                OnPropertyChanged(nameof(AmountColor));
            }
        }
    }

    public decimal Amount => Money.Parse(text);

    /// <summary>True when the box has text that isn't a positive number.</summary>
    public bool HasError => !Money.TryParse(text, out _);

    public Color AmountColor => HasError ? AppColors.Danger : AppColors.Text;

    /// <summary>The name shown to screen readers and in results, so a blank name isn't read as nothing.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(name) ? "Unnamed" : name.Trim();

    public string AmountDescription => $"{DisplayName} amount";
    public string RemoveDescription => $"Remove {DisplayName}";

    /// <summary>The share of the income, for example "20,00%". Not shown for income items.</summary>
    public string PercentText { get => percentText; set => SetProperty(ref percentText, value); }

    public bool ShowPercent { get; }

    /// <summary>True for a row the user just added; the page then puts the cursor in its name.</summary>
    public bool IsNew { get; set; }

    public ICommand RemoveCommand { get; }
}
