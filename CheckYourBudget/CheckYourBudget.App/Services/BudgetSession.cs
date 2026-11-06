using CheckYourBudget.Core;

namespace CheckYourBudget.App.Services;

/// <summary>The budget book for the whole app: loaded once at start, saved after every change.</summary>
public sealed class BudgetSession
{
    private readonly BudgetFile file;
    private string? loadProblem;
    private bool saveFailed;

    public BudgetSession()
    {
        file = new BudgetFile(AppFolders.Data);
        LoadResult result = file.Load();
        Book = result.Book;
        loadProblem = result.Problem;
    }

    public BudgetBook Book { get; }

    public BudgetRule Rule => Book.CurrentRule();

    /// <summary>Something the user should know about their saved data, or null.</summary>
    public string? Problem => loadProblem
        ?? (saveFailed ? "Your latest changes couldn't be saved. Check that the device has free space." : null);

    /// <summary>The chosen rule or its percentages changed.</summary>
    public event Action? RuleChanged;

    public event Action? ProblemChanged;

    public void Save()
    {
        string? before = Problem;
        saveFailed = !file.Save(Book);
        if (Problem != before)
        {
            ProblemChanged?.Invoke();
        }
    }

    public void DismissProblem()
    {
        loadProblem = null;
        ProblemChanged?.Invoke();
    }

    public void SetRule(string name)
    {
        if (Book.Rule == name)
        {
            return;
        }

        Book.Rule = name;
        Save();
        RuleChanged?.Invoke();
    }

    public void SetCustomSplit(CustomSplit split)
    {
        if (Book.CustomSplit == split)
        {
            return;
        }

        Book.CustomSplit = split;
        Save();
        if (Book.Rule == BudgetRule.CustomName)
        {
            RuleChanged?.Invoke();
        }
    }
}

/// <summary>Where the app keeps its data and puts exports.</summary>
public static class AppFolders
{
    // Set CHECKYOURBUDGET_DATA to try the app on a scratch copy instead of the real data.
    private static readonly string? Override = Environment.GetEnvironmentVariable("CHECKYOURBUDGET_DATA");

    /// <summary>
    /// On Windows the same folder the old desktop app used, so its budget.json is imported on first start.
    /// On Android the app's private storage.
    /// </summary>
    public static string Data =>
        !string.IsNullOrEmpty(Override) ? Override
#if WINDOWS
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CheckYourBudget");
#else
        : FileSystem.AppDataDirectory;
#endif

    /// <summary>On Windows the Documents folder; on Android a cache folder the file is shared from.</summary>
    public static string Export =>
        !string.IsNullOrEmpty(Override) ? Path.Combine(Override, "exports")
#if WINDOWS
        : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
#else
        : FileSystem.CacheDirectory;
#endif
}
