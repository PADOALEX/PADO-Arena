namespace Pado.Arena.Core.Combat;

public enum BattleEventKind { Information, Attack, Dodge, Critical, Damage, Defense, Mana, Item, Defeat, Victory, Progress }
public sealed record BattleEvent(BattleEventKind Kind, string Message);
public enum PlayerAction { Attack, Defend, Special, Item }
public sealed record PlayerCommand(PlayerAction Action, int ItemIndex = -1);
public sealed record TurnResult(bool Success, IReadOnlyList<BattleEvent> Events);
public sealed record SpecialAbility(string Name, int ManaCost, int Multiplier, int FlatBonus, int Hits, bool IgnoreDefense, bool CanCrit);