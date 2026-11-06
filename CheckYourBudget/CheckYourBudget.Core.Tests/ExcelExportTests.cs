using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace CheckYourBudget.Core.Tests;

[TestClass]
public sealed class ExcelExportTests
{
    private static BudgetBook Book()
    {
        BudgetBook book = new();
        book.Add(new MonthBudget
        {
            Month = new(2026, 10),
            Income = [new("Salary", "2000")],
            Needs = [new("Food", "400"), new("Rent", "600")],
            Wants = [new("Cinema", "100")],
            Investment = [new("ETF", "300")],
            Savings = [new("Emergency fund", "100")],
        });
        book.Add(new MonthBudget
        {
            Month = new(2026, 11),
            Income = [new("Salary", "2000"), new("Side job", "500")],
            Needs = [new("Food", "450")],
        });
        return book;
    }

    private static SpreadsheetDocument Export(BudgetBook book)
    {
        MemoryStream stream = new();
        ExcelExport.Write(book, stream);
        stream.Position = 0;
        return SpreadsheetDocument.Open(stream, isEditable: false);
    }

    private static Dictionary<string, Cell> Cells(SpreadsheetDocument document, string sheetName)
    {
        WorkbookPart workbook = document.WorkbookPart!;
        Sheet sheet = workbook.Workbook!.Descendants<Sheet>().Single(s => s.Name == sheetName);
        WorksheetPart part = (WorksheetPart)workbook.GetPartById(sheet.Id!);
        return part.Worksheet!.Descendants<Cell>().ToDictionary(c => c.CellReference!.Value!);
    }

    private static string TextOf(Cell cell) => cell.InlineString?.InnerText ?? cell.CellValue?.Text ?? "";

    private static decimal NumberOf(Cell cell)
    {
        Assert.AreEqual(CellValues.Number, cell.DataType!.Value, $"{cell.CellReference} should be a number");
        return decimal.Parse(cell.CellValue!.Text, System.Globalization.CultureInfo.InvariantCulture);
    }

    [TestMethod]
    public void Has_a_summary_sheet_then_one_sheet_per_month_newest_first()
    {
        using SpreadsheetDocument document = Export(Book());

        string?[] names = [.. document.WorkbookPart!.Workbook!.Descendants<Sheet>().Select(s => s.Name?.Value)];
        CollectionAssert.AreEqual(new[] { "Summary", "November 2026", "October 2026" }, names);
    }

    [TestMethod]
    public void The_summary_has_one_row_of_numbers_per_month()
    {
        using SpreadsheetDocument document = Export(Book());
        Dictionary<string, Cell> cells = Cells(document, "Summary");

        Assert.AreEqual("Rule: 50/40/10", TextOf(cells["A2"]));
        Assert.AreEqual("Month", TextOf(cells["A4"]));
        Assert.AreEqual("November 2026", TextOf(cells["A5"]));
        Assert.AreEqual(2500m, NumberOf(cells["B5"]));
        Assert.AreEqual("October 2026", TextOf(cells["A6"]));
        Assert.AreEqual(2000m, NumberOf(cells["B6"]), "income");
        Assert.AreEqual(1500m, NumberOf(cells["C6"]), "spent");
        Assert.AreEqual(500m, NumberOf(cells["D6"]), "left over");
        Assert.AreEqual(1000m, NumberOf(cells["E6"]), "needs");
        Assert.AreEqual(0.5m, NumberOf(cells["F6"]), "needs is 50%, stored as a fraction and shown as a percentage");
    }

    [TestMethod]
    public void A_month_sheet_lists_every_item_the_totals_and_the_rule()
    {
        using SpreadsheetDocument document = Export(Book());
        Dictionary<string, Cell> cells = Cells(document, "October 2026");
        Dictionary<string, string> text = cells.ToDictionary(c => c.Key, c => TextOf(c.Value));

        Assert.AreEqual("October 2026", text["A1"]);
        Assert.AreEqual("Income", text["A4"]);
        Assert.AreEqual("Salary", text["B4"]);
        Assert.AreEqual("Needs", text["A5"]);
        Assert.AreEqual("Food", text["B5"]);
        Assert.AreEqual(400m, NumberOf(cells["C5"]));
        Assert.AreEqual(0.2m, NumberOf(cells["D5"]));

        string leftOver = text.Single(t => t.Value == "Left over").Key;
        Assert.AreEqual(500m, NumberOf(cells["C" + leftOver[1..]]));

        string[] results = [.. text.Where(t => t.Key.StartsWith('F') && t.Value is "OK" or "Over" or "Under").Select(t => t.Value)];
        CollectionAssert.AreEqual(new[] { "OK", "Under", "OK" }, results, "needs 50% OK, investment+savings 20% under 40%, wants 5% OK");
    }

    [TestMethod]
    public void An_empty_book_still_makes_a_workbook()
    {
        using SpreadsheetDocument document = Export(new BudgetBook());

        Assert.AreEqual("Summary", document.WorkbookPart!.Workbook!.Descendants<Sheet>().Single().Name?.Value);
    }
}
