using System.IO;
using System.Text.Json;

namespace BudgetProcents.Desktop.ViewModels;

/// <summary>What is kept between runs: the salary and each section's items, as typed.</summary>
public sealed record SavedBudget(string? Salary, List<SavedItem>? Needs, List<SavedItem>? Wants, List<SavedItem>? Investment);

public sealed record SavedItem(string? Name, string? Amount);

/// <summary>Reads and writes the budget in %APPDATA%\CheckYourBudget\budget.json.</summary>
internal static class BudgetStore
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CheckYourBudget", "budget.json");

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    /// <summary>The saved budget, or null on the first run or when the file can't be read.</summary>
    public static SavedBudget? Load()
    {
        try
        {
            return File.Exists(FilePath)
                ? JsonSerializer.Deserialize<SavedBudget>(File.ReadAllText(FilePath), Options)
                : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }

    public static void Save(SavedBudget budget)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(budget, Options));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Saving is a convenience: the app keeps working if the file can't be written.
        }
    }
}
