using System;
using System.Collections.Generic;
using Verdict.Extensions;
using Verdict.Async;
using Verdict.Fluent;
using Verdict.Rich;
using Xunit;

namespace Verdict.Allocation.Tests;

/// <summary>
/// The operations that must allocate nothing, ever. This list may grow and must
/// not shrink: a row moved out of here is the library changing what it promises.
/// </summary>
public class ZeroAllocationTests
{
    private sealed class Payload
    {
        public int Id { get; init; }
    }

    private static readonly Error Err = new("NOT_FOUND", "missing");
    private static readonly Payload Obj = new() { Id = 7 };
    private static readonly Result<int> OkInt = Result<int>.Success(42);
    private static readonly Result<string> OkStr = Result<string>.Success("hello");
    private static readonly Result<int> FailInt = Result<int>.Failure(Err);
    private static readonly Result OkUnit = Result.Success();

    private static readonly Func<int, int> Double = static x => x * 2;
    private static readonly Func<int, string> ToText = static _ => "n";
    private static readonly Func<int, Result<int>> Bind = static x => Result<int>.Success(x + 1);
    private static readonly Func<Error, int> OnError = static _ => -1;
    private static readonly Action<int> NoOp = static _ => { };
    private static readonly Action<Error> NoOpError = static _ => { };
    private static readonly Func<int, bool> IsPositive = static x => x > 0;
    private static readonly Func<int> NotThrowing = static () => 1;

