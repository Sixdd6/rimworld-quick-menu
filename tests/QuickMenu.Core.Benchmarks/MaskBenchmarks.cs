using BenchmarkDotNet.Attributes;

using QuickMenu.Core.Render;

namespace QuickMenu.Benchmarks;

/// <summary>Whole mask set render (design.md section 6 budget: 5 ms, once at startup).</summary>
[MemoryDiagnoser]
public class MaskBenchmarks
{
    private byte[] _buffer = null!;

    [GlobalSetup]
    public void Setup() => _buffer = new byte[MaskSet.MaxByteCount];

    [Benchmark]
    public byte RenderWholeSet()
    {
        for (int i = 0; i < MaskSet.Count; i++)
        {
            MaskSet.Render((MaskId)i, _buffer);
        }

        return _buffer[0];
    }
}
