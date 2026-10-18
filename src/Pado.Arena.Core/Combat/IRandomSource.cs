namespace Pado.Arena.Core.Combat;

/// <summary>An injectable source of pseudo-random values for reproducible battles.</summary>
public interface IRandomSource
{
    int NextInt(int minInclusive, int maxExclusive);
    int NextPercent(); // 0..99
}

public sealed class SeededRandomSource(int? seed = null) : IRandomSource
{
    private readonly Random _random = seed is int value ? new Random(value) : new Random();
    public int NextInt(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
    public int NextPercent() => _random.Next(0, 100);
}