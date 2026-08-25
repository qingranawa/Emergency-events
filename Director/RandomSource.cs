using System;

namespace EmergencyEvents.Director;

/// <summary>
/// Director 来源仲裁使用的可注入随机源。
/// </summary>
public interface IRandomSource
{
    double NextUnit();
}

/// <summary>
/// 正式运行使用的线程安全随机源。
/// </summary>
public sealed class ProductionRandomSource : IRandomSource
{
    private static readonly ProductionRandomSource shared = new ProductionRandomSource();
    private readonly Random random = new Random();

    public static IRandomSource Shared => shared;

    public double NextUnit()
    {
        lock (random)
        {
            return random.NextDouble();
        }
    }
}

/// <summary>
/// 测试用 Seed 随机源，保证相同 Seed 产生相同序列。
/// </summary>
public sealed class SeededRandomSource : IRandomSource
{
    private readonly Random random;

    public SeededRandomSource(int seed)
    {
        random = new Random(seed);
    }

    public double NextUnit()
    {
        return random.NextDouble();
    }
}

/// <summary>
/// 测试用固定随机源，用于精确验证边界和 FDI 方向。
/// </summary>
public sealed class DeterministicRandomSource : IRandomSource
{
    private readonly double value;

    public DeterministicRandomSource(double value)
    {
        this.value = value;
    }

    public double NextUnit()
    {
        return value;
    }
}
