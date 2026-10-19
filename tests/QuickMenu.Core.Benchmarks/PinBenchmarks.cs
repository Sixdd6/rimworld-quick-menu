using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

using QuickMenu.Core.Pins;
using QuickMenu.TestData;

namespace QuickMenu.Benchmarks;

/// <summary>P-5 (toggle), P-16 (parse 200 pins), P-17 (save 200 pins, pure part).</summary>
[MemoryDiagnoser]
public class PinBenchmarks
{
    private PinBoard _board = null!;
    private PinBoard _toggleBoard = null!;
    private List<string> _lines = null!;
    private PinKey _key = null!;
    private string _defName = null!;

    [GlobalSetup]
    public void Setup()
    {
        _board = SyntheticCatalog.Pins();
        _lines = PinSerializer.ToLines(_board);
        _toggleBoard = SyntheticCatalog.Pins(SyntheticCatalog.PinCount - 1);
        _toggleBoard.Capacity = SyntheticCatalog.PinCount;
        _defName = "Bench_Def_Name_123456";
        _key = PinKey.Build(PinKeyKind.BuildThing, "Bench_Toggle_Target");
    }

    [Benchmark]
    public PinResult PinBoard_Toggle() => _toggleBoard.Toggle(_key, "Bench label", "author.mod");

    [Benchmark]
    public PinKey PinKey_Build() => PinKey.Build(PinKeyKind.BuildThing, _defName);

    [Benchmark]
    public PinLoadResult FromLines_200() => PinSerializer.FromLines(_lines);

    [Benchmark]
    public List<string> ToLines_200() => PinSerializer.ToLines(_board);
}
