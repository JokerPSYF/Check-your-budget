namespace BudgetProcents.Core;

public static class BudgetCalculator
{
    public const string Currency = "€";

    // Rule 50/40/10: needs / investment / wants, as a percentage of the salary.
    public const decimal NeedsTargetPercent = 50m;
    public const decimal InvestmentTargetPercent = 40m;
    public const decimal WantsTargetPercent = 10m;

    public static BudgetResult Calculate(BudgetInput input)
    {
        decimal salary = input.Salary;

        decimal car = input.Gas + input.Car;
        decimal games = input.Games;
        decimal home = input.InternetAndTv + input.HomeStuff + input.Dad + input.Cat + input.ElectricityAndWater;
        decimal food = input.Food;
        decimal clothes = input.Clothes + input.Haircut;
        decimal fun = input.GoingOut + input.Cinema;
        decimal hobby = input.Hobby;
        decimal future = input.Future;
        decimal health = input.Health;
        decimal sum = car + games + home + food + clothes + fun + hobby + future + health;

        decimal needs = car + home + food + clothes + health;
        decimal investment = future;
        decimal wants = games + fun + hobby;

        Share ShareOf(decimal amount) => new(amount, Percent(salary, amount));
        Share Target(decimal percent) => new(salary * percent / 100, percent);

        return new BudgetResult
        {
            Car = ShareOf(car),
            Games = ShareOf(games),
            Home = ShareOf(home),
            Food = ShareOf(food),
            Clothes = ShareOf(clothes),
            Fun = ShareOf(fun),
            Hobby = ShareOf(hobby),
            Future = ShareOf(future),
            Health = ShareOf(health),

            TotalSpent = sum,
            Residue = salary - sum,

            Needs = ShareOf(needs),
            Investment = ShareOf(investment),
            Wants = ShareOf(wants),

            NeedsTarget = Target(NeedsTargetPercent),
            InvestmentTarget = Target(InvestmentTargetPercent),
            WantsTarget = Target(WantsTargetPercent),
        };
    }

    /// <summary>What percentage of the salary the amount is. 0 when there is no salary yet.</summary>
    private static decimal Percent(decimal salary, decimal amount)
        => salary == 0 ? 0 : amount / salary * 100;
}
