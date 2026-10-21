using System;
using System.Linq;
using QuickMenu.Core;
using Xunit;

namespace QuickMenu.Core.Tests;

/// <summary>Architecture rule (CLAUDE.md): Core must not reference any game or Unity assembly (NFR-8).</summary>
public sealed class CoreIsolationTests
{
    [Fact]
    public void CoreAssembly_ReferencesOnlyTheBaseClassLibrary()
    {
        var core = typeof(BuildInfo).Assembly;
        var names = core.GetReferencedAssemblies().Select(a => a.Name ?? string.Empty).ToArray();

        Assert.NotEmpty(names);
        foreach (var name in names)
        {
            var allowed = name == "netstandard" || name == "mscorlib"
                || name.StartsWith("System", StringComparison.Ordinal)
                || name.StartsWith("Microsoft.", StringComparison.Ordinal);
            Assert.True(allowed, $"QuickMenu.Core references non-BCL assembly '{name}'");
        }

        Assert.DoesNotContain(names, n =>
            n.StartsWith("UnityEngine", StringComparison.Ordinal)
            || n is "Assembly-CSharp" or "UnityStubs" or "Waylith" or "0Harmony");
    }
}
