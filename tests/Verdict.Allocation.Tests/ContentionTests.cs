using System;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Verdict.Fluent;
using Xunit;

namespace Verdict.Allocation.Tests;

/// <summary>
/// Marks the tests that must not run beside anything else, because they count
/// collections and another test allocating in the same process moves the count.
/// </summary>
[CollectionDefinition("gc", DisableParallelization = true)]
public class GcCollectionDefinition
{
}

/// <summary>
/// A per-call byte count says nothing about contention or about GC pressure
/// across generations, which is what a heavy workload actually means. This runs
/// the core composition chain on every core and asserts the collector never runs.
/// </summary>
[Collection("gc")]
public class ContentionTests
{
    private const int PerThread = 200_000;

    private static readonly Func<int, int> Double = static x => x * 2;
    private static readonly Func<Error, int> OnError = static _ => -1;

    private static long Chain(int iterations)
    {
        long accumulator = 0;
        for (var i = 0; i < iterations; i++)
        {
            accumulator += Result<int>.Success(i).Map(Double).Match(Double, OnError);
        }
        return accumulator;
    }

    /// <summary>
    /// The hard assertion. Each worker measures its own thread, so another test
    /// allocating elsewhere in the process cannot make this pass or fail.
    /// </summary>
    [Fact]
    public void SustainedConcurrentUseAllocatesNothingOnAnyThread()
    {
        var threads = Environment.ProcessorCount;
        var perThreadBytes = new long[threads];

        Parallel.For(0, threads, i =>
        {
            Chain(1_000);

            var before = GC.GetAllocatedBytesForCurrentThread();
            AllocationHarness.Sink = Chain(PerThread) == long.MinValue ? new object() : null;
            perThreadBytes[i] = GC.GetAllocatedBytesForCurrentThread() - before;
        });

        for (var i = 0; i < threads; i++)
        {
            Assert.True(
                perThreadBytes[i] == 0,
                $"Worker {i} allocated {perThreadBytes[i]:N0} bytes across {PerThread:N0} composed "
                + "operations. The core is meant to be free under sustained concurrent use, which is "
                + "the claim a capacity planner actually buys.");
        }
    }

    /// <summary>
    /// The claim in the form a reader cares about: the collector never runs.
    /// </summary>
    /// <remarks>
    /// Softer than the per-thread assertion above because it is process-wide, so
    /// it lives in a non-parallel collection. If this ever goes flaky while the
    /// per-thread test stays green, the cause is another test in this process and
    /// not the library.
    /// </remarks>
    [Fact]
    public void SustainedConcurrentUseDoesNotCollect()
    {
        var threads = Environment.ProcessorCount;

        Parallel.For(0, threads, _ => Chain(1_000));
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

        var gen0 = GC.CollectionCount(0);
        var gen1 = GC.CollectionCount(1);
        var gen2 = GC.CollectionCount(2);

        Parallel.For(0, threads, _ => AllocationHarness.Sink = Chain(PerThread) == long.MinValue ? new object() : null);

        Assert.Equal(gen0, GC.CollectionCount(0));
        Assert.Equal(gen1, GC.CollectionCount(1));
        Assert.Equal(gen2, GC.CollectionCount(2));
    }

    /// <summary>
    /// Server GC is a runtime setting rather than a source one, so the csproj
    /// property is part of the gate. If it stops applying, the two tests above
    /// keep passing while measuring something other than what they claim.
    /// </summary>
    [Fact]
    public void ServerGarbageCollectionIsActuallyEnabled()
    {
        // The runtime forces workstation GC on a single-core host whatever the
        // csproj says, so on one of those this would fail for a reason that has
        // nothing to do with the library. Assert it where it can be true.
        if (Environment.ProcessorCount < 2)
        {
            Assert.False(GCSettings.IsServerGC, "Single core hosts run workstation GC.");
            return;
        }

        Assert.True(
            GCSettings.IsServerGC,
            "ServerGarbageCollection is set in Verdict.Allocation.Tests.csproj but is not in effect. "
            + "The contention numbers were measured under server GC, so without it these tests "
            + "measure a different collector than the one they document.");
    }
}
