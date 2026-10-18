namespace Pado.Arena.Core.Characters;

public sealed class Enemy : Character
{
    private Enemy(string type, int hp, int mp, int attack, int defense, int dodge, int crit, int experience)
        : base(type, hp, mp, attack, defense, dodge, crit)
    {
        TypeId = type;
        ExperienceReward = experience;
    }

    public string TypeId { get; }
    public int ExperienceReward { get; }

    public static Enemy Create(string type) => type switch
    {
        "Fast Goblin" => new(type, 60, 20, 12, 2, 25, 15, 40),
        "Balanced Skeleton" => new(type, 90, 30, 15, 5, 10, 10, 70),
        "Strong Orc" => new(type, 130, 50, 20, 8, 5, 20, 100),
        _ => throw new InvalidDataException($"Unknown enemy type: {type}")
    };

    public void Load(int health, int mana)
    {
        RestoreVitals(MaxHealth, health, MaxMana, mana, Attack, Defense);
    }
}