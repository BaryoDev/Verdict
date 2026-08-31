using System;
using Xunit;

namespace Verdict.Allocation.Tests;

/// <summary>
/// Measures what an operation allocates, and nothing else.
/// </summary>
/// <remarks>
/// Three things here are load bearing rather than incidental, and removing any
/// of them turns this into a test that cannot fail:
/// <list type="bullet">
/// <item>The delegate is created by the caller before the baseline is read, so
/// the closure the caller had to allocate is not charged to the library.</item>
/// <item>A warm-up call runs first, so JIT compilation is not measured.</item>
/// <item>Every input is hoisted out of the measured loop for the same reason.
/// A test that constructs its inputs inside the loop measures the test.</item>
/// </list>
/// </remarks>
public static class AllocationHarness
{
    // Stops the JIT eliding work whose result is never observed.
    public static volatile object? Sink;

    // Ten thousand rather than a thousand because the harness itself has a small
    // fixed cost, and dividing it by ten thousand keeps it under a tenth of a byte
    // per call. At a thousand it showed up as half a byte and pushed exact budgets over.
    public const int Iterations = 10_000;

    public static double BytesPerCall(Action action)
    {
        action();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < Iterations; i++)
        {
            action();
        }

        return (double)(GC.GetAllocatedBytesForCurrentThread() - before) / Iterations;
    }

    /// <summary>
    /// Asserts an operation allocates nothing at all.
    /// </summary>
    public static void AllocatesNothing(string name, Action action)
    {
        var measured = BytesPerCall(action);

        Assert.True(
            measured == 0,
            $"{name} allocated {measured:N2} bytes per call, expected 0. "
            + "Zero allocation on this path is the reason the library exists, so this is a design "
            + "regression rather than a slow test. Look for boxing, a captured variable, an "
            + "interface return, or a params array.");
    }

    /// <summary>
    /// Asserts an operation stays inside a named byte budget.
    /// </summary>
    /// <remarks>
    /// A budget is not a target. Changing one of these numbers is a deliberate
    /// edit that shows up in review, which is the point: an operation that
    /// quietly doubles its allocation is the failure this catches.
    /// </remarks>
    public static void WithinBudget(string name, int maxBytes, Action action)
    {
        var measured = BytesPerCall(action);

        Assert.True(
            measured <= maxBytes,
            $"{name} allocated {measured:N2} bytes per call, over its {maxBytes} byte budget. "
            + "If the increase is intended, change the budget in the same commit so the cost is "
            + "visible in review.");
    }
}
