using Pado.Arena.Core.Characters;
using Pado.Arena.Core.Combat;
using Pado.Arena.Core.Inventory;
using Pado.Arena.Core.Persistence;

namespace Pado.Arena.Core.Game;

public enum GameStatus { InProgress, Victory, Defeat }

public sealed class GameSession
{
    private readonly CombatEngine _engine;
    private readonly List<Enemy> _enemies;
    private readonly List<string> _history = [];

    public GameSession(Hero hero, IEnumerable<Enemy> enemies, IRandomSource random)
    {
        Hero = hero ?? throw new ArgumentNullException(nameof(hero));
        ArgumentNullException.ThrowIfNull(enemies);
        _enemies = enemies.ToList();
        if (_enemies.Count != 3 || _enemies.Any(e => e is null))
            throw new ArgumentException("An arena must contain exactly three enemies.", nameof(enemies));
        _engine = new CombatEngine(random);
    }

    public Hero Hero { get; }
    public IReadOnlyList<Enemy> Enemies => _enemies.AsReadOnly();
    public IReadOnlyList<string> History => _history.AsReadOnly();
    public int CurrentEnemyIndex { get; private set; }
    public Enemy? CurrentEnemy => CurrentEnemyIndex < _enemies.Count ? _enemies[CurrentEnemyIndex] : null;
    public GameStatus Status => !Hero.IsAlive ? GameStatus.Defeat : CurrentEnemyIndex >= _enemies.Count ? GameStatus.Victory : GameStatus.InProgress;

    public TurnResult Play(PlayerCommand command)
    {
        if (Status != GameStatus.InProgress)
            return new(false, [new(BattleEventKind.Information, "The arena run has ended.")]);
        Enemy enemy = CurrentEnemy!;
        TurnResult turn = _engine.Resolve(Hero, enemy, command);
        if (!turn.Success) return turn;
        List<BattleEvent> events = [.. turn.Events];
        if (Hero.IsAlive && !enemy.IsAlive)
        {
            events.Add(new(BattleEventKind.Victory, $"Defeated {enemy.Name}!"));
            int levelUps = Hero.GainExperience(enemy.ExperienceReward);
            events.Add(new(BattleEventKind.Progress, $"Gained {enemy.ExperienceReward} XP. Level {Hero.Level} ({Hero.Experience}/{Hero.Level * 100} XP)."));
            if (levelUps > 0)
                events.Add(new(BattleEventKind.Progress, $"Level up x{levelUps}! +15 Max HP, +10 Max MP, +2 ATK, +1 DEF per level."));
            Hero.Heal(30);
            Hero.RestoreMana(20);
            Hero.AddItem(Item.SmallPotion);
            events.Add(new(BattleEventKind.Item, "Recovered up to 30 HP and 20 MP; found a Small Potion."));
            CurrentEnemyIndex++;
            if (Status == GameStatus.Victory)
                events.Add(new(BattleEventKind.Victory, "ARENA CLEARED! All three enemies defeated."));
            else
                events.Add(new(BattleEventKind.Information, $"Next challenger: {CurrentEnemy!.Name}."));
        }
        else if (!Hero.IsAlive)
        {
            events.Add(new(BattleEventKind.Defeat, "GAME OVER. Your arena run has ended."));
        }
        foreach (BattleEvent e in events) _history.Add(e.Message);
        return new(true, events);
    }

    public GameSave CaptureSave() => new(
        1, CurrentEnemyIndex,
        new HeroSave(Hero.ClassId, Hero.Name, Hero.MaxHealth, Hero.Health, Hero.MaxMana, Hero.Mana,
            Hero.Attack, Hero.Defense, Hero.Level, Hero.Experience, Hero.IsDefending, [.. Hero.Inventory]),
        _enemies.Select(e => new EnemySave(e.TypeId, e.Health, e.Mana)).ToList(),
        [.. _history]);

    public static GameSession FromSave(GameSave save, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.Version != 1 || save.CurrentEnemyIndex is < 0 or > 3 ||
            save.Hero is null || save.Enemies is null || save.Enemies.Count != 3 ||
            save.History is null || save.History.Count > 10_000 ||
            save.History.Any(x => x is null || x.Length > 1_000))
            throw new InvalidDataException("Invalid or unsupported arena save.");
        Hero hero = GameFactory.CreateHero(save.Hero.ClassId, save.Hero.Name);
        hero.Load(save.Hero);
        List<Enemy> enemies = [];
        foreach (EnemySave state in save.Enemies)
        {
            if (state is null) throw new InvalidDataException("Missing enemy state.");
            Enemy enemy = Enemy.Create(state.TypeId);
            enemy.Load(state.Health, state.Mana);
            enemies.Add(enemy);
        }
        for (int i = 0; i < 3; i++)
        {
            if (i < save.CurrentEnemyIndex && enemies[i].IsAlive)
                throw new InvalidDataException("An already defeated enemy is alive in the save.");
            if (i > save.CurrentEnemyIndex && !enemies[i].IsAlive)
                throw new InvalidDataException("A future enemy is defeated in the save.");
        }
        if (save.CurrentEnemyIndex < 3 && !enemies[save.CurrentEnemyIndex].IsAlive)
            throw new InvalidDataException("Current enemy cannot already be defeated.");
        if (!hero.IsAlive && save.CurrentEnemyIndex == 3)
            throw new InvalidDataException("A defeated hero cannot have won the arena.");
        GameSession session = new(hero, enemies, random) { CurrentEnemyIndex = save.CurrentEnemyIndex };
        session._history.AddRange(save.History);
        return session;
    }
}