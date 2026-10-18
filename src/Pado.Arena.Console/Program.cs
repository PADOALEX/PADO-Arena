using Pado.Arena.Core.Combat;

namespace Pado.Arena.ConsoleApp;

internal static class Program
{
    private static int Main(string[] args)
    {
        if (args.Contains("--help", StringComparer.OrdinalIgnoreCase))
        {
            System.Console.WriteLine("PADO Arena | dotnet run --project src/Pado.Arena.Console -- [--seed INTEGER]");
            return 0;
        }
        int? seed = null;
        if (args.Length != 0)
        {
            if (args.Length != 2 || args[0] != "--seed" || !int.TryParse(args[1], out int value))
            {
                System.Console.Error.WriteLine("Invalid arguments. Use --help for usage.");
                return 2;
            }
            seed = value;
        }
        return new ConsoleGame(System.Console.In, System.Console.Out, new SeededRandomSource(seed)).Run();
    }
}