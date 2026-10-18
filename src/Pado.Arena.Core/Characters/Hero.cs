using Pado.Arena.Core.Inventory;
using Pado.Arena.Core.Combat;
using Pado.Arena.Core.Persistence;

namespace Pado.Arena.Core.Characters;

public abstract class Hero : Character
{
    protected Hero(string name, int health, int mana, int attack, int defense, int dodgeChance, int critChance)
        : base(name, health, mana, attack, defense, dodgeChance, critChance) { }

    private readonly List<Item> _inventory = [];
    public IReadOnlyList<Item> Inventory => _inventory.AsReadOnly();
    public bool IsDefending { get; private set; }
    public int Level { get; private set; } = 1;
    public int Experience { get; private set; }
    public abstract string ClassId { get; }
    public abstract SpecialAbility Special { get; }

    public void Defend() => IsDefending = true;
    public void ClearDefense() => IsDefending = false;
    public void AddItem(Item item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.Validate();
        if (_inventory.Count >= 30) throw new InvalidOperationException("Inventory is full.");
        _inventory.Add(item);
    }

    public bool UseItem(int index)
    {
        if (index < 0 || index >= _inventory.Count || !IsAlive) return false;
        Item item = _inventory[index];
        Heal(item.HealAmount);
        RestoreMana(item.ManaRestore);
        BoostStats(item.AttackBoost, item.DefenseBoost);
        _inventory.RemoveAt(index);
        return true;
    }

    public int GainExperience(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Experience = checked(Experience + amount);
        int levelUps = 0;
        while (Level < 50 && Experience >= Level * 100)
        {
            Experience -= Level * 100;
            Level++;
            levelUps++;
            int newHealth = MaxHealth + 15;
            int newMana = MaxMana + 10;
            RestoreVitals(newHealth, Math.Min(newHealth, Health + 15), newMana,
                Math.Min(newMana, Mana + 10), Attack + 2, Defense + 1);
        }
        if (Level == 50) Experience = Math.Min(Experience, Level * 100 - 1);
        return levelUps;
    }

    internal void Load(HeroSave state)
    {
        if (state.Level is < 1 or > 50 || state.Experience < 0 || state.Experience >= state.Level * 100 ||
            state.MaxHealth != MaxHealth + (state.Level - 1) * 15 ||
            state.MaxMana != MaxMana + (state.Level - 1) * 10 ||
            state.Attack < Attack + (state.Level - 1) * 2 ||
            state.Defense < Defense + (state.Level - 1) ||
            state.IsDefending || state.Inventory is null || state.Inventory.Count > 30)
            throw new InvalidDataException("Invalid hero progression in save file.");
        Level = state.Level;
        Experience = state.Experience;
        RestoreVitals(state.MaxHealth, state.Health, state.MaxMana, state.Mana, state.Attack, state.Defense);
        _inventory.Clear();
        foreach (Item item in state.Inventory)
        {
            if (item is null) throw new InvalidDataException("Invalid inventory entry.");
            item.Validate();
            _inventory.Add(item);
        }
        IsDefending = state.IsDefending;
    }
}

public sealed class Warrior(string name) : Hero(name, 150, 40, 15, 8, 5, 10)
{
    public override string ClassId => "Warrior";
    public override SpecialAbility Special => new("Heavy Strike", 20, 2, 0, 1, false, true);
}

public sealed class Mage(string name) : Hero(name, 80, 100, 25, 3, 10, 5)
{
    public override string ClassId => "Mage";
    public override SpecialAbility Special => new("Fireball", 30, 1, 15, 1, true, false);
}

public sealed class Archer(string name) : Hero(name, 100, 60, 18, 5, 20, 25)
{
    public override string ClassId => "Archer";
    public override SpecialAbility Special => new("Double Shot", 25, 1, 0, 2, false, true);
}