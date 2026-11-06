using CheckYourBudget.App.ViewModels;

namespace CheckYourBudget.App.Views;

public partial class RulePage : ContentPage
{
    public RulePage(RuleViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
