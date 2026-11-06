using CheckYourBudget.App.ViewModels;

namespace CheckYourBudget.App.Views;

public partial class HistoryPage : ContentPage
{
    private readonly HistoryViewModel viewModel;

    public HistoryPage(HistoryViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = this.viewModel = viewModel;
    }

    // Months only change on the other screens, so the list is rebuilt each time this one is shown.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        viewModel.Refresh();
    }
}
