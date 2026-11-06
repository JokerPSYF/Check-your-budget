using CheckYourBudget.App.ViewModels;

namespace CheckYourBudget.App.Views;

public partial class MonthPage : ContentPage
{
    private const double WideWidth = 900;
    private bool? wide;

    public MonthPage(MonthViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    // Phones show the results above the inputs; wide windows put the results in a column on the right.
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        bool isWide = width >= WideWidth;
        if (wide == isWide)
        {
            return;
        }

        wide = isWide;
        Columns.ColumnDefinitions = isWide
            ? [new ColumnDefinition(GridLength.Star), new ColumnDefinition(new GridLength(420))]
            : [new ColumnDefinition(GridLength.Star)];

        Grid.SetColumnSpan(Notices, isWide ? 2 : 1);
        Grid.SetRow(Inputs, isWide ? 1 : 2);
        Grid.SetColumn(Inputs, 0);
        Grid.SetRow(Results, 1);
        Grid.SetColumn(Results, isWide ? 1 : 0);
    }

    // A row added with an "Add" button starts as "New item": put the cursor there, selected,
    // so typing the real name replaces it.
    private void NameEntry_Loaded(object? sender, EventArgs e)
    {
        if (sender is Entry { BindingContext: ItemViewModel { IsNew: true } item } entry)
        {
            item.IsNew = false;
            entry.Dispatcher.Dispatch(() =>
            {
                entry.Focus();
                entry.CursorPosition = 0;
                entry.SelectionLength = entry.Text?.Length ?? 0;
            });
        }
    }
}
