using System.Windows.Input;
using BudgetProcents.Core;

namespace BudgetProcents.Desktop.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private string spentText = "";
    private string leftOverText = "";
    private bool isOverspent;
    private IReadOnlyList<RuleRow> rules = [];
    private IReadOnlyList<GroupRow> groups = [];

    public MainViewModel()
    {
        NeedsFields = [InternetAndTv, HomeStuff, Dad, Cat, ElectricityAndWater, Gas, Car, Food, Clothes, Haircut, Health];
        WantsFields = [GoingOut, Cinema, Hobby, Games];
        InvestmentFields = [Future];

        IEnumerable<AmountField> allFields = [Salary, .. NeedsFields, .. WantsFields, .. InvestmentFields];
        foreach (AmountField field in allFields)
        {
            // Any change recalculates the whole budget, so the results update as you type.
            field.PropertyChanged += (_, _) => Recalculate();
        }

        ClearCommand = new RelayCommand(() =>
        {
            foreach (AmountField field in allFields)
            {
                field.Text = "";
            }
        });

        Recalculate();
    }

    public AmountField Salary { get; } = new("Monthly salary");

    public AmountField InternetAndTv { get; } = new("Internet and TV");
    public AmountField HomeStuff { get; } = new("Home stuff");
    public AmountField Dad { get; } = new("Dad");
    public AmountField Cat { get; } = new("Cat");
    public AmountField ElectricityAndWater { get; } = new("Electricity and water");
    public AmountField Gas { get; } = new("Gas");
    public AmountField Car { get; } = new("Car");
    public AmountField Food { get; } = new("Food");
    public AmountField Clothes { get; } = new("Clothes");
    public AmountField Haircut { get; } = new("Haircut");
    public AmountField Health { get; } = new("Health");
    public AmountField GoingOut { get; } = new("Going out");
    public AmountField Cinema { get; } = new("Cinema");
    public AmountField Hobby { get; } = new("Hobby");
    public AmountField Games { get; } = new("Games");
    public AmountField Future { get; } = new("Your future");

    public IReadOnlyList<AmountField> NeedsFields { get; }
    public IReadOnlyList<AmountField> WantsFields { get; }
    public IReadOnlyList<AmountField> InvestmentFields { get; }

    public ICommand ClearCommand { get; }

    public string Currency => BudgetCalculator.Currency;

    public string SpentText { get => spentText; private set => SetProperty(ref spentText, value); }
    public string LeftOverText { get => leftOverText; private set => SetProperty(ref leftOverText, value); }
    public bool IsOverspent { get => isOverspent; private set => SetProperty(ref isOverspent, value); }
    public IReadOnlyList<RuleRow> Rules { get => rules; private set => SetProperty(ref rules, value); }
    public IReadOnlyList<GroupRow> Groups { get => groups; private set => SetProperty(ref groups, value); }

    private void Recalculate()
    {
        BudgetResult r = BudgetCalculator.Calculate(new BudgetInput
        {
            Salary = Salary.Amount,
            InternetAndTv = InternetAndTv.Amount,
            Food = Food.Amount,
            GoingOut = GoingOut.Amount,
            HomeStuff = HomeStuff.Amount,
            Clothes = Clothes.Amount,
            Gas = Gas.Amount,
            Dad = Dad.Amount,
            Hobby = Hobby.Amount,
            Car = Car.Amount,
            Cat = Cat.Amount,
            ElectricityAndWater = ElectricityAndWater.Amount,
            Cinema = Cinema.Amount,
            Haircut = Haircut.Amount,
            Games = Games.Amount,
            Future = Future.Amount,
            Health = Health.Amount,
        });

        SpentText = Format.Money(r.TotalSpent);
        LeftOverText = Format.Money(r.Residue);
        IsOverspent = r.IsOverspent;

        Rules =
        [
            new("Needs", r.Needs, BudgetCalculator.NeedsTargetPercent,
                $"Up to 50% ({Format.Money(r.NeedsTarget.Amount)}): food, home, transport, health",
                r.NeedsOverTarget ? RuleStatus.Over : RuleStatus.Ok),
            new("Investment", r.Investment, BudgetCalculator.InvestmentTargetPercent,
                $"At least 40% ({Format.Money(r.InvestmentTarget.Amount)}): emergency fund, education, stocks/ETFs, property",
                r.InvestmentUnderTarget ? RuleStatus.Under : RuleStatus.Ok),
            new("Wants", r.Wants, BudgetCalculator.WantsTargetPercent,
                $"Up to 10% ({Format.Money(r.WantsTarget.Amount)}): going out, entertainment, hobbies",
                r.WantsOverTarget ? RuleStatus.Over : RuleStatus.Ok),
        ];

        Groups =
        [
            new("Home", "Internet and TV, home stuff, dad, cat, electricity and water", r.Home),
            new("Car", "Gas, car", r.Car),
            new("Food", null, r.Food),
            new("Clothes", "Clothes, haircut", r.Clothes),
            new("Health", null, r.Health),
            new("Fun", "Going out, cinema", r.Fun),
            new("Hobby", null, r.Hobby),
            new("Games", null, r.Games),
            new("Your future", null, r.Future),
        ];
    }
}
