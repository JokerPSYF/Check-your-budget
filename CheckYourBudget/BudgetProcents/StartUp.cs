using System;
using System.Text;
using BudgetProcents.Core;

namespace BudgetTest
{
   public class StartUp
    {
        public static void Main()
        {
            //ПРОГРАМА КОЯТО ЩЕ ПОКАЖЕ ПО ПРОЦЕНТИ ЗА КАКВО СИ ХАЧРИШ ПАРИТЕ НАЙ-МНОГО В ДАДЕНИЯ МЕСЕЦ

            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.White;

            Console.OutputEncoding = Encoding.Unicode;


            //INPUT
            Console.Write("Put your salary: "); decimal salary = decimal.Parse(Console.ReadLine());

            Console.Write("Money for Internet and TV: "); decimal TV = decimal.Parse(Console.ReadLine());

            Console.Write("Money for food: "); decimal food = decimal.Parse(Console.ReadLine());

            Console.Write("Money for go out: "); decimal goOut = decimal.Parse(Console.ReadLine());

            Console.Write("Money for home stuf: "); decimal homeStuf = decimal.Parse(Console.ReadLine());

            Console.Write("Money for clothes: "); decimal clothes = decimal.Parse(Console.ReadLine());

            Console.Write("Money for gas: "); decimal gas = decimal.Parse(Console.ReadLine());

            Console.Write("Money for hobby: "); decimal hobby = decimal.Parse(Console.ReadLine());

            Console.Write("Money for car: "); decimal car = decimal.Parse(Console.ReadLine());

            Console.Write("Money for cat: "); decimal cat = decimal.Parse(Console.ReadLine());

            Console.Write("Money for electricity and water: "); decimal electricity = decimal.Parse(Console.ReadLine());

            Console.Write("Money for cinema: "); decimal cinema = decimal.Parse(Console.ReadLine());

            Console.Write("Money for cut hair: "); decimal cutHair = decimal.Parse(Console.ReadLine());

            Console.Write("Money for games: "); decimal games = decimal.Parse(Console.ReadLine());

            Console.Write("Money for your future: "); decimal future = decimal.Parse(Console.ReadLine());

            Console.Write("Money for your health: "); decimal health = decimal.Parse(Console.ReadLine());


            //LOGIC
            BudgetSummary result = BudgetCalculator.Summarize(salary,
            [
                new("Internet and TV", Category.Needs, TV),
                new("Food", Category.Needs, food),
                new("Go out", Category.Wants, goOut),
                new("Home stuff", Category.Needs, homeStuf),
                new("Clothes", Category.Needs, clothes),
                new("Gas", Category.Needs, gas),
                new("Hobby", Category.Wants, hobby),
                new("Car", Category.Needs, car),
                new("Cat", Category.Needs, cat),
                new("Electricity and water", Category.Needs, electricity),
                new("Cinema", Category.Wants, cinema),
                new("Cut hair", Category.Needs, cutHair),
                new("Games", Category.Wants, games),
                new("Your future", Category.Investment, future),
                new("Your health", Category.Needs, health),
            ], BudgetRule.FiftyFortyTen);
            decimal Percent(decimal amount) => BudgetCalculator.ShareOf(salary, amount).Percent;
            decimal Goal(Category category) => result.TargetFor(category).Goal.Amount;
            const string currency = BudgetCalculator.Currency;

            //OUTPUT
            Console.WriteLine();

            Console.WriteLine($"Rule : 50/40/10 \n50% ({Goal(Category.Needs):f2} {currency}) for needs (food, house, transport, debt) \n" +
                $"40% ({Goal(Category.Investment):f2} {currency})for investment (emergency fund, self-education," +
                $" stocks/ETFs, rental, properties\n" +
                $"10% ({Goal(Category.Wants):f2} {currency}) for wants " +
                $"(restaurants, entertainment, vacations, luxuary items, electronics)");

            Console.WriteLine($"Money for car:{Percent(gas + car):f2}% ");

            Console.WriteLine($"Money for games:{Percent(games):f2}% ");

            Console.WriteLine($"Money for home:{Percent(TV + homeStuf + cat + electricity):f2}% ");

            Console.WriteLine($"Money for food:{Percent(food):f2}% ");

            Console.WriteLine($"Money for clothes:{Percent(clothes + cutHair):f2}% ");

            Console.WriteLine($"Money for fun:{Percent(goOut + cinema):f2}% ");

            Console.WriteLine($"Money for hobby: {Percent(hobby):f2}% ");

            Console.WriteLine($"Money for your future:{Percent(future):f2}% ");

            Console.WriteLine($"Residue: {result.Residue}{currency}");

            Console.WriteLine($"Result:");

            Console.WriteLine($"Needs: {result.Needs.Percent:f2}%({(result.Needs.Percent / 100 * salary):f2}{currency})" +
                $" Investment:{result.Investment.Percent:f2}%({(result.Investment.Percent / 100 * salary):f2}{currency})" +
                $" Wants:{result.Wants.Percent:f2}%({(result.Wants.Percent / 100 * salary):f2}{currency})");

            Console.WriteLine("Do you wanna start over y/n");
            string input = Console.ReadLine();

            if (input == "y")
            {
                Console.Clear();
                StartUp.Main();
            }
        }
    }
}