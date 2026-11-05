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

            Console.Write("Money for dad: "); decimal dad = decimal.Parse(Console.ReadLine());

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
            BudgetResult result = BudgetCalculator.Calculate(new BudgetInput
            {
                Salary = salary,
                InternetAndTv = TV,
                Food = food,
                GoingOut = goOut,
                HomeStuff = homeStuf,
                Clothes = clothes,
                Gas = gas,
                Dad = dad,
                Hobby = hobby,
                Car = car,
                Cat = cat,
                ElectricityAndWater = electricity,
                Cinema = cinema,
                Haircut = cutHair,
                Games = games,
                Future = future,
                Health = health,
            });
            const string currency = BudgetCalculator.Currency;

            //OUTPUT
            Console.WriteLine();

            Console.WriteLine($"Rule : 50/40/10 \n50% ({result.NeedsTarget.Amount:f2} {currency}) for needs (food, house, transport, debt) \n" +
                $"40% ({result.InvestmentTarget.Amount:f2} {currency})for investment (emergency fund, self-education," +
                $" stocks/ETFs, rental, properties\n" +
                $"10% ({result.WantsTarget.Amount:f2} {currency}) for wants " +
                $"(restaurants, entertainment, vacations, luxuary items, electronics)");

            Console.WriteLine($"Money for car:{result.Car.Percent:f2}% ");

            Console.WriteLine($"Money for games:{result.Games.Percent:f2}% ");

            Console.WriteLine($"Money for home:{result.Home.Percent:f2}% ");

            Console.WriteLine($"Money for food:{result.Food.Percent:f2}% ");

            Console.WriteLine($"Money for clothes:{result.Clothes.Percent:f2}% ");

            Console.WriteLine($"Money for fun:{result.Fun.Percent:f2}% ");

            Console.WriteLine($"Money for hobby: {result.Hobby.Percent:f2}% ");

            Console.WriteLine($"Money for your future:{result.Future.Percent:f2}% ");

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