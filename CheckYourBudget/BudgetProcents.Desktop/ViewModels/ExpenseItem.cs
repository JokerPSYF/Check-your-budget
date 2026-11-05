using System.Windows.Input;

namespace BudgetProcents.Desktop.ViewModels;

/// <summary>One row the user can rename, fill in and remove, for example "Food" in Needs.</summary>
public sealed class ExpenseItem : AmountField
{
    private string name;

    public ExpenseItem(string name, string text, Action<ExpenseItem> remove) : base(text)
    {
        this.name = name;
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
            }
        }
    }

    /// <summary>The name shown in the results, so a blank name doesn't leave an empty line.</summary>
    public string DisplayName => string.IsNullOrWhiteSpace(name) ? "Unnamed" : name.Trim();

    /// <summary>True for a row the user just added; the window then puts the cursor in its name.</summary>
    public bool IsNew { get; set; }

    public ICommand RemoveCommand { get; }
}
