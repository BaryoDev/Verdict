using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Verdict;
using Verdict.Fluent;
using Verdict.Extensions;
using Verdict.Async;
using Verdict.Rich;
using Verdict.Json;

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
        ErrorSanitizationExample();      // NEW
        ErrorCodeValidationExample();    // NEW

        // Fluent Package Examples
        FluentApiExample();
        PatternMatchingExample();
        RailwayOrientedProgrammingExample();

        // Extensions Package Examples
        MultiErrorValidationExample();
        CombineResultsExample();
        TryPatternExample();
        ErrorCollectionDisposalExample();
        DynamicErrorFactoryExample();    // NEW

        // Async Package Examples
        await AsyncPipelineExample();
        await AsyncErrorHandlingExample();
        await CancellationTokenExample();  // NEW
        await TimeoutExample();            // NEW

        // Json Package Examples
        JsonSerializationExample();        // NEW

        // Rich Package Examples
        RichMetadataExample();
        SuccessMessagesExample();
        ErrorMetadataExample();

        // Production-Ready Examples
        ZeroAllocationExample();           // NEW
        ThreadSafeUsageExample();          // NEW
        ProperDisposalExample();           // NEW
        ProductionTryPatternExample();     // NEW

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

    #region New Security & Robustness Examples

    static void ErrorSanitizationExample()
    {
        Console.WriteLine("═══ NEW: ERROR SANITIZATION (Security) ═══");
        Console.WriteLine("Prevent sensitive information leakage in production");

        try
        {
            throw new InvalidOperationException("Connection to server=prod.db;password=secret123 failed");
        }
        catch (Exception ex)
        {
            // Unsanitized - exposes sensitive info (use in development only)
            // NOTE: This is intentionally using the deprecated method for demonstration
#pragma warning disable CS0618 // Intentional: demonstrating deprecated vs new API
            var devError = Error.FromException(ex);
#pragma warning restore CS0618
            Console.WriteLine($"Development: {devError.Message}");

            // Sanitized - safe for production (RECOMMENDED)
            var prodError = Error.FromException(ex, sanitize: true);
            Console.WriteLine($"Production: {prodError.Message}");

            // Custom sanitized message
            var customError = Error.FromException(ex, sanitize: true, sanitizedMessage: "Database connection failed");
            Console.WriteLine($"Custom: {customError.Message}");
        }

        Console.WriteLine();
    }

    static void ErrorCodeValidationExample()
    {
        Console.WriteLine("═══ NEW: ERROR CODE VALIDATION (Security) ═══");
        Console.WriteLine("Ensure error codes contain only safe characters");

        // Valid codes
        Console.WriteLine($"'NOT_FOUND' valid: {Error.IsValidErrorCode("NOT_FOUND")}");
        Console.WriteLine($"'Error123' valid: {Error.IsValidErrorCode("Error123")}");

        // Invalid codes (could cause issues in logs or HTTP headers)
        Console.WriteLine($"'error-code' valid: {Error.IsValidErrorCode("error-code")}");
        Console.WriteLine($"'error.code' valid: {Error.IsValidErrorCode("error.code")}");

        // Create validated error (throws if invalid)
        var error = Error.CreateValidated("VALID_CODE", "This is validated");
        Console.WriteLine($"✓ Created validated error: {error.Code}");

        Console.WriteLine();
    }

    static void DynamicErrorFactoryExample()
    {
        Console.WriteLine("═══ NEW: DYNAMIC ERROR FACTORY (Validation) ═══");
        Console.WriteLine("Include value information in error messages");

        var userAge = 15;
        var result = Result<int>.Success(userAge)
            .Ensure(
                age => age >= 18,
                age => new Error("AGE_RESTRICTION", $"User is {age} years old, must be at least 18"));

        if (result.IsFailure)
        {
            Console.WriteLine($"✗ {result.Error.Message}");
        }

        // Real-world: Password validation with length info
        var password = "abc123";
        var passwordResult = Result<string>.Success(password)
            .Ensure(
                p => p.Length >= 12,
                p => new Error("WEAK_PASSWORD", $"Password has {p.Length} chars, minimum is 12"));

        if (passwordResult.IsFailure)
        {
            Console.WriteLine($"✗ {passwordResult.Error.Message}");
        }

        Console.WriteLine();
    }

    static async Task CancellationTokenExample()
    {
        Console.WriteLine("═══ NEW: CANCELLATION TOKEN SUPPORT (Async) ═══");
        Console.WriteLine("Properly cancel async operations");

        using var cts = new CancellationTokenSource();

        // Normal operation completes
        var result1 = await Task.FromResult(Result<int>.Success(42))
            .MapAsync(async (x, ct) =>
            {
                await Task.Delay(10, ct);
                return x * 2;
            }, cts.Token);
        Console.WriteLine($"✓ Completed: {result1.Value}");

        // Demonstrate cancellation awareness
        Console.WriteLine("  (Cancellation tokens are passed through the entire chain)");

        Console.WriteLine();
    }

    static async Task TimeoutExample()
    {
        Console.WriteLine("═══ NEW: TIMEOUT SUPPORT (Async) ═══");
        Console.WriteLine("Apply timeouts to async Result operations");

        // Fast operation - completes before timeout
        var fastResult = await Task.Run(async () =>
        {
            await Task.Delay(10);
            return Result<string>.Success("Fast response");
        }).WithTimeout(TimeSpan.FromSeconds(5), "TIMEOUT", "Operation timed out");

        Console.WriteLine($"Fast operation: {(fastResult.IsSuccess ? $"✓ {fastResult.Value}" : $"✗ {fastResult.Error.Code}")}");

        // Simulated slow operation - would timeout
        Console.WriteLine("  (WithTimeout returns failure if operation exceeds duration)");

        Console.WriteLine();
    }

    static void JsonSerializationExample()
    {
        Console.WriteLine("═══ NEW: JSON SERIALIZATION (Verdict.Json) ═══");
        Console.WriteLine("Serialize/deserialize Results for APIs and storage");

        // Serialize success
        var successResult = Result<int>.Success(42);
        var successJson = successResult.ToJson();
        Console.WriteLine($"Success JSON: {successJson}");

        // Serialize failure
        var failureResult = Result<int>.Failure("NOT_FOUND", "Resource not found");
        var failureJson = failureResult.ToJson();
        Console.WriteLine($"Failure JSON: {failureJson}");

        // Deserialize
        var restored = VerdictJsonExtensions.FromJson<int>(successJson);
        Console.WriteLine($"Restored: IsSuccess={restored.IsSuccess}, Value={restored.Value}");

        // Non-generic Result
        var nonGenericJson = Result.Success().ToJson();
        Console.WriteLine($"Non-generic: {nonGenericJson}");

        Console.WriteLine();
    }

    #endregion

    #region Production-Ready Examples

    static void ZeroAllocationExample()
    {
        Console.WriteLine("═══ NEW: ZERO-ALLOCATION PATTERNS (Performance) ═══");
        Console.WriteLine("Demonstrates zero-allocation success path");

        // All these operations allocate ZERO bytes on the heap for success path
        var result1 = Result<int>.Success(42);
        var result2 = Result<int>.Failure("ERROR", "message");

        // Struct-based - lives on stack (readonly struct)
        Console.WriteLine("Result<T> is a readonly struct - zero heap allocation on success path");

        // Chain operations without allocation
        var sum = 0;
        for (int i = 0; i < 1000; i++)
        {
            var r = Result<int>.Success(i);
            if (r.IsSuccess) sum += r.Value;
        }
        Console.WriteLine($"✓ Processed 1000 results with zero heap allocation: sum={sum}");

        // Error struct is also zero-allocation
        var error = new Error("CODE", "Message");
        Console.WriteLine($"✓ Error struct created: [{error.Code}] (no heap allocation)");

        Console.WriteLine();
    }

    static void ThreadSafeUsageExample()
    {
        Console.WriteLine("═══ NEW: THREAD-SAFE USAGE (Concurrency) ═══");
        Console.WriteLine("Demonstrates safe concurrent result handling");

        // Results are immutable - safe to share across threads
        var sharedResult = Result<string>.Success("shared data");

        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(() =>
        {
            // Safe: reading immutable struct
            if (sharedResult.IsSuccess)
            {
                return $"Thread {i}: {sharedResult.Value}";
            }
            return $"Thread {i}: failed";
        })).ToArray();

        Task.WaitAll(tasks);
        Console.WriteLine($"✓ {tasks.Length} threads safely accessed shared Result");

        Console.WriteLine();
    }

    static void ProperDisposalExample()
    {
        Console.WriteLine("═══ NEW: PROPER DISPOSAL (Resource Management) ═══");
        Console.WriteLine("MultiResult uses ArrayPool - always dispose!");

        // CORRECT: Use using statement or try-finally
        var errors = new[] {
            new Error("E1", "Error 1"),
            new Error("E2", "Error 2")
        };

        var multiResult = MultiResult<int>.Failure(errors);
        try
        {
            Console.WriteLine($"MultiResult has {multiResult.ErrorCount} errors");
            foreach (var error in multiResult.Errors)
            {
                Console.WriteLine($"  • {error.Code}");
            }
        }
        finally
        {
            multiResult.DisposeErrors(); // Return array to pool
            Console.WriteLine("✓ Errors disposed - array returned to pool");
        }

        Console.WriteLine();
    }

    static void ProductionTryPatternExample()
    {
        Console.WriteLine("═══ NEW: PRODUCTION TRY PATTERN (Error Handling) ═══");
        Console.WriteLine("Safe exception handling with sanitization");

        // Default: sanitized messages (safe for production)
        var result1 = TryExtensions.Try<int>(() =>
            throw new Exception("Internal: connection string=secret;password=123"));
        Console.WriteLine($"Default (sanitized): {result1.Error.Message}");

        // Custom error factory for controlled error creation
        var result2 = TryExtensions.Try<int>(
            () => throw new ArgumentException("Invalid user input"),
            ex => new Error("VALIDATION_ERROR", "Please check your input"));
        Console.WriteLine($"Custom factory: {result2.Error.Message}");

        // Development mode with full details (via custom factory)
        var result3 = TryExtensions.Try<int>(
            () => throw new InvalidOperationException("Debug info here"),
            ex => Error.FromException(ex, sanitize: false));
        Console.WriteLine($"Development mode: {result3.Error.Message}");

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
            // NOTE: Intentionally using deprecated method for demonstration
#pragma warning disable CS0618 // Intentional: showing exception handling pattern
            return Result<int>.Failure(Error.FromException(ex));
#pragma warning restore CS0618
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
