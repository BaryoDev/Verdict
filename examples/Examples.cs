using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Verdict;
using Verdict.Fluent;
using Verdict.Extensions;
using Verdict.Async;
using Verdict.Rich;

namespace Verdict.Examples;

/// <summary>
/// Comprehensive examples demonstrating all Verdict usage patterns.
/// Shows real-world scenarios for each package in the Verdict ecosystem.
/// </summary>
public class Examples
{
    public static async Task Main()
    {
        Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        VERDICT - Comprehensive Usage Examples             ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝\n");

        // Core Package Examples
        BasicUsageExample();
        ImplicitConversionExample();
        NonGenericResultExample();
        ErrorHandlingExample();
        ValueOrExample();

        // Fluent Package Examples
        FluentApiExample();
        PatternMatchingExample();
        RailwayOrientedProgrammingExample();

        // Extensions Package Examples
        MultiErrorValidationExample();
        CombineResultsExample();
        TryPatternExample();
        ErrorCollectionDisposalExample();

        // Async Package Examples
        await AsyncPipelineExample();
        await AsyncErrorHandlingExample();

        // Rich Package Examples
        RichMetadataExample();
        SuccessMessagesExample();
        ErrorMetadataExample();

        // Real-World Scenarios
        FormValidationScenario();
        await ApiCallScenario();
        DomainDrivenDesignScenario();

        Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║             Examples completed successfully!               ║");
        Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
    }

    #region Core Package Examples

    static void BasicUsageExample()
    {
        Console.WriteLine("═══ 1. BASIC USAGE (Core) ═══");

        var successResult = Divide(10, 2);
        if (successResult.IsSuccess)
        {
            Console.WriteLine($"✓ Success: 10 / 2 = {successResult.Value}");
        }

        var failureResult = Divide(10, 0);
        if (failureResult.IsFailure)
        {
            Console.WriteLine($"✗ Failure: [{failureResult.Error.Code}] {failureResult.Error.Message}");
        }

        Console.WriteLine();
    }

    static void ImplicitConversionExample()
    {
        Console.WriteLine("═══ 2. IMPLICIT CONVERSIONS (Core) ═══");

        // Implicit conversion from T to Result<T>
        Result<int> success = 42;
        Console.WriteLine($"✓ Implicit from value: {success.Value}");

        // Implicit conversion from Error to Result<T>
        Result<int> failure = new Error("ERROR", "Something went wrong");
        Console.WriteLine($"✗ Implicit from error: {failure.Error.Message}");

        Console.WriteLine();
    }

    static void NonGenericResultExample()
    {
        Console.WriteLine("═══ 3. NON-GENERIC RESULT (Core) ═══");
        Console.WriteLine("Use for void operations (no return value)");

        var result1 = SaveToDatabase("John Doe");
        Console.WriteLine($"Save operation: {(result1.IsSuccess ? "✓ Success" : "✗ Failed")}");

        var result2 = SaveToDatabase("");
        if (result2.IsFailure)
        {
            Console.WriteLine($"✗ Error: {result2.Error.Message}");
        }

        Console.WriteLine();
    }

    static void ErrorHandlingExample()
    {
        Console.WriteLine("═══ 4. ERROR HANDLING WITH EXCEPTIONS (Core) ═══");

        var result = ProcessWithException();
        if (result.IsFailure)
        {
            Console.WriteLine($"✗ Error Code: {result.Error.Code}");
            Console.WriteLine($"  Message: {result.Error.Message}");
            if (result.Error.Exception != null)
            {
                Console.WriteLine($"  Exception Type: {result.Error.Exception.GetType().Name}");
            }
        }

        Console.WriteLine();
    }

    static void ValueOrExample()
    {
        Console.WriteLine("═══ 5. VALUE OR FALLBACK (Core) ═══");

        var success = Result<int>.Success(42);
        var failure = Result<int>.Failure("ERROR", "Failed");

        Console.WriteLine($"Success value or 0: {success.ValueOr(0)}");
        Console.WriteLine($"Failure value or 0: {failure.ValueOr(0)}");
        Console.WriteLine($"Failure value or default: {failure.ValueOrDefault}");

        // Using factory function
        var computed = failure.ValueOr(error =>
        {
            Console.WriteLine($"  Computing fallback for error: {error.Code}");
            return 999;
        });
        Console.WriteLine($"Computed fallback: {computed}");

        Console.WriteLine();
    }

    #endregion

    #region Fluent Package Examples

