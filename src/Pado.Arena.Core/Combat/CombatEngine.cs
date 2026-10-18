using Pado.Arena.Core.Characters;

namespace Pado.Arena.Core.Combat;

/// <summary>One atomic player turn, followed by an enemy turn only if both fighters survive.</summary>
public sealed class CombatEngine(IRandomSource random)
{
    private readonly IRandomSource _random = random ?? throw new ArgumentNullException(nameof(random));

    public TurnResult Resolve(Hero hero, Enemy enemy, PlayerCommand command)
    {
        ArgumentNullException.ThrowIfNull(hero);
        ArgumentNullException.ThrowIfNull(enemy);
        ArgumentNullException.ThrowIfNull(command);
        List<BattleEvent> events = [];
        if (!hero.IsAlive || !enemy.IsAlive)
            return new(false, [new(BattleEventKind.Information, "This battle has already ended.")]);
        if (!Enum.IsDefined(command.Action))
            return new(false, [new(BattleEventKind.Information, "Unknown action.")]);
        if (command.Action == PlayerAction.Special && hero.Mana < hero.Special.ManaCost)
            return new(false, [new(BattleEventKind.Information, $"Not enough mana for {hero.Special.Name} ({hero.Special.ManaCost} MP).")]);
        if (command.Action == PlayerAction.Item && (command.ItemIndex < 0 || command.ItemIndex >= hero.Inventory.Count))
            return new(false, [new(BattleEventKind.Information, "Select a valid inventory item.")]);

        switch (command.Action)
        {
            case PlayerAction.Attack:
                Strike(hero, enemy, hero.Attack, false, true, events);
                hero.RestoreMana(5);
                break;
            case PlayerAction.Defend:
                hero.Defend();
                hero.RestoreMana(15);
                events.Add(new(BattleEventKind.Defense, $"{hero.Name} guards against the next enemy action and recovers 15 MP."));
                break;
            case PlayerAction.Special:
                SpecialAbility skill = hero.Special;
                hero.TrySpendMana(skill.ManaCost);
                events.Add(new(BattleEventKind.Attack, $"{hero.Name} uses {skill.Name} (-{skill.ManaCost} MP)."));
                for (int i = 0; i < skill.Hits && enemy.IsAlive; i++)
                    Strike(hero, enemy, checked(hero.Attack * skill.Multiplier + skill.FlatBonus), skill.IgnoreDefense, skill.CanCrit, events);
                break;
            case PlayerAction.Item:
                string itemName = hero.Inventory[command.ItemIndex].Name;
                hero.UseItem(command.ItemIndex);
                events.Add(new(BattleEventKind.Item, $"{hero.Name} uses {itemName}."));
                break;
        }

        if (enemy.IsAlive)
            EnemyTurn(enemy, hero, events);
        hero.ClearDefense(); // Guard never leaks into another turn, even when the attack misses.
        if (!hero.IsAlive) events.Add(new(BattleEventKind.Defeat, $"{hero.Name} has fallen."));
        return new(true, events);
    }

    private void EnemyTurn(Enemy enemy, Hero hero, List<BattleEvent> events)
    {
        enemy.RestoreMana(5);
        int decision = _random.NextInt(1, 4);
        if (decision == 3 && enemy.TrySpendMana(25))
        {
            events.Add(new(BattleEventKind.Attack, $"{enemy.Name} uses Brutal Attack (-25 MP)."));
            Strike(enemy, hero, enemy.Attack + 10, false, true, events);
        }
        else Strike(enemy, hero, enemy.Attack, false, true, events);
    }

    private void Strike(Character attacker, Character target, int baseDamage, bool ignoreDefense, bool canCrit, List<BattleEvent> events)
    {
        if (!attacker.IsAlive || !target.IsAlive) return;
        if (_random.NextPercent() < target.DodgeChance)
        {
            events.Add(new(BattleEventKind.Dodge, $"{target.Name} dodges {attacker.Name}'s attack!"));
            return;
        }
        bool critical = canCrit && _random.NextPercent() < attacker.CritChance;
        int beforeDefense = checked(baseDamage * (critical ? 2 : 1));
        int damage = ignoreDefense ? beforeDefense : Math.Max(1, beforeDefense - target.Defense);
        if (target is Hero hero && hero.IsDefending)
            damage = Math.Max(1, (damage + 1) / 2);
        int actualDamage = Math.Min(target.Health, damage);
        target.TakeDamage(damage);
        events.Add(new(BattleEventKind.Damage,
            $"{attacker.Name} hits {target.Name} for {actualDamage} damage{(critical ? " (CRITICAL!)" : "")}{(ignoreDefense ? " [ignores armor]" : "")}."));
        if (critical) events.Add(new(BattleEventKind.Critical, "Critical hit!"));
    }
}