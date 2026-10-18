namespace Pado.Arena.Core.Inventory;

public sealed record Item(string Name, int HealAmount, int ManaRestore, int AttackBoost, int DefenseBoost)
{
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 80 ||
            HealAmount is < 0 or > 1000 || ManaRestore is < 0 or > 1000 ||
            AttackBoost is < 0 or > 100 || DefenseBoost is < 0 or > 100 ||
            HealAmount + ManaRestore + AttackBoost + DefenseBoost == 0)
            throw new InvalidDataException("Invalid item data.");
    }

    public static Item HealthPotion => new("Health Potion", 50, 0, 0, 0);
    public static Item ManaPotion => new("Mana Potion", 0, 40, 0, 0);
    public static Item ElixirOfPower => new("Elixir of Power", 20, 20, 5, 0);
    public static Item SmallPotion => new("Small Potion", 30, 15, 0, 0);
}