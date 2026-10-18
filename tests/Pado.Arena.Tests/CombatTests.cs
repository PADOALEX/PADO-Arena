using Pado.Arena.Core.Characters;
using Pado.Arena.Core.Combat;
using Pado.Arena.Core.Game;
using Pado.Arena.Core.Inventory;
using Pado.Arena.Core.Persistence;
using Xunit;

namespace Pado.Arena.Tests;

public class CombatTests
{
    private sealed class ControlledRandom(params int[] percentages) : IRandomSource
    {
        private readonly Queue<int> _rolls = new(percentages);
        public int NextPercent() => _rolls.Count > 0 ? _rolls.Dequeue() : 99;
        public int NextInt(int minInclusive, int maxExclusive) => minInclusive;
    }

    [Fact]
    public void NormalAttack_SubtractsArmor_WithMinimumOneDamage()
    {
        var hero = new Warrior("Tester");
        var enemy = Enemy.Create("Fast Goblin");
        new CombatEngine(new ControlledRandom()).Resolve(hero, enemy, new(PlayerAction.Attack));
        Assert.Equal(47, enemy.Health); // 15 ATK - 2 DEF = 13
        Assert.InRange(hero.Health, 0, hero.MaxHealth);
    }

    [Fact]
    public void CriticalHit_DoublesRawDamageBeforeArmor()
    {
        var enemy = Enemy.Create("Fast Goblin");
        new CombatEngine(new ControlledRandom(99, 0)).Resolve(new Warrior("Tester"), enemy, new(PlayerAction.Attack));
        Assert.Equal(32, enemy.Health); // 60 - ((15 * 2) - 2)
    }

    [Fact]
    public void Dodge_PreventsDamage()
    {
        var enemy = Enemy.Create("Fast Goblin");
        new CombatEngine(new ControlledRandom(0)).Resolve(new Warrior("Tester"), enemy, new(PlayerAction.Attack));
        Assert.Equal(enemy.MaxHealth, enemy.Health);
    }

    [Fact]
    public void Defend_IsClearedAfterEnemyAttackEvenIfHeroDodges()
    {
        var hero = new Archer("Tester");
        new CombatEngine(new ControlledRandom(0)).Resolve(hero, Enemy.Create("Fast Goblin"), new(PlayerAction.Defend));
        Assert.False(hero.IsDefending);
        Assert.Equal(hero.MaxHealth, hero.Health);
    }

    [Fact]
    public void Defend_HalvesIncomingDamageAndDoesNotPersist()
    {
        var hero = new Warrior("Tester");
        new CombatEngine(new ControlledRandom()).Resolve(hero, Enemy.Create("Fast Goblin"), new(PlayerAction.Defend));
        Assert.Equal(148, hero.Health); // (12 - 8) / 2
        Assert.False(hero.IsDefending);
    }

    [Fact]
    public void InsufficientMana_DoesNotConsumeTurnOrChangeHealth()
    {
        var hero = new Warrior("Tester");
        Assert.True(hero.TrySpendMana(40));
        var enemy = Enemy.Create("Fast Goblin");
        var result = new CombatEngine(new ControlledRandom()).Resolve(hero, enemy, new(PlayerAction.Special));
        Assert.False(result.Success);
        Assert.Equal(hero.MaxHealth, hero.Health);
        Assert.Equal(enemy.MaxHealth, enemy.Health);
    }

    [Fact]
    public void KillingEnemy_SkipsRetaliationAndClampsHealthToZero()
    {
        var enemy = Enemy.Create("Fast Goblin");
        enemy.TakeDamage(59);
        var hero = new Warrior("Tester");
        new CombatEngine(new ControlledRandom()).Resolve(hero, enemy, new(PlayerAction.Attack));
        Assert.Equal(0, enemy.Health);
        Assert.Equal(150, hero.Health);
    }

    [Fact]
    public void DoubleShot_StopsWhenFirstArrowKillsEnemy()
    {
        var enemy = Enemy.Create("Fast Goblin");
        enemy.TakeDamage(59);
        var result = new CombatEngine(new ControlledRandom()).Resolve(new Archer("Tester"), enemy, new(PlayerAction.Special));
        Assert.Single(result.Events, e => e.Kind == BattleEventKind.Damage);
        Assert.Equal(0, enemy.Health);
    }

