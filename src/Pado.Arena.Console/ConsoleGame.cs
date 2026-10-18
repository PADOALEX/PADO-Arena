using Pado.Arena.Core.Combat;
using Pado.Arena.Core.Game;
using Pado.Arena.Core.Persistence;

namespace Pado.Arena.ConsoleApp;

public sealed class ConsoleGame(TextReader input, TextWriter output, IRandomSource random)
{
    private readonly TextReader _input = input ?? throw new ArgumentNullException(nameof(input));
    private readonly TextWriter _output = output ?? throw new ArgumentNullException(nameof(output));
    private readonly IRandomSource _random = random ?? throw new ArgumentNullException(nameof(random));
    private const string SaveFile = "arena-save.json";
    private const string LogFile = "battlelog.txt";

    public int Run()
    {
        _output.WriteLine("==========================================");
        _output.WriteLine("      PADO ARENA | TURN-BASED RPG          ");
        _output.WriteLine("==========================================");
        _output.WriteLine("1) New game   2) Load game   3) Quit");
        while (true)
        {
            _output.Write("Select: ");
            string? choice = _input.ReadLine();
            if (choice is null or "3") return 0;
            if (choice == "1")
            {
                GameSession? session = CreateNewGame();
                if (session is null) return 0;
                return BattleLoop(session);
            }
            if (choice == "2")
            {
                try { return BattleLoop(SaveGameStore.Load(SaveFile, _random)); }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
                {
                    _output.WriteLine($"Could not load save: {error.Message}");
                }
            }
            else _output.WriteLine("Choose 1, 2 or 3.");
        }
    }

    private GameSession? CreateNewGame()
    {
        _output.WriteLine("Choose class: 1) Warrior (tank) 2) Mage (fireball) 3) Archer (double shot)");
        string? type;
        while (true)
        {
            _output.Write("Class: ");
            string? selection = _input.ReadLine();
            if (selection is null) return null;
            type = selection switch { "1" => "Warrior", "2" => "Mage", "3" => "Archer", _ => null };
            if (type is not null) break;
            _output.WriteLine("Choose 1, 2 or 3.");
        }
        _output.Write("Hero name (Enter for Hero): ");
        string? rawName = _input.ReadLine();
        if (rawName is null) return null;
        string name = string.IsNullOrWhiteSpace(rawName) ? "Hero" : rawName.Trim();
        if (name.Length > 80) name = name[..80];
        return GameFactory.NewGame(type!, name, _random);
    }

    private int BattleLoop(GameSession session)
    {
        _output.WriteLine($"\nWelcome, {session.Hero.Name} the {session.Hero.ClassId}! Fight three challengers.");
        int announced = -1;
        while (session.Status == GameStatus.InProgress)
        {
            if (announced != session.CurrentEnemyIndex)
            {
                announced = session.CurrentEnemyIndex;
                _output.WriteLine($"\n===== BATTLE {announced + 1}/3 | {session.CurrentEnemy!.Name} =====");
            }
            var hero = session.Hero;
            var enemy = session.CurrentEnemy!;
            _output.WriteLine($"{hero.Name} Lv.{hero.Level}: {hero.Health}/{hero.MaxHealth} HP | {hero.Mana}/{hero.MaxMana} MP | ATK {hero.Attack} DEF {hero.Defense} | XP {hero.Experience}/{hero.Level * 100}");
            _output.WriteLine($"{enemy.Name}: {enemy.Health}/{enemy.MaxHealth} HP | {enemy.Mana}/{enemy.MaxMana} MP");
            _output.WriteLine($"1) Attack   2) Defend (+15 MP)   3) {hero.Special.Name} ({hero.Special.ManaCost} MP)   4) Item   5) Save & exit   6) Exit");
            _output.Write("Action: ");
            string? action = _input.ReadLine();
            if (action is null or "6") { WriteLog(session); return 0; }
            if (action == "5")
            {
                try
                {
                    SaveGameStore.Save(SaveFile, session);
                    _output.WriteLine($"Progress saved to {SaveFile}.");
                    WriteLog(session);
                    return 0;
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException)
                {
                    _output.WriteLine($"Could not save progress: {e.Message}");
                    continue;
                }
            }
            PlayerCommand? command = action switch
            {
                "1" => new(PlayerAction.Attack),
                "2" => new(PlayerAction.Defend),
                "3" => new(PlayerAction.Special),
                "4" => ChooseItem(session),
                _ => null
            };
            if (command is null) { _output.WriteLine("No action taken."); continue; }
            TurnResult outcome = session.Play(command);
            foreach (BattleEvent e in outcome.Events) _output.WriteLine("  > " + e.Message);
        }
        _output.WriteLine(session.Status == GameStatus.Victory ? "\n*** VICTORY! ***" : "\n*** GAME OVER ***");
        WriteLog(session);
        return 0;
    }

    private PlayerCommand? ChooseItem(GameSession session)
    {
        var items = session.Hero.Inventory;
        if (items.Count == 0) { _output.WriteLine("Inventory empty."); return null; }
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            _output.WriteLine($"{i + 1}) {item.Name} | HP +{item.HealAmount}, MP +{item.ManaRestore}, ATK +{item.AttackBoost}, DEF +{item.DefenseBoost}");
        }
        _output.Write("Item number (0 to cancel): ");
        string? line = _input.ReadLine();
        if (!int.TryParse(line, out int index) || index == 0) return null;
        return new PlayerCommand(PlayerAction.Item, index - 1);
    }

    private void WriteLog(GameSession session)
    {
        try
        {
            File.WriteAllLines(LogFile, session.History);
            _output.WriteLine($"Battle history written to {LogFile} ({session.History.Count} events).");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            _output.WriteLine($"Could not write battle history: {error.Message}");
        }
    }
}