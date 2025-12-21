using System;
using Upshot;
using Upshot.Fluent;

namespace Upshot.Examples;

/// <summary>
/// Example demonstrating Upshot usage patterns.
/// </summary>
public class Examples
{
    public static void Main()
    {
        Console.WriteLine("=== Upshot Examples ===\n");
        
        BasicUsageExample();
        ImplicitConversionExample();
        FluentApiExample();
        PatternMatchingExample();
    }

    static void BasicUsageExample()
    {
        Console.WriteLine("1. Basic Usage:");
        
        var successResult = Divide(10, 2);
        if (successResult.IsSuccess)
        {
            Console.WriteLine($"   Success: 10 / 2 = {successResult.Value}");
        }
        
        var failureResult = Divide(10, 0);
        if (failureResult.IsFailure)
        {
            Console.WriteLine($"   Failure: [{failureResult.Error.Code}] {failureResult.Error.Message}");
        }
        
        Console.WriteLine();
    }

    static void ImplicitConversionExample()
    {
        Console.WriteLine("2. Implicit Conversions:");
        
        Result<int> success = 42;  // Implicit from T
        Console.WriteLine($"   Implicit success: {success.Value}");
        
        Result<int> failure = new Error("ERROR", "Something went wrong");  // Implicit from Error
        Console.WriteLine($"   Implicit failure: {failure.Error.Message}");
        
        Console.WriteLine();
    }

    static void FluentApiExample()
    {
        Console.WriteLine("3. Fluent API:");
        
        var result = Divide(100, 5)
            .Map(x => x * 2)
            .Map(x => x + 10)
            .OnSuccess(x => Console.WriteLine($"   Chained result: {x}"))
            .OnFailure(e => Console.WriteLine($"   Error: {e.Message}"));
        
        Console.WriteLine();
    }

    static void PatternMatchingExample()
    {
        Console.WriteLine("4. Pattern Matching:");
        
        var message1 = Divide(20, 4).Match(
            onSuccess: value => $"   Result is {value}",
            onFailure: error => $"   Error: {error.Message}"
        );
        Console.WriteLine(message1);
        
        var message2 = Divide(20, 0).Match(
            onSuccess: value => $"   Result is {value}",
            onFailure: error => $"   Error: {error.Message}"
        );
        Console.WriteLine(message2);
        
        Console.WriteLine();
    }

    static Result<int> Divide(int numerator, int denominator)
    {
        if (denominator == 0)
            return Result<int>.Failure("DIVIDE_BY_ZERO", "Cannot divide by zero");
        
        return Result<int>.Success(numerator / denominator);
    }
}
