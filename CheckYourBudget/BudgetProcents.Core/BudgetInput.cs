namespace BudgetProcents.Core;

/// <summary>Monthly salary and how much was spent in each expense category.</summary>
public sealed record BudgetInput
{
    public decimal Salary { get; init; }

    public decimal InternetAndTv { get; init; }
    public decimal Food { get; init; }
    public decimal GoingOut { get; init; }
    public decimal HomeStuff { get; init; }
    public decimal Clothes { get; init; }
    public decimal Gas { get; init; }
    public decimal Dad { get; init; }
    public decimal Hobby { get; init; }
    public decimal Car { get; init; }
    public decimal Cat { get; init; }
    public decimal ElectricityAndWater { get; init; }
    public decimal Cinema { get; init; }
    public decimal Haircut { get; init; }
    public decimal Games { get; init; }
    public decimal Future { get; init; }
    public decimal Health { get; init; }
}
