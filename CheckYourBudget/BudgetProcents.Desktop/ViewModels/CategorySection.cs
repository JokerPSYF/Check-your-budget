using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using BudgetProcents.Core;

namespace BudgetProcents.Desktop.ViewModels;

/// <summary>One input section (Needs, Wants, Investment or Savings) with its own list of items.</summary>
public sealed class CategorySection
{
    private readonly Action changed;

    public CategorySection(string title, string addLabel, Category category, Action changed)
    {
        Title = title;
        AddLabel = addLabel;
        Category = category;
        this.changed = changed;

        Items.CollectionChanged += (_, _) => changed();
        AddCommand = new RelayCommand(() => Add("New item", "", isNew: true));
    }

    public string Title { get; }
    public string AddLabel { get; }
    public Category Category { get; }
    public ObservableCollection<ExpenseItem> Items { get; } = [];
    public ICommand AddCommand { get; }

    public void Add(string name, string amount, bool isNew = false)
    {
        ExpenseItem item = new(name, amount, Remove) { IsNew = isNew };
        item.PropertyChanged += OnItemChanged;
        Items.Add(item);
    }

    private void Remove(ExpenseItem item)
    {
        item.PropertyChanged -= OnItemChanged;
        Items.Remove(item);
    }

    private void OnItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Only what the user typed matters; the derived properties change along with it.
        if (e.PropertyName is nameof(ExpenseItem.Name) or nameof(ExpenseItem.Text))
        {
            changed();
        }
    }
}
