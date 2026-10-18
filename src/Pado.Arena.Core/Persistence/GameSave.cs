using Pado.Arena.Core.Inventory;

namespace Pado.Arena.Core.Persistence;

public sealed record HeroSave(
    string ClassId, string Name, int MaxHealth, int Health, int MaxMana, int Mana,
    int Attack, int Defense, int Level, int Experience, bool IsDefending, List<Item> Inventory);

public sealed record EnemySave(string TypeId, int Health, int Mana);

public sealed record GameSave(int Version, int CurrentEnemyIndex, HeroSave Hero, List<EnemySave> Enemies, List<string> History);