    [Fact]
    public void Fireball_IgnoresDefenseAndConsumesMana()
    {
        var hero = new Mage("Tester");
        var enemy = Enemy.Create("Strong Orc");
        new CombatEngine(new ControlledRandom()).Resolve(hero, enemy, new(PlayerAction.Special));
        Assert.Equal(90, enemy.Health); // Mage 25 ATK + 15, no armor
        Assert.Equal(70, hero.Mana);
    }

    [Fact]
    public void Potion_IsConsumedAndVitalsAreClamped()
    {
        var hero = new Warrior("Tester");
        hero.AddItem(Item.HealthPotion);
        hero.TakeDamage(10);
        Assert.True(hero.UseItem(0));
        Assert.Equal(150, hero.Health);
        Assert.Empty(hero.Inventory);
        Assert.False(hero.UseItem(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => hero.Heal(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => hero.RestoreMana(-1));
    }

    [Fact]
    public void Victory_OnlyAfterThreeEnemiesAndExperienceProgresses()
    {
        var hero = new Warrior("Tester");
        var enemies = Enumerable.Range(0, 3).Select(_ => Enemy.Create("Fast Goblin")).ToList();
        var game = new GameSession(hero, enemies, new ControlledRandom());
        for (int i = 0; i < 3; i++)
        {
            game.CurrentEnemy!.TakeDamage(59);
            Assert.True(game.Play(new(PlayerAction.Attack)).Success);
        }
        Assert.Equal(GameStatus.Victory, game.Status);
        Assert.Equal(2, hero.Level);
        Assert.Equal(20, hero.Experience);
        Assert.False(game.Play(new(PlayerAction.Attack)).Success);
    }

    [Fact]
    public void Death_EndsTheRunAndCannotBeHealed()
    {
        var hero = new Mage("Tester");
        var enemy = Enemy.Create("Strong Orc");
        var game = new GameSession(hero, [enemy, Enemy.Create("Fast Goblin"), Enemy.Create("Strong Orc")], new ControlledRandom());
        hero.TakeDamage(79);
        game.Play(new(PlayerAction.Defend));
        Assert.Equal(GameStatus.Defeat, game.Status);
        hero.Heal(50);
        Assert.Equal(0, hero.Health);
    }

    [Fact]
    public void Seed_MakesEnemyLineupRepeatable()
    {
        var left = GameFactory.NewGame("Mage", "A", new SeededRandomSource(123));
        var right = GameFactory.NewGame("Mage", "A", new SeededRandomSource(123));
        Assert.Equal(left.Enemies.Select(x => x.TypeId), right.Enemies.Select(x => x.TypeId));
    }

    [Fact]
    public void SaveFile_RoundTripsProgressAndRejectsMalformedData()
    {
        var game = GameFactory.NewGame("Archer", "Ranger", new ControlledRandom());
        game.Play(new(PlayerAction.Defend));
        string path = Path.Combine(Path.GetTempPath(), "pado-arena-" + Guid.NewGuid() + ".json");
        try
        {
            SaveGameStore.Save(path, game);
            GameSession loaded = SaveGameStore.Load(path, new ControlledRandom());
            Assert.Equal(game.Hero.Health, loaded.Hero.Health);
            Assert.Equal(game.Hero.ClassId, loaded.Hero.ClassId);
            Assert.Equal(game.Enemies.Select(x => x.TypeId), loaded.Enemies.Select(x => x.TypeId));
            File.WriteAllText(path, "{invalid-json");
            Assert.Throws<InvalidDataException>(() => SaveGameStore.Load(path, new ControlledRandom()));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void TamperedSave_RejectsNegativeHealth()
    {
        var game = GameFactory.NewGame("Warrior", "Tester", new ControlledRandom());
        var original = game.CaptureSave();
        var invalid = original with { Hero = original.Hero with { Health = -5 } };
        Assert.Throws<InvalidDataException>(() => GameSession.FromSave(invalid, new ControlledRandom()));
    }
}