    public static TheoryData<string, Action> MustAllocateNothing() => new()
    {
        // ---- Verdict, the core ----
        { "Result<int>.Success", static () => { var r = Result<int>.Success(42); Keep(r.IsSuccess); } },
        { "Result<string>.Success", static () => { var r = Result<string>.Success("hello"); Keep(r.IsSuccess); } },
        { "Result<Payload>.Success, reference-typed T", static () => { var r = Result<Payload>.Success(Obj); Keep(r.IsSuccess); } },
        { "Result<(int,string)>.Success", static () => { var r = Result<(int, string)>.Success((1, "a")); Keep(r.IsSuccess); } },
        { "Result<int>.Failure(error)", static () => { var r = Result<int>.Failure(Err); Keep(r.IsFailure); } },
        { "Result<int>.Failure(code, message)", static () => { var r = Result<int>.Failure("E", "m"); Keep(r.IsFailure); } },
        { "Result.Success, non-generic", static () => { var r = Result.Success(); Keep(r.IsSuccess); } },
        { "Result.Failure(error), non-generic", static () => { var r = Result.Failure(Err); Keep(r.IsFailure); } },
        { "result.Value", static () => Keep(OkInt.Value == 0) },
        { "result.Error", static () => Keep(OkIntErrorCode()) },
        { "result.ValueOr(fallback)", static () => Keep(FailInt.ValueOr(0) == 0) },
        { "result.ValueOrDefault", static () => Keep(OkInt.ValueOrDefault == 0) },
        { "result.Equals", static () => Keep(OkInt.Equals(OkInt)) },
        { "result.GetHashCode, value T", static () => Keep(OkInt.GetHashCode() == 0) },
        { "result.GetHashCode, reference T", static () => Keep(OkStr.GetHashCode() == 0) },
        { "implicit Result<int> from value", static () => { Result<int> r = 42; Keep(r.IsSuccess); } },
        { "implicit Result<int> from error", static () => { Result<int> r = Err; Keep(r.IsFailure); } },
        { "result.Bind", static () => { var r = OkInt.Bind(Bind); Keep(r.IsSuccess); } },
        { "result.Tap", static () => { var r = OkInt.Tap(NoOp); Keep(r.IsSuccess); } },
        { "result.TapError", static () => { var r = FailInt.TapError(NoOpError); Keep(r.IsFailure); } },
        { "result.ToNonGeneric", static () => { var r = OkInt.ToNonGeneric(); Keep(r.IsSuccess); } },
        { "result.ToGeneric", static () => { var r = OkUnit.ToGeneric(); Keep(r.IsSuccess); } },
        { "new Error(code, message)", static () => { var e = new Error("E", "m"); Keep(e.Code is null); } },
        { "Error.Create", static () => { var e = Error.Create("E", "m"); Keep(e.Code is null); } },
        { "error.Equals", static () => Keep(Err.Equals(Err)) },
        { "Result.Success().ToString()", static () => { AllocationHarness.Sink = OkUnit.ToString(); } },

        // ---- Verdict.Fluent ----
        { "result.Map, int to int", static () => { var r = OkInt.Map(Double); Keep(r.IsSuccess); } },
        { "result.Map, int to string", static () => { var r = OkInt.Map(ToText); Keep(r.IsSuccess); } },
        { "result.Match, success branch", static () => Keep(OkInt.Match(Double, OnError) == 0) },
        { "result.Match, failure branch", static () => Keep(FailInt.Match(Double, OnError) == 0) },
        { "result.OnSuccess", static () => { var r = OkInt.OnSuccess(NoOp); Keep(r.IsSuccess); } },
        { "result.OnFailure", static () => { var r = FailInt.OnFailure(NoOpError); Keep(r.IsFailure); } },
        { "three-step chain Map, Bind, Match", static () => Keep(OkInt.Map(Double).Bind(Bind).Match(Double, OnError) == 0) },

        // ---- Verdict.Extensions ----
        { "MultiResult<int>.Success", static () => { var r = MultiResult<int>.Success(42); Keep(r.IsSuccess); } },
        { "Combine, two results", static () => { var r = CombineExtensions.Combine(OkInt, OkInt); Keep(r.IsSuccess); } },
        { "Combine, three results", static () => { var r = CombineExtensions.Combine(OkInt, OkInt, OkInt); Keep(r.IsSuccess); } },
        { "Ensure, predicate holds", static () => { var r = OkInt.Ensure(IsPositive, Err); Keep(r.IsSuccess); } },
        { "Try, nothing thrown", static () => { var r = TryExtensions.Try(NotThrowing); Keep(r.IsSuccess); } },

        // ---- Verdict.Rich ----
        { "result.AsRich", static () => { var r = OkInt.AsRich(); Keep(r.IsSuccess); } },

        // ---- Verdict.Async, the ValueTask fast path ----
        // Read through .Result rather than awaited, because these are completed
        // and the fast path is synchronous. Awaiting them in the harness would
        // measure the harness's own state machine instead of the library.
        { "ValueTask Map, completed antecedent", static () => { var r = OkInt.AsValueTask().Map(Double); Keep(r.Result.IsSuccess); } },
        { "ValueTask Bind, completed antecedent", static () => { var r = OkInt.AsValueTask().Bind(Bind); Keep(r.Result.IsSuccess); } },
        { "ValueTask Tap, completed antecedent", static () => { var r = OkInt.AsValueTask().Tap(NoOp); Keep(r.Result.IsSuccess); } },
        { "ValueTask Ensure, completed antecedent", static () => { var r = OkInt.AsValueTask().Ensure(IsPositive, Err); Keep(r.Result.IsSuccess); } },
        { "ValueTask Match, completed antecedent", static () => { var r = OkInt.AsValueTask().Match(Double, OnError); Keep(r.Result == 0); } },
        { "ValueTask Map on a failure, completed", static () => { var r = FailInt.AsValueTask().Map(Double); Keep(r.Result.IsFailure); } },
        { "ValueTask four-step chain, completed", static () => { var r = OkInt.AsValueTask().Map(Double).Map(Double).Map(Double).Map(Double); Keep(r.Result.IsSuccess); } },
    };

    private static bool OkIntErrorCode() => FailInt.Error.Code is null;

    private static void Keep(bool value) => AllocationHarness.Sink = value ? null : AllocationHarness.Sink;

    [Theory]
    [MemberData(nameof(MustAllocateNothing))]
    public void OperationAllocatesNothing(string name, Action operation) =>
        AllocationHarness.AllocatesNothing(name, operation);
}
