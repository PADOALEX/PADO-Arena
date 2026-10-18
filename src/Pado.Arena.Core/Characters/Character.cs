namespace Pado.Arena.Core.Characters;

public abstract class Character
{
    protected Character(string name, int health, int mana, int attack, int defense, int dodgeChance, int critChance)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 80) throw new ArgumentException("Name must be 1-80 characters.", nameof(name));
        if (health <= 0 || mana < 0 || attack <= 0 || defense < 0) throw new ArgumentOutOfRangeException(nameof(health), "Invalid base stats.");
        if (dodgeChance is < 0 or > 100 || critChance is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(dodgeChance), "Percent must be 0-100.");
        Name = name;
        MaxHealth = Health = health;
        MaxMana = Mana = mana;
        Attack = attack;
        Defense = defense;
        DodgeChance = dodgeChance;
        CritChance = critChance;
    }

    public string Name { get; }
    public int MaxHealth { get; protected set; }
    public int Health { get; private set; }
    public int MaxMana { get; protected set; }
    public int Mana { get; private set; }
    public int Attack { get; protected set; }
    public int Defense { get; protected set; }
    public int DodgeChance { get; }
    public int CritChance { get; }
    public bool IsAlive => Health > 0;

    public void TakeDamage(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Health = Math.Max(0, Health - amount);
    }

    public void Heal(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        if (IsAlive) Health = (int)Math.Min(MaxHealth, (long)Health + amount);
    }

    public void RestoreMana(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Mana = (int)Math.Min(MaxMana, (long)Mana + amount);
    }

    public bool TrySpendMana(int cost)
    {
        if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
        if (Mana < cost) return false;
        Mana -= cost;
        return true;
    }

    public void BoostStats(int attack, int defense)
    {
        if (attack < 0 || defense < 0) throw new ArgumentOutOfRangeException(nameof(attack));
        Attack = checked(Attack + attack);
        Defense = checked(Defense + defense);
    }

    internal void RestoreVitals(int maxHealth, int health, int maxMana, int mana, int attack, int defense)
    {
        if (maxHealth is < 1 or > 100_000 || health < 0 || health > maxHealth ||
            maxMana is < 0 or > 100_000 || mana < 0 || mana > maxMana ||
            attack is < 1 or > 100_000 || defense is < 0 or > 100_000)
            throw new InvalidDataException("Invalid character stats in save file.");
        MaxHealth = maxHealth;
        Health = health;
        MaxMana = maxMana;
        Mana = mana;
        Attack = attack;
        Defense = defense;
    }
}