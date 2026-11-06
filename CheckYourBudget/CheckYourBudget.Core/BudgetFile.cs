using System.Text.Json;
using System.Text.Json.Serialization;

namespace CheckYourBudget.Core;

/// <summary>What <see cref="BudgetFile.Load"/> found.</summary>
/// <param name="Problem">Set when the saved file couldn't be used. Show it to the user.</param>
public sealed record LoadResult(BudgetBook Book, string? Problem);

/// <summary>Reads and writes the budget book, budget-book.json, in one folder.</summary>
/// <remarks>
/// Nothing the user typed is ever overwritten by mistake:
/// <list type="bullet">
/// <item>A save writes a temporary file and then swaps it in, so a crash halfway leaves the previous file whole.</item>
/// <item>A file that can't be read is moved aside before anything new is saved, and the user is told where it went.</item>
/// <item>If it can't be moved aside, or was saved by a newer version of the app, saving stays off for the session.</item>
/// </list>
/// </remarks>
public sealed class BudgetFile(string folder)
{
    public const string FileName = "budget-book.json";

    /// <summary>The file of the Windows app before months existed. It is imported once and never written.</summary>
    public const string OldFileName = "budget.json";

    private bool savingBlocked;

    public string FilePath { get; } = Path.Combine(folder, FileName);

    private string OldFilePath => Path.Combine(folder, OldFileName);

    public LoadResult Load()
    {
        savingBlocked = false;
        if (File.Exists(FilePath))
        {
            return LoadBook();
        }

        // First run of this version: bring over what the old Windows app saved, if anything.
        return new(File.Exists(OldFilePath) ? ImportOld() ?? new BudgetBook() : new BudgetBook(), null);
    }

    /// <summary>Saves the book. False when it couldn't be saved, or saving is off because of a problem found by <see cref="Load"/>.</summary>
    public bool Save(BudgetBook book)
    {
        if (savingBlocked)
        {
            return false;
        }

        string temp = FilePath + ".tmp";
        try
        {
            Directory.CreateDirectory(folder);
            using (FileStream stream = new(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, book, BudgetJson.Default.BudgetBook);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temp, FilePath, overwrite: true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private LoadResult LoadBook()
    {
        string json;
        try
        {
            json = File.ReadAllText(FilePath);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Blocked("Your saved budget couldn't be opened, so changes won't be saved this time to keep it safe.");
        }

        BudgetBook? book;
        try
        {
            book = JsonSerializer.Deserialize(json, BudgetJson.Default.BudgetBook);
        }
        catch (JsonException)
        {
            book = null;
        }

        if (book is null)
        {
            string? keptAs = SetAside();
            return keptAs is null
                ? Blocked("Your saved budget couldn't be read, and it couldn't be moved out of the way, so changes won't be saved this time.")
                : new(new BudgetBook(), $"Your saved budget couldn't be read. It was kept as {keptAs} and the app started fresh.");
        }

        if (book.Version > BudgetBook.CurrentVersion)
        {
            return Blocked("Your budget was saved by a newer version of the app. Update the app; until then changes won't be saved.");
        }

        book.Normalize();
        book.Version = BudgetBook.CurrentVersion;
        return new(book, null);
    }

    private LoadResult Blocked(string problem)
    {
        savingBlocked = true;
        return new(new BudgetBook(), problem);
    }

    // Renames the unreadable file to budget-book.unreadable-<date>.json. Returns the new name, or null if it failed.
    private string? SetAside()
    {
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        for (int attempt = 1; attempt <= 100; attempt++)
        {
            string name = attempt == 1
                ? $"budget-book.unreadable-{stamp}.json"
                : $"budget-book.unreadable-{stamp}-{attempt}.json";
            string target = Path.Combine(folder, name);
            if (File.Exists(target))
            {
                continue;
            }

            try
            {
                File.Move(FilePath, target);
                return name;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }

        return null;
    }

    private BudgetBook? ImportOld()
    {
        OldBudget? old;
        try
        {
            old = JsonSerializer.Deserialize(File.ReadAllText(OldFilePath), BudgetJson.Default.OldBudget);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }

        if (old is null)
        {
            return null;
        }

        YearMonth month = YearMonth.Current;
        MonthBudget defaults = MonthBudget.Defaults(month);
        MonthBudget imported = new()
        {
            Month = month,
            Income = [new BudgetItem("Salary", old.Salary ?? "")],
            // A list missing from the old file (Savings in files older than it) starts with the default items.
            Needs = Items(old.Needs) ?? defaults.Needs,
            Wants = Items(old.Wants) ?? defaults.Wants,
            Investment = Items(old.Investment) ?? defaults.Investment,
            Savings = Items(old.Savings) ?? defaults.Savings,
        };

        BudgetBook book = new()
        {
            Rule = BudgetRule.BuiltIn.FirstOrDefault(r => r.Name == old.Rule)?.Name ?? BudgetRule.FiftyFortyTen.Name,
        };
        book.Add(imported);
        book.Normalize();
        return book;

        static List<BudgetItem>? Items(List<OldItem?>? items)
            => items?.OfType<OldItem>().Select(i => new BudgetItem(i.Name ?? "", i.Amount ?? "")).ToList();
    }
}

/// <summary>The file the Windows app saved before months existed: one budget with a single salary.</summary>
internal sealed record OldBudget(
    string? Salary,
    List<OldItem?>? Needs,
    List<OldItem?>? Wants,
    List<OldItem?>? Investment,
    List<OldItem?>? Savings,
    string? Rule);

internal sealed record OldItem(string? Name, string? Amount);

// Generated at build time instead of found by reflection, so it keeps working in trimmed Android builds.
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(BudgetBook))]
[JsonSerializable(typeof(OldBudget))]
internal sealed partial class BudgetJson : JsonSerializerContext;
