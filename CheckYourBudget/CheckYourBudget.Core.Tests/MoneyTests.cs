namespace CheckYourBudget.Core.Tests;

[TestClass]
public sealed class MoneyTests
{
    [TestMethod]
    [DataRow("12,50", 12.50)]
    [DataRow("12.50", 12.50)]
    [DataRow(" 7 ", 7.0)]
    [DataRow("0", 0.0)]
    [DataRow(".5", 0.5)]
    [DataRow("", 0.0)]
    [DataRow("   ", 0.0)]
    [DataRow(null, 0.0)]
    public void Reads_amounts_with_either_decimal_separator(string? text, double expected)
    {
        Assert.IsTrue(Money.TryParse(text, out decimal amount));
        Assert.AreEqual((decimal)expected, amount);
    }

    [TestMethod]
    [DataRow("-5")]
    [DataRow("abc")]
    [DataRow("12€")]
    [DataRow("1.234,56")]
    [DataRow("1 000")]
    public void Rejects_negative_numbers_letters_and_thousands_separators(string text)
    {
        Assert.IsFalse(Money.TryParse(text, out _));
        Assert.AreEqual(0m, Money.Parse(text));
    }
}
