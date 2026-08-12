using System;
using Xunit;

namespace Verdict.Tests;

/// <summary>
/// Gates the promise the library exists for.
///
/// "Zero allocation on the success path" is the reason to choose Verdict over a class-based
/// Result, and until now it was a claim in the README rather than something the build enforced.
/// A contributor can break it without breaking a single behavioural test: add a field that
/// boxes, capture a variable in a lambda, return an interface instead of the struct, and every
/// other test still passes while the benchmark quietly regresses.
///
/// These assert the allocation count directly, so that change fails here instead of in
/// somebody's production GC graph.
/// </summary>
public class AllocationTests
{
    // Prevents the JIT from eliding work whose result is never observed.
    private static volatile object? _sink;

    private const int Iterations = 1_000;

    [Fact]
    public void SuccessAllocatesNothing()
    {
        AssertNoAllocations(() =>
        {
            var result = Result<int>.Success(42);
            _sink = result.IsSuccess ? null : _sink;
        });
    }

    [Fact]
    public void ImplicitConversionFromValueAllocatesNothing()
    {
        AssertNoAllocations(() =>
        {
            Result<int> result = 42;
            _sink = result.IsSuccess ? null : _sink;
        });
    }

    [Fact]
    public void NonGenericSuccessAllocatesNothing()
    {
        AssertNoAllocations(() =>
        {
            var result = Result.Success();
            _sink = result.IsSuccess ? null : _sink;
        });
    }

    [Fact]
    public void ReadingValueAllocatesNothing()
    {
        var result = Result<int>.Success(42);

        AssertNoAllocations(() =>
        {
            var value = result.Value;
            _sink = value == 0 ? _sink : null;
        });
    }

    [Fact]
    public void FailureAllocatesNothing()
    {
        // Error is a readonly record struct, so constructing a failure should stay on the stack
        // too. The string literals are interned and allocated once by the runtime, not per call.
        var error = new Error("NOT_FOUND", "missing");

        AssertNoAllocations(() =>
        {
            var result = Result<int>.Failure(error);
            _sink = result.IsFailure ? null : _sink;
        });
    }

    [Fact]
    public void ReadingErrorAllocatesNothing()
    {
        var result = Result<int>.Failure(new Error("NOT_FOUND", "missing"));

        AssertNoAllocations(() =>
        {
            var error = result.Error;
            _sink = error.Code is null ? _sink : null;
        });
    }

    /// <summary>
    /// Runs <paramref name="action" /> a thousand times and asserts the thread allocated nothing.
    /// </summary>
    /// <remarks>
    /// The delegate is allocated by the caller before the baseline is read, so the closure itself
    /// is not counted. One warm-up call runs first so JIT compilation is not measured either.
    /// </remarks>
    private static void AssertNoAllocations(Action action)
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

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(
            allocated == 0,
            $"Expected 0 bytes allocated across {Iterations:N0} iterations, measured {allocated:N0} bytes "
            + $"({(double)allocated / Iterations:N2} per call). The zero-allocation success path is the "
            + "reason this library exists, so this is a design regression rather than a slow test. "
            + "Look for boxing, a captured variable, an interface return, or a params array.");
    }
}
