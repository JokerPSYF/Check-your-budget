using System.Windows.Input;
using CheckYourBudget.App.Charts;
using CheckYourBudget.App.Services;
using CheckYourBudget.Core;

namespace CheckYourBudget.App.ViewModels;

/// <summary>Every month that was filled in, newest first, a chart of the last year, and the Excel export.</summary>
public sealed class HistoryViewModel : ObservableObject
{
    private const string ExcelType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly BudgetSession session;
    private readonly MonthViewModel month;
    private IReadOnlyList<HistoryRow> months = [];
    private IDrawable chart = new MonthBarsChart([]);
    private string chartTitle = "";
    private string? exportMessage;
    private string? exportedFile;

    public HistoryViewModel(BudgetSession session, MonthViewModel month)
    {
        this.session = session;
        this.month = month;
        ExportCommand = new AsyncCommand(ExportAsync);
        OpenExportCommand = new AsyncCommand(OpenExportAsync);
        Legend = [.. Enum.GetValues<Category>().Select(c => new LegendItem(AppColors.Of(c), c.ToString(), "", ""))];
    }

    public IReadOnlyList<HistoryRow> Months { get => months; private set { if (SetProperty(ref months, value)) { OnPropertyChanged(nameof(HasMonths)); OnPropertyChanged(nameof(IsEmpty)); } } }
    public bool HasMonths => months.Count > 0;
    public bool IsEmpty => months.Count == 0;

    public IDrawable Chart { get => chart; private set => SetProperty(ref chart, value); }
    public string ChartTitle { get => chartTitle; private set => SetProperty(ref chartTitle, value); }
    public IReadOnlyList<LegendItem> Legend { get; private set; }

    public string RuleTitle => session.Rule.Name == BudgetRule.CustomName
        ? $"Compared with your custom rule {session.Book.CustomSplit}"
        : $"Compared with the {session.Rule.Name} rule";

    public ICommand ExportCommand { get; }
    public ICommand OpenExportCommand { get; }

    public string? ExportMessage { get => exportMessage; private set { if (SetProperty(ref exportMessage, value)) OnPropertyChanged(nameof(HasExportMessage)); } }
    public bool HasExportMessage => exportMessage is not null;

    /// <summary>On Windows the export is saved to Documents and can then be opened from here.</summary>
    public bool CanOpenExport => exportedFile is not null && DeviceInfo.Platform == DevicePlatform.WinUI;

    /// <summary>Rebuilds the list from the book. Called whenever the page is shown.</summary>
    public void Refresh()
    {
        BudgetRule rule = session.Rule;
        List<(MonthBudget Month, BudgetSummary Summary)> all =
            [.. session.Book.Months.OrderByDescending(m => m.Month).Select(m => (m, m.Summarize(rule)))];

        Months = [.. all.Select(m => new HistoryRow(m.Month.Month, m.Summary, Open))];

        List<(MonthBudget Month, BudgetSummary Summary)> lastYear = [.. all.Take(12).Reverse()];
        Chart = new MonthBarsChart([.. lastYear.Select(m => new MonthBarsChart.Bar(m.Month.Month, m.Summary.Income, Spending(m.Summary)))]);
        ChartTitle = lastYear.Count switch
        {
            0 => "",
            1 => lastYear[0].Month.Month.Title,
            _ => $"{lastYear[0].Month.Month.ShortTitle} – {lastYear[^1].Month.Month.ShortTitle}",
        };

        Legend = [.. Enum.GetValues<Category>().Select(c => new LegendItem(AppColors.Of(c), c.ToString(), "", ""))];
        OnPropertyChanged(nameof(Legend));
        OnPropertyChanged(nameof(RuleTitle));
    }

    internal static List<Slice> Spending(BudgetSummary s)
        => [.. Enum.GetValues<Category>().Select(c => new Slice(s.ShareOf(c).Amount, AppColors.Of(c)))];

    private async void Open(YearMonth target)
    {
        month.Show(target);
        await Shell.Current.GoToAsync("//month");
    }

    private async Task ExportAsync()
    {
        ExportMessage = "Exporting…";
        exportedFile = null;
        OnPropertyChanged(nameof(CanOpenExport));
        try
        {
            string path = await Task.Run(() => WriteWorkbook(session.Book));
            exportedFile = path;
            if (DeviceInfo.Platform == DevicePlatform.WinUI)
            {
                ExportMessage = $"Saved as {path}";
            }
            else
            {
                ExportMessage = null;
                await Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Budget",
                    File = new ShareFile(path, ExcelType),
                });
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            ExportMessage = $"The Excel file couldn't be saved: {e.Message}";
        }

        OnPropertyChanged(nameof(CanOpenExport));
    }

    private async Task OpenExportAsync()
    {
        if (exportedFile is not null)
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest("Budget", new ReadOnlyFile(exportedFile, ExcelType)));
        }
    }

    // Saves "Budget 2026-11-06.xlsx", or "... (2).xlsx" when a file of that name is open in Excel.
    private static string WriteWorkbook(BudgetBook book)
    {
        Directory.CreateDirectory(AppFolders.Export);
        string name = Path.GetFileNameWithoutExtension(ExcelExport.FileName(DateTime.Now));
        IOException? last = null;
        for (int attempt = 1; attempt <= 20; attempt++)
        {
            string path = Path.Combine(AppFolders.Export, attempt == 1 ? $"{name}.xlsx" : $"{name} ({attempt}).xlsx");
            try
            {
                using FileStream stream = new(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
                ExcelExport.Write(book, stream);
                return path;
            }
            catch (IOException e)
            {
                last = e;
            }
        }

        throw last!;
    }
}

/// <summary>One month in the History list.</summary>
public sealed class HistoryRow
{
    public HistoryRow(YearMonth month, BudgetSummary s, Action<YearMonth> open)
    {
        Title = month.Title;
        IncomeText = $"Income {Money.Format(s.Income)}";
        SpentText = $"Spent {Money.Format(s.TotalSpent)}";
        LeftOverText = s.IsOverspent ? $"{Money.Format(-s.Residue)} over" : $"{Money.Format(s.Residue)} left";
        LeftOverColor = s.IsOverspent ? AppColors.Danger : AppColors.Ok;
        SplitText = string.Join(" · ", Enum.GetValues<Category>().Select(c => $"{c} {s.ShareOf(c).Percent:0}%"));
        TargetsText = s.Targets.All(t => t.IsMet)
            ? "Every target met"
            : $"{s.Targets.Count(t => t.IsMet)} of {s.Targets.Count} targets met";

        List<Slice> slices = HistoryViewModel.Spending(s);
        if (s.Residue > 0)
        {
            slices.Add(new Slice(s.Residue, AppColors.LeftOver));
        }

        Bar = new SplitBar(slices);
        OpenCommand = new RelayCommand(() => open(month));
        Description = $"{Title}: {IncomeText}, {SpentText}, {LeftOverText}. {TargetsText}. Opens the month.";
    }

    public string Title { get; }
    public string IncomeText { get; }
    public string SpentText { get; }
    public string LeftOverText { get; }
    public Color LeftOverColor { get; }
    public string SplitText { get; }
    public string TargetsText { get; }
    public IDrawable Bar { get; }
    public ICommand OpenCommand { get; }
    public string Description { get; }
}
