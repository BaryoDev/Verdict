using System;
using System.Collections.Generic;
using Xunit;

namespace Verdict.Tests;

/// <summary>
/// Without IEquatable and explicit overrides, Result falls through to
/// ValueType.Equals, which compares by reflection and boxes. That allocates on
/// every comparison, which contradicts the zero-allocation guarantee whenever a
/// Result is used in a set, as a dictionary key, or with Contains/Distinct.
/// </summary>
public class ResultEqualityTests
{
    [Fact]
    public void ResultT_ImplementsIEquatable()
    {
        Assert.True(typeof(IEquatable<Result<int>>).IsAssignableFrom(typeof(Result<int>)));
    }

    [Fact]
    public void Result_ImplementsIEquatable()
    {
        Assert.True(typeof(IEquatable<Result>).IsAssignableFrom(typeof(Result)));
    }

    [Fact]
    public void ResultT_OverridesEqualsAndGetHashCode()
    {
        Assert.NotEqual(typeof(ValueType), typeof(Result<int>).GetMethod("Equals", new[] { typeof(object) })!.DeclaringType);
        Assert.NotEqual(typeof(ValueType), typeof(Result<int>).GetMethod("GetHashCode")!.DeclaringType);
    }

    [Fact]
    public void Result_OverridesEqualsAndGetHashCode()
    {
        Assert.NotEqual(typeof(ValueType), typeof(Result).GetMethod("Equals", new[] { typeof(object) })!.DeclaringType);
        Assert.NotEqual(typeof(ValueType), typeof(Result).GetMethod("GetHashCode")!.DeclaringType);
    }

    [Fact]
    public void Successes_WithSameValue_AreEqual()
    {
        Assert.True(Result<int>.Success(42).Equals(Result<int>.Success(42)));
        Assert.True(Result<int>.Success(42) == Result<int>.Success(42));
        Assert.False(Result<int>.Success(42) != Result<int>.Success(42));
    }

    [Fact]
    public void Successes_WithDifferentValues_AreNotEqual()
    {
        Assert.False(Result<int>.Success(1).Equals(Result<int>.Success(2)));
        Assert.True(Result<int>.Success(1) != Result<int>.Success(2));
    }

    [Fact]
    public void SuccessAndFailure_AreNeverEqual()
    {
        Assert.False(Result<int>.Success(0).Equals(Result<int>.Failure("E", "m")));
    }

    [Fact]
    public void Failures_WithSameError_AreEqual()
    {
        Assert.True(Result<int>.Failure("E", "m").Equals(Result<int>.Failure("E", "m")));
    }

    [Fact]
    public void ReferenceTypeSuccess_UsesValueEquality_NotReferenceEquality()
    {
        Assert.True(Result<string>.Success("abc").Equals(Result<string>.Success(new string("abc".ToCharArray()))));
    }

    [Fact]
    public void NullValueSuccess_DoesNotThrow()
    {
        var a = Result<string?>.Success(null);
        var b = Result<string?>.Success(null);

        Assert.True(a.Equals(b));
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void EqualResults_ShareAHashCode()
    {
        Assert.Equal(Result<int>.Success(7).GetHashCode(), Result<int>.Success(7).GetHashCode());
        Assert.Equal(Result<int>.Failure("E", "m").GetHashCode(), Result<int>.Failure("E", "m").GetHashCode());
    }

    [Fact]
    public void WorksAsADictionaryKeyAndInASet()
    {
        var set = new HashSet<Result<int>> { Result<int>.Success(1), Result<int>.Success(1), Result<int>.Success(2) };

        Assert.Equal(2, set.Count);
        Assert.Contains(Result<int>.Success(1), set);
    }

    [Fact]
    public void Equals_DoesNotAllocate()
    {
        var a = Result<int>.Success(1);
        var b = Result<int>.Success(1);

        // Warm up so JIT and any first-call setup are not counted.
        for (var i = 0; i < 100; i++) { _ = a.Equals(b); }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1_000; i++) { _ = a.Equals(b); }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0, allocated);
    }

    [Fact]
    public void GetHashCode_DoesNotAllocate()
    {
        var a = Result<int>.Success(1);
        for (var i = 0; i < 100; i++) { _ = a.GetHashCode(); }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1_000; i++) { _ = a.GetHashCode(); }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void SetOperations_DoNotAllocatePerComparison()
    {
        var list = new List<Result<int>> { Result<int>.Success(1) };
        var needle = Result<int>.Success(1);
        for (var i = 0; i < 100; i++) { _ = list.Contains(needle); }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1_000; i++) { _ = list.Contains(needle); }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }
}
