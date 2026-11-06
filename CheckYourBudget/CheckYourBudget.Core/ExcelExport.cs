using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace CheckYourBudget.Core;

/// <summary>Writes the budget book as an Excel workbook: a summary of all months, then one sheet per month.</summary>
public static class ExcelExport
{
    // Indexes into the cell formats of the stylesheet below.
    private const uint Plain = 0;
    private const uint Bold = 1;
    private const uint Euro = 2;
    private const uint Percent = 3;
    private const uint BoldEuro = 4;
    private const uint Title = 5;

    public static string FileName(DateTime now) => $"Budget {now:yyyy-MM-dd}.xlsx";

    public static void Write(BudgetBook book, Stream output)
    {
        using SpreadsheetDocument document = SpreadsheetDocument.Create(output, SpreadsheetDocumentType.Workbook);
        WorkbookPart workbook = document.AddWorkbookPart();
        workbook.Workbook = new Workbook();
        workbook.AddNewPart<WorkbookStylesPart>().Stylesheet = Styles();
        Sheets sheets = workbook.Workbook.AppendChild(new Sheets());

        BudgetRule rule = book.CurrentRule();
        AddSheet(workbook, sheets, "Summary", [24, 14, 14, 14, 14, 12, 14, 12, 14, 13, 14, 12], SummaryRows(book, rule));

        // Newest month first, matching the History screen.
        foreach (MonthBudget month in book.Months.OrderByDescending(m => m.Month))
        {
            AddSheet(workbook, sheets, month.Month.Title, [24, 26, 14, 14, 14, 12], MonthRows(month, rule));
        }

        workbook.Workbook.Save();
    }

    private static IEnumerable<Row> SummaryRows(BudgetBook book, BudgetRule rule)
    {
        yield return Line(Text("Budget by month", Title));
        yield return Line(Text($"Rule: {RuleTitle(book, rule)}"));
        yield return Line();
        yield return Line(
            Text("Month", Bold), Text("Income", Bold), Text("Spent", Bold), Text("Left over", Bold),
            Text("Needs", Bold), Text("Needs %", Bold), Text("Wants", Bold), Text("Wants %", Bold),
            Text("Investment", Bold), Text("Investment %", Bold), Text("Savings", Bold), Text("Savings %", Bold));

        foreach (MonthBudget month in book.Months.OrderByDescending(m => m.Month))
        {
            BudgetSummary s = month.Summarize(rule);
            yield return Line(
                Text(month.Month.Title), Number(s.Income, Euro), Number(s.TotalSpent, Euro), Number(s.Residue, Euro),
                Number(s.Needs.Amount, Euro), Number(s.Needs.Percent / 100, Percent),
                Number(s.Wants.Amount, Euro), Number(s.Wants.Percent / 100, Percent),
                Number(s.Investment.Amount, Euro), Number(s.Investment.Percent / 100, Percent),
                Number(s.Savings.Amount, Euro), Number(s.Savings.Percent / 100, Percent));
        }
    }

    private static IEnumerable<Row> MonthRows(MonthBudget month, BudgetRule rule)
    {
        BudgetSummary s = month.Summarize(rule);

        yield return Line(Text(month.Month.Title, Title));
        yield return Line();
        yield return Line(Text("Section", Bold), Text("Item", Bold), Text("Amount", Bold), Text("% of income", Bold));

        foreach (BudgetItem item in month.Income)
        {
            yield return Line(Text("Income"), Text(item.Name), Number(item.Value, Euro));
        }

        foreach (Category category in Enum.GetValues<Category>())
        {
            foreach (BudgetItem item in month.ItemsOf(category))
            {
                yield return Line(Text(category.ToString()), Text(item.Name), Number(item.Value, Euro),
                    Number(BudgetCalculator.ShareOf(s.Income, item.Value).Percent / 100, Percent));
            }
        }

        yield return Line();
        yield return Line(Text("Income", Bold), Empty(), Number(s.Income, BoldEuro));
        yield return Line(Text("Spent", Bold), Empty(), Number(s.TotalSpent, BoldEuro));
        yield return Line(Text("Left over", Bold), Empty(), Number(s.Residue, BoldEuro));

        yield return Line();
        yield return Line(Text($"Rule {rule.Name}", Bold));
        yield return Line(Text("Target", Bold), Text("Goal", Bold), Text("Goal amount", Bold),
            Text("Actual", Bold), Text("Actual %", Bold), Text("Result", Bold));
        foreach (TargetCheck check in s.Targets)
        {
            string goal = $"{(check.Target.Kind == TargetKind.AtMost ? "Up to" : "At least")} {check.Target.Percent:0.##}%";
            string result = check.IsMet ? "OK" : check.Target.Kind == TargetKind.AtMost ? "Over" : "Under";
            yield return Line(Text(check.Target.Name), Text(goal), Number(check.Goal.Amount, Euro),
                Number(check.Actual.Amount, Euro), Number(check.Actual.Percent / 100, Percent), Text(result));
        }
    }

