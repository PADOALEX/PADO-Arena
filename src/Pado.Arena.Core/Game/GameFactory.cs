using Pado.Arena.Core.Characters;
using Pado.Arena.Core.Combat;
using Pado.Arena.Core.Inventory;

namespace Pado.Arena.Core.Game;

public static class GameFactory
{
    private static readonly string[] EnemyTypes = ["Fast Goblin", "Balanced Skeleton", "Strong Orc"];

    public static Hero CreateHero(string classId, string name) => classId switch
    {
        "Warrior" => new Warrior(name),
        "Mage" => new Mage(name),
        "Archer" => new Archer(name),
        _ => throw new InvalidDataException("Unknown hero class.")
    };

    public static GameSession NewGame(string classId, string name, IRandomSource random)
    {
        ArgumentNullException.ThrowIfNull(random);
        Hero hero = CreateHero(classId, name);
        hero.AddItem(Item.HealthPotion);
        hero.AddItem(Item.ManaPotion);
        hero.AddItem(Item.ElixirOfPower);
        List<Enemy> enemies = [];
        for (int i = 0; i < 3; i++) enemies.Add(Enemy.Create(EnemyTypes[random.NextInt(0, 3)]));
        return new GameSession(hero, enemies, random);
    }
}