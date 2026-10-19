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
    private PinnedSet<object, object> _marked = null!;
    private object[] _probes = null!;
    private object[] _defProbes = null!;
    private List<PinTarget<object, object>> _targets = null!;
    private int _next;

    [GlobalSetup]
    public void Setup()
    {
        _board = SyntheticCatalog.Pins();
        _lines = PinSerializer.ToLines(_board);
        _toggleBoard = SyntheticCatalog.Pins(SyntheticCatalog.PinCount - 1);
        _toggleBoard.Capacity = SyntheticCatalog.PinCount;
        _defName = "Bench_Def_Name_123456";
        _key = PinKey.Build(PinKeyKind.BuildThing, "Bench_Toggle_Target");
        _targets = new List<PinTarget<object, object>>(SyntheticCatalog.PinCount);
        _probes = new object[SyntheticCatalog.PinCount * 2];
        _defProbes = new object[SyntheticCatalog.PinCount * 2];
        for (int i = 0; i < SyntheticCatalog.PinCount; i++)
        {
            _probes[i] = new object();
            _defProbes[i] = new object();
            _targets.Add(new PinTarget<object, object>(_probes[i], null, _defProbes[i]));
        }

        for (int i = SyntheticCatalog.PinCount; i < _probes.Length; i++)
        {
            _probes[i] = new object();
            _defProbes[i] = new object();
        }

        _marked = new PinnedSet<object, object>();
        _marked.Rebuild(_targets);
    }

    [Benchmark]
    public PinResult PinBoard_Toggle() => _toggleBoard.Toggle(_key, "Bench label", "author.mod");

    [Benchmark]
    public PinKey PinKey_Build() => PinKey.Build(PinKeyKind.BuildThing, _defName);

    [Benchmark]
    public PinLoadResult FromLines_200() => PinSerializer.FromLines(_lines);

    [Benchmark]
    public List<string> ToLines_200() => PinSerializer.ToLines(_board);

    /// <summary>P-19: one marker lookup (members and non-members alternate), 200 pins.</summary>
    [Benchmark]
    public PinMark PinnedSetContains()
    {
        int i = _next++ % _probes.Length;
        return _marked.Lookup(_probes[i], _defProbes[i]);
    }

    [Benchmark]
    public void PinnedSet_Rebuild_200() => _marked.Rebuild(_targets);
}
