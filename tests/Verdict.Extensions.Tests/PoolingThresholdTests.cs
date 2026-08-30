using System;
using System.Collections.Generic;
using System.Linq;
using Verdict.Extensions;
using Xunit;

namespace Verdict.Extensions.Tests;

/// <summary>
/// The pool only earns its keep in a band, and outside that band it costs more
/// than it saves.
/// </summary>
/// <remarks>
/// Measured on eight threads before this changed: pooled and disposed correctly
/// cost 88 bytes an operation, an ordinary array cost 96, and pooled with the
/// dispose forgotten cost 496 with six times the gen0 collections. Forgetting is
/// the default outcome, because the type is a struct that cannot be used with
/// <c>using</c> and usually arrives from a combinator rather than from the caller.
/// </remarks>
public class PoolingThresholdTests
{
    private static List<Error> Errors(int count) =>
        Enumerable.Range(0, count).Select(i => new Error($"E{i}", $"message {i}")).ToList();

    [Fact]
    public void ASmallCollectionIsNotPooled()
    {
        var collection = ErrorCollection.Create((IEnumerable<Error>)Errors(ErrorCollection.PoolingThreshold));

        collection.Dispose();

        // No rental means nothing to return, so disposing cannot invalidate it.
        Assert.False(collection.IsDisposed);
        Assert.Equal(ErrorCollection.PoolingThreshold, collection.Count);
    }

    [Fact]
    public void ASmallCollectionSurvivesASiblingBeingDisposed()
    {
        // The shape that made a derived MultiResult unreadable: a combinator hands
        // out a second result sharing the first one's collection.
        var original = ErrorCollection.Create((IEnumerable<Error>)Errors(3));
        var copy = original;

        copy.Dispose();

        Assert.Equal(3, original.Count);
        Assert.Equal("E0", original.First().Code);
    }

    [Fact]
    public void ACollectionJustOverTheThresholdIsPooled()
    {
        var collection = ErrorCollection.Create((IEnumerable<Error>)Errors(ErrorCollection.PoolingThreshold + 1));

        collection.Dispose();

        Assert.True(collection.IsDisposed);
    }

    [Fact]
    public void ACollectionOverTheCeilingIsNotPooledEither()
    {
        // ArrayPool.Shared keeps what it is given back, and past roughly 3,500
        // errors that array is on the large object heap and stays there.
        var count = ErrorCollection.PoolingCeiling + 1;
        var collection = ErrorCollection.Create((IEnumerable<Error>)Errors(count));

        collection.Dispose();

        Assert.False(collection.IsDisposed);
        Assert.Equal(count, collection.Count);
    }

    [Fact]
    public void NothingIsDroppedAtAnySize()
    {
        // The bound is on what goes into the pool, not on what the caller gets.
        foreach (var count in new[] { 1, 8, 9, 1024, 1025, 10_000 })
        {
            var collection = ErrorCollection.Create((IEnumerable<Error>)Errors(count));

            Assert.Equal(count, collection.Count);
            Assert.Equal("E0", collection.First().Code);
            Assert.Equal($"E{count - 1}", collection[count - 1].Code);

            collection.Dispose();
        }
    }

    [Fact]
    public void AnUnknownSizedSequenceIsAlsoKeptWhole()
    {
        // The growth path, where the count is not known before enumerating.
        static IEnumerable<Error> Stream(int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return new Error($"E{i}", $"message {i}");
            }
        }

        foreach (var count in new[] { 3, 9, 2000 })
        {
            var collection = ErrorCollection.Create(Stream(count));

            Assert.Equal(count, collection.Count);
            Assert.Equal($"E{count - 1}", collection[count - 1].Code);

            collection.Dispose();
        }
    }
}