    static void FluentApiExample()
    {
        Console.WriteLine("═══ 6. FLUENT API CHAINING (Fluent) ═══");

        var result = Divide(100, 5)
            .Map(x => x * 2)                // Transform success value
            .Map(x => x + 10)               // Chain transformations
            .OnSuccess(x => Console.WriteLine($"✓ Chained result: {x}"))
            .OnFailure(e => Console.WriteLine($"✗ Error: {e.Message}"));

        Console.WriteLine();
    }

    static void PatternMatchingExample()
    {
        Console.WriteLine("═══ 7. PATTERN MATCHING (Fluent) ═══");

        var message1 = Divide(20, 4).Match(
            onSuccess: value => $"✓ Result is {value}",
            onFailure: error => $"✗ Error: {error.Message}"
        );
        Console.WriteLine(message1);

        var message2 = Divide(20, 0).Match(
            onSuccess: value => $"✓ Result is {value}",
            onFailure: error => $"✗ Error: {error.Message}"
        );
        Console.WriteLine(message2);

        Console.WriteLine();
    }

    static void RailwayOrientedProgrammingExample()
    {
        Console.WriteLine("═══ 8. RAILWAY-ORIENTED PROGRAMMING (Fluent) ═══");
        Console.WriteLine("Success path continues, failure path short-circuits");

        var pipeline1 = Result<int>.Success(10)
            .Map(x => x * 2)                    // 10 -> 20
            .Map(x => x + 5)                    // 20 -> 25
            .Map(x => x / 5);                   // 25 -> 5
        Console.WriteLine($"✓ Success pipeline: {pipeline1.Value}");

        var pipeline2 = Divide(10, 0)           // Failure here
            .Map(x => x * 2)                    // Skipped
            .Map(x => x + 5)                    // Skipped
            .OnFailure(e => Console.WriteLine($"✗ Pipeline failed at: {e.Message}"));

        Console.WriteLine();
    }

    #endregion

    #region Extensions Package Examples

    static void MultiErrorValidationExample()
    {
        Console.WriteLine("═══ 9. MULTI-ERROR VALIDATION (Extensions) ═══");
        Console.WriteLine("Return all validation errors at once");

        var user = new { Name = "", Age = -5, Email = "invalid" };
        var result = ValidateUser(user.Name, user.Age, user.Email);

        if (result.IsFailure)
        {
            Console.WriteLine($"✗ Validation failed with {result.ErrorCount} errors:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  • [{error.Code}] {error.Message}");
            }
        }