    private static string RuleTitle(BudgetBook book, BudgetRule rule)
        => rule.Name == BudgetRule.CustomName ? $"Custom {book.CustomSplit}" : rule.Name;

    private static void AddSheet(WorkbookPart workbook, Sheets sheets, string name, double[] widths, IEnumerable<Row> rows)
    {
        WorksheetPart part = workbook.AddNewPart<WorksheetPart>();
        SheetData data = new();
        uint index = 1;
        foreach (Row row in rows)
        {
            row.RowIndex = index;
            int column = 0;
            foreach (Cell cell in row.Elements<Cell>())
            {
                cell.CellReference = $"{ColumnName(column++)}{index}";
            }

            data.Append(row);
            index++;
        }

        Columns columns = new(widths.Select((width, i) =>
            new Column { Min = (uint)i + 1, Max = (uint)i + 1, Width = width, CustomWidth = true }));
        part.Worksheet = new Worksheet(columns, data);

        sheets.Append(new Sheet
        {
            Id = workbook.GetIdOfPart(part),
            SheetId = (uint)sheets.Count() + 1,
            Name = name,
        });
    }

    private static string ColumnName(int index)
    {
        string name = "";
        for (index++; index > 0; index = (index - 1) / 26)
        {
            name = (char)('A' + (index - 1) % 26) + name;
        }

        return name;
    }

    private static Row Line(params Cell[] cells) => new(cells);

    private static Cell Empty() => new();

    private static Cell Text(string text, uint style = Plain) => new()
    {
        DataType = CellValues.InlineString,
        InlineString = new InlineString(new Text(text)),
        StyleIndex = style,
    };

    private static Cell Number(decimal value, uint style) => new()
    {
        DataType = CellValues.Number,
        CellValue = new CellValue(Math.Round(value, 4)),
        StyleIndex = style,
    };

    private static Stylesheet Styles() => new(
        new NumberingFormats(new NumberingFormat { NumberFormatId = 164, FormatCode = "#,##0.00 \"€\"" }) { Count = 1 },
        new Fonts(
            new Font(),
            new Font(new DocumentFormat.OpenXml.Spreadsheet.Bold()),
            new Font(new DocumentFormat.OpenXml.Spreadsheet.Bold(), new FontSize { Val = 14 })) { Count = 3 },
        // Excel expects the first two fills to be these two.
        new Fills(
            new Fill(new PatternFill { PatternType = PatternValues.None }),
            new Fill(new PatternFill { PatternType = PatternValues.Gray125 })) { Count = 2 },
        new Borders(new Border()) { Count = 1 },
        new CellFormats(
            new CellFormat(),                                                                         // Plain
            new CellFormat { FontId = 1, ApplyFont = true },                                          // Bold
            new CellFormat { NumberFormatId = 164, ApplyNumberFormat = true },                        // Euro
            new CellFormat { NumberFormatId = 10, ApplyNumberFormat = true },                         // Percent (0.00%)
            new CellFormat { NumberFormatId = 164, FontId = 1, ApplyNumberFormat = true, ApplyFont = true }, // BoldEuro
            new CellFormat { FontId = 2, ApplyFont = true }) { Count = 6 });                          // Title
}
