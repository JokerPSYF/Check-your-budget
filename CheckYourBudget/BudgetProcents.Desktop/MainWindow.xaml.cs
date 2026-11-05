using System.Windows;
using System.Windows.Controls;
using BudgetProcents.Desktop.ViewModels;

namespace BudgetProcents.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    // A row added with an "Add" button starts as "New item": put the cursor there, selected,
    // so typing the real name replaces it.
    private void NameBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: ExpenseItem { IsNew: true } item } box)
        {
            item.IsNew = false;
            box.Focus();
            box.SelectAll();
        }
    }
}