        Console.WriteLine();
    }

    static void CombineResultsExample()
    {
        Console.WriteLine("═══ 10. COMBINE/MERGE OPERATIONS (Extensions) ═══");

        var results = new[]
        {
            Divide(10, 2),      // Success: 5
            Divide(20, 4),      // Success: 5
            Divide(30, 0)       // Failure
        };

        var merged = CombineExtensions.Merge(results);
        if (merged.IsFailure)
        {
            Console.WriteLine($"✗ Merged result failed with {merged.ErrorCount} error(s)");
            Console.WriteLine($"  First error: {merged.Errors.ToArray()[0].Message}");
        }

        Console.WriteLine();
    }

    static void TryPatternExample()
    {
        Console.WriteLine("═══ 11. TRY PATTERN (Extensions) ═══");
        Console.WriteLine("Automatically catch exceptions and convert to Result");

        var result = TryExtensions.Try(() =>
        {
            Console.WriteLine("  Attempting risky operation...");
            if (new Random().Next(2) == 0)
                throw new InvalidOperationException("Random failure!");
            return 42;
        });

        if (result.IsSuccess)
            Console.WriteLine($"✓ Try succeeded: {result.Value}");
        else
            Console.WriteLine($"✗ Try caught exception: {result.Error.Message}");

        Console.WriteLine();
    }

    static void ErrorCollectionDisposalExample()
    {
        Console.WriteLine("═══ 12. ERROR COLLECTION DISPOSAL (Extensions) ═══");
        Console.WriteLine("Properly manage ArrayPool for large error collections");

        // Create large error collection using ArrayPool
        var largeErrors = Enumerable.Range(0, 1000)
            .Select(i => new Error($"ERROR_{i}", $"Error message {i}"));

        var result = MultiResult<int>.Failure(ErrorCollection.Create(largeErrors));
        Console.WriteLine($"Created result with {result.ErrorCount} errors (uses ArrayPool)");

        // Important: Dispose to return array to pool
        result.DisposeErrors();
        Console.WriteLine("✓ Disposed errors and returned array to pool");

        Console.WriteLine();
    }

    #endregion

    #region Async Package Examples

    static async Task AsyncPipelineExample()
    {
        Console.WriteLine("═══ 13. ASYNC PIPELINE (Async) ═══");
        Console.WriteLine("Chain async operations seamlessly");

        var result = await FetchUserIdAsync(123)
            .MapAsync(userId => FetchUserNameAsync(userId))
            .MapAsync(userName => FetchUserEmailAsync(userName))
            .TapAsync(email => Task.Run(() =>
                Console.WriteLine($"✓ Final email: {email}")));

        Console.WriteLine();
    }

    static async Task AsyncErrorHandlingExample()
    {
        Console.WriteLine("═══ 14. ASYNC ERROR HANDLING (Async) ═══");

        var result = await FetchUserIdAsync(999)  // Will fail
            .MapAsync(userId => FetchUserNameAsync(userId))
            .TapErrorAsync(error => Task.Run(() =>
                Console.WriteLine($"✗ Async pipeline failed: {error.Message}")));

        Console.WriteLine();
    }

    #endregion

    #region Rich Package Examples

    static void RichMetadataExample()
    {
        Console.WriteLine("═══ 15. RICH METADATA (Rich) ═══");
        Console.WriteLine("Add success messages and error metadata");

        var result = ProcessOrder(123)
            .WithSuccess("Order validated")
            .WithSuccess("Payment processed")
            .WithSuccess("Email sent");

        if (result.IsSuccess)
        {
            Console.WriteLine($"✓ Order processed: {result.Value}");
            Console.WriteLine("  Steps completed:");
            foreach (var success in result.Successes)
            {
                Console.WriteLine($"    • {success.Message}");
            }
        }

        Console.WriteLine();
    }

    static void SuccessMessagesExample()
    {
        Console.WriteLine("═══ 16. SUCCESS MESSAGES (Rich) ═══");
        Console.WriteLine("Track operation steps for auditing");

        var order = RichResult<string>.Success("ORDER_12345")
            .WithSuccess("Inventory checked")
            .WithSuccess("Payment authorized")
            .WithSuccess("Shipping label created");

        Console.WriteLine($"✓ Order {order.Value} completed with {order.Successes.Count} steps");

        Console.WriteLine();
    }

    static void ErrorMetadataExample()
    {
        Console.WriteLine("═══ 17. ERROR METADATA (Rich) ═══");
        Console.WriteLine("Add diagnostic information to errors");

        var result = RichResult<int>.Failure("VALIDATION_ERROR", "Invalid input")
            .WithErrorMetadata("Field", "Email")
            .WithErrorMetadata("Value", "invalid@")
            .WithErrorMetadata("Timestamp", DateTime.UtcNow);

        Console.WriteLine($"✗ Error: {result.Error.Message}");
        Console.WriteLine($"  Metadata ({result.ErrorMetadata.Count} items):");
        foreach (var kvp in result.ErrorMetadata)
        {
            Console.WriteLine($"    • {kvp.Key}: {kvp.Value}");
        }

        Console.WriteLine();
    }

    #endregion

    #region Real-World Scenarios

    static void FormValidationScenario()
    {
        Console.WriteLine("═══ 18. REAL-WORLD: FORM VALIDATION ═══");
        Console.WriteLine("E-commerce checkout validation with multiple errors");

        var form = new
        {
            Email = "invalid-email",
            CreditCard = "1234",
            ShippingAddress = "",
            AgreeToTerms = false
        };

        var errors = new List<Error>();

        if (!form.Email.Contains("@"))
            errors.Add(new Error("INVALID_EMAIL", "Email must contain @"));

        if (form.CreditCard.Length < 16)
            errors.Add(new Error("INVALID_CARD", "Credit card must be 16 digits"));

        if (string.IsNullOrEmpty(form.ShippingAddress))
            errors.Add(new Error("MISSING_ADDRESS", "Shipping address is required"));

        if (!form.AgreeToTerms)
            errors.Add(new Error("TERMS_NOT_AGREED", "Must agree to terms"));

        var result = errors.Any()
            ? MultiResult<string>.Failure(errors.ToArray())
            : MultiResult<string>.Success("ORDER_PLACED");

        if (result.IsFailure)
        {
            Console.WriteLine($"✗ Checkout failed with {result.ErrorCount} errors:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"  • {error.Message}");
            }
        }

        Console.WriteLine();
    }

    static async Task ApiCallScenario()
    {
        Console.WriteLine("═══ 19. REAL-WORLD: API CALL CHAIN ═══");
        Console.WriteLine("Microservice communication with error propagation");

        var result = await AuthenticateUser("john@example.com", "password123")
            .BindAsync(token => FetchUserProfile(token))
            .BindAsync(profile => FetchUserOrders(profile))
            .MapAsync(orders => Task.FromResult($"Found {orders.Length} orders"))
            .TapAsync(message => Task.Run(() =>
                Console.WriteLine($"✓ {message}")))
            .TapErrorAsync(error => Task.Run(() =>
                Console.WriteLine($"✗ API chain failed: {error.Message}")));

        Console.WriteLine();
    }

    static void DomainDrivenDesignScenario()
    {
        Console.WriteLine("═══ 20. REAL-WORLD: DOMAIN-DRIVEN DESIGN ═══");
        Console.WriteLine("Aggregate validation with business rules");

        var order = new
        {
            Total = 150.00m,
            Items = 3,
            CustomerId = 123,
            InventoryAvailable = true
        };

        var result = ValidateDomainRules(order)
            .Match(
                onSuccess: orderId => $"✓ Order {orderId} created successfully",
                onFailure: error => $"✗ Business rule violation: {error.Message}"
            );

        Console.WriteLine(result);
        Console.WriteLine();
    }

    #endregion

    #region Helper Methods

    static Result<int> Divide(int numerator, int denominator)
    {
        if (denominator == 0)
            return Result<int>.Failure("DIVIDE_BY_ZERO", "Cannot divide by zero");

        return Result<int>.Success(numerator / denominator);
    }

    static Result SaveToDatabase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Result.Failure("INVALID_NAME", "Name cannot be empty");

        Console.WriteLine($"  Saving '{name}' to database...");
        return Result.Success();
    }

    static Result<int> ProcessWithException()
    {
        try
        {
            throw new InvalidOperationException("Database connection failed");
        }
        catch (Exception ex)
        {
            return Result<int>.Failure(Error.FromException(ex));
        }
    }

    static MultiResult<string> ValidateUser(string name, int age, string email)
    {
        var errors = new List<Error>();

        if (string.IsNullOrEmpty(name))
            errors.Add(new Error("MISSING_NAME", "Name is required"));

        if (age < 0)
            errors.Add(new Error("INVALID_AGE", "Age cannot be negative"));

        if (!email.Contains("@"))
            errors.Add(new Error("INVALID_EMAIL", "Email must contain @"));

        return errors.Any()
            ? MultiResult<string>.Failure(errors.ToArray())
            : MultiResult<string>.Success("USER_VALID");
    }

    static async Task<Result<int>> FetchUserIdAsync(int id)
    {
        await Task.Delay(10); // Simulate async work
        return id == 999
            ? Result<int>.Failure("USER_NOT_FOUND", "User does not exist")
            : Result<int>.Success(id);
    }

    static async Task<string> FetchUserNameAsync(int userId)
    {
        await Task.Delay(10);
        return $"User_{userId}";
    }

    static async Task<string> FetchUserEmailAsync(string userName)
    {
        await Task.Delay(10);
        return $"{userName}@example.com";
    }

    static RichResult<string> ProcessOrder(int orderId)
    {
        return RichResult<string>.Success($"ORDER_{orderId}");
    }

    static async Task<Result<string>> AuthenticateUser(string email, string password)
    {
        await Task.Delay(10);
        return Result<string>.Success("TOKEN_ABC123");
    }

    static async Task<Result<string>> FetchUserProfile(string token)
    {
        await Task.Delay(10);
        return Result<string>.Success("PROFILE_123");
    }

    static async Task<Result<string[]>> FetchUserOrders(string profile)
    {
        await Task.Delay(10);
        return Result<string[]>.Success(new[] { "ORDER_1", "ORDER_2", "ORDER_3" });
    }

    static Result<string> ValidateDomainRules(dynamic order)
    {
        if (order.Total < 10)
            return Result<string>.Failure("MIN_ORDER_VALUE", "Order must be at least $10");

        if (order.Items > 100)
            return Result<string>.Failure("MAX_ITEMS_EXCEEDED", "Maximum 100 items per order");

        if (!order.InventoryAvailable)
            return Result<string>.Failure("OUT_OF_STOCK", "Items are out of stock");

        return Result<string>.Success($"ORDER_{order.CustomerId}_{DateTime.Now.Ticks}");
    }

    #endregion
}
