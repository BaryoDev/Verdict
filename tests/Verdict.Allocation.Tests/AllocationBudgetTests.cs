using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Verdict.Async;
using Verdict.Extensions;
using Verdict.Json;
using Verdict.Rich;
using Xunit;

namespace Verdict.Allocation.Tests;

/// <summary>
/// The operations that do allocate, and how much they are allowed to.
/// </summary>
/// <remarks>
/// These are budgets rather than targets. The numbers were measured on net8.0
/// Release and rounded up to the next eight byte boundary, because the CLR
/// rounds an allocation to one and a budget set at the exact measured value
/// fails on the rounding. They are tight enough that an operation which doubles
/// its allocation fails here. Changing a number is a deliberate edit that
/// appears in review, which is the whole point of writing them down.
/// </remarks>
public class AllocationBudgetTests
{
    private static readonly Error Err = new("NOT_FOUND", "missing");
    private static readonly Error[] ThreeErrors = { Err, Err, Err };
    private static readonly List<Error> ThreeErrorsList = new() { Err, Err, Err };
    private static readonly Result<int> OkInt = Result<int>.Success(42);
    private static readonly Result<int> FailInt = Result<int>.Failure(Err);
    private static readonly Result<int>[] MixedResults = { OkInt, OkInt, FailInt };
    private static readonly Func<int> Throwing = static () => throw new InvalidOperationException("x");

    private static readonly JsonSerializerOptions JsonOptions = VerdictJsonExtensions.CreateVerdictJsonOptions();
    private static readonly string SerializedOk = JsonSerializer.Serialize(OkInt, JsonOptions);

    public static TheoryData<string, int, Action> Budgets() => new()
    {
        // ---- Verdict, the core ----
        { "result.ToString(), success", 56, static () => { AllocationHarness.Sink = OkInt.ToString(); } },
        { "result.ToString(), failure", 88, static () => { AllocationHarness.Sink = FailInt.ToString(); } },
        { "error.ToString()", 80, static () => { AllocationHarness.Sink = Err.ToString(); } },

        // ---- Verdict.Extensions ----
        { "MultiResult<int>.Failure(error)", 48, static () => { var r = MultiResult<int>.Failure(Err); Keep(r.IsFailure); } },
        { "ErrorCollection.Create(error)", 48, static () => { var c = ErrorCollection.Create(Err); Keep(c.Count == 0); } },
        { "ErrorCollection.Create(Error[3])", 96, static () => { var c = ErrorCollection.Create(ThreeErrors); Keep(c.Count == 0); } },
        { "ErrorCollection.Create(IEnumerable) then dispose", 96, static () => { var c = ErrorCollection.Create((IEnumerable<Error>)ThreeErrorsList); Keep(c.Count == 0); c.Dispose(); } },
        // Skipping the dispose is what a caller actually does, and it is the row that
        // makes the pooling argument in #44. Kept so the cost of forgetting is visible.
        { "ErrorCollection.Create(IEnumerable), dispose forgotten", 512, static () => { var c = ErrorCollection.Create((IEnumerable<Error>)ThreeErrorsList); Keep(c.Count == 0); } },
        { "Merge(Result<int>[3])", 96, static () => { var r = CombineExtensions.Merge(MixedResults); Keep(r.IsFailure); } },
        { "Try, exception thrown", 384, static () => { var r = TryExtensions.Try(Throwing); Keep(r.IsFailure); } },

        // ---- Verdict.Rich ----
        { "result.WithSuccess", 80, static () => { var r = OkInt.WithSuccess("done"); Keep(r.IsSuccess); } },
        { "result.WithErrorMetadata", 104, static () => { var r = FailInt.WithErrorMetadata("k", "v"); Keep(r.IsFailure); } },

        // ---- Verdict.Json ----
        { "JsonSerializer.Serialize(Result<int>)", 96, static () => { AllocationHarness.Sink = JsonSerializer.Serialize(OkInt, JsonOptions); } },
        { "JsonSerializer.Deserialize<Result<int>>", 128, static () => { var r = JsonSerializer.Deserialize<Result<int>>(SerializedOk, JsonOptions); Keep(r.IsSuccess); } },

        // ---- Verdict.Async, the Task API ----
        // Kept as a budget rather than fixed, because an async Task method
        // allocates whether it waits or not. This row is also what makes the
        // ValueTask rows in ZeroAllocationTests meaningful: the same four steps
        // over Task cost this much, and over ValueTask they cost nothing.
        { "Task four-step chain, completed antecedent", 512, static () => { var t = Task.FromResult(OkInt).Map(Double).Map(Double).Map(Double).Map(Double); Keep(t.Result.IsSuccess); } },
    };

    private static readonly Func<int, int> Double = static x => x * 2;

    private static void Keep(bool value) => AllocationHarness.Sink = value ? null : AllocationHarness.Sink;

    [Theory]
    [MemberData(nameof(Budgets))]
    public void OperationStaysWithinBudget(string name, int maxBytes, Action operation) =>
        AllocationHarness.WithinBudget(name, maxBytes, operation);
}
