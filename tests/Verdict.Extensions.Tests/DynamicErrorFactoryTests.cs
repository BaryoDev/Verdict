using System.Linq;
using FluentAssertions;
using Verdict.Extensions;
using Xunit;

namespace Verdict.Extensions.Tests;

/// <summary>
/// Tests for dynamic error factory in validation extensions.
/// </summary>
public class DynamicErrorFactoryTests
{
    #region Result<T> Ensure with Error Factory

    [Fact]
    public void Ensure_WithErrorFactory_PredicateTrue_ShouldReturnSuccess()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var ensured = result.Ensure(
            x => x > 0,
            x => new Error("INVALID", $"Value {x} is not positive"));

        // Assert
        ensured.IsSuccess.Should().BeTrue();
        ensured.Value.Should().Be(42);
    }

    [Fact]
    public void Ensure_WithErrorFactory_PredicateFalse_ShouldReturnDynamicError()
    {
        // Arrange
        var result = Result<int>.Success(-5);

        // Act
        var ensured = result.Ensure(
            x => x > 0,
            x => new Error("INVALID", $"Value {x} is not positive"));

        // Assert
        ensured.IsFailure.Should().BeTrue();
        ensured.Error.Code.Should().Be("INVALID");
        ensured.Error.Message.Should().Be("Value -5 is not positive");
    }

    [Fact]
    public void Ensure_WithErrorFactory_OnFailure_ShouldPropagateOriginalError()
    {
        // Arrange
        var result = Result<int>.Failure("ORIGINAL", "Original error");

        // Act
        var ensured = result.Ensure(
            x => x > 0,
            x => new Error("NEVER_CALLED", $"This should not be called"));

        // Assert
        ensured.IsFailure.Should().BeTrue();
        ensured.Error.Code.Should().Be("ORIGINAL");
    }

    [Fact]
    public void Ensure_WithErrorFactory_IncludesValueInError()
    {
        // Arrange
        var user = new TestUser { Name = "John", Age = 15 };
        var result = Result<TestUser>.Success(user);

        // Act
        var ensured = result.Ensure(
            u => u.Age >= 18,
            u => new Error("AGE_RESTRICTION", $"User {u.Name} is only {u.Age} years old, minimum age is 18"));

        // Assert
        ensured.IsFailure.Should().BeTrue();
        ensured.Error.Message.Should().Be("User John is only 15 years old, minimum age is 18");
    }

    #endregion

    #region MultiResult<T> Ensure with Error Factory

    [Fact]
    public void MultiResult_Ensure_WithErrorFactory_PredicateTrue_ShouldReturnSuccess()
    {
        // Arrange
        var result = MultiResult<int>.Success(42);

        // Act
        var ensured = result.Ensure(
            x => x > 0,
            x => new Error("INVALID", $"Value {x} is not positive"));

        // Assert
        ensured.IsSuccess.Should().BeTrue();
        ensured.Value.Should().Be(42);
    }

    [Fact]
    public void MultiResult_Ensure_WithErrorFactory_PredicateFalse_ShouldReturnDynamicError()
    {
        // Arrange
        var result = MultiResult<string>.Success("test");

        // Act
        var ensured = result.Ensure(
            s => s.Length >= 10,
            s => new Error("TOO_SHORT", $"String '{s}' has length {s.Length}, minimum is 10"));

        // Assert
        ensured.IsFailure.Should().BeTrue();
        var firstError = ensured.Errors.ToArray()[0];
        firstError.Code.Should().Be("TOO_SHORT");
        firstError.Message.Should().Be("String 'test' has length 4, minimum is 10");
    }

    [Fact]
    public void MultiResult_Ensure_WithErrorFactory_OnFailure_ShouldPropagateOriginalError()
    {
        // Arrange
        var result = MultiResult<int>.Failure("ORIGINAL", "Original error");

        // Act
        var ensured = result.Ensure(
            x => x > 0,
            x => new Error("NEVER_CALLED", "This should not be called"));

        // Assert
        ensured.IsFailure.Should().BeTrue();
        var firstError = ensured.Errors.ToArray()[0];
        firstError.Code.Should().Be("ORIGINAL");
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public void Ensure_EmailValidation_ShouldIncludeEmailInError()
    {
        // Arrange
        var email = "invalid-email";
        var result = Result<string>.Success(email);

        // Act
        var validated = result.Ensure(
            e => e.Contains("@") && e.Contains("."),
            e => new Error("INVALID_EMAIL", $"'{e}' is not a valid email address"));

        // Assert
        validated.IsFailure.Should().BeTrue();
        validated.Error.Message.Should().Be("'invalid-email' is not a valid email address");
    }

    [Fact]
    public void Ensure_RangeValidation_ShouldIncludeValueAndBounds()
    {
        // Arrange
        var value = 150;
        var min = 0;
        var max = 100;
        var result = Result<int>.Success(value);

        // Act
        var validated = result.Ensure(
            v => v >= min && v <= max,
            v => new Error("OUT_OF_RANGE", $"Value {v} is outside valid range [{min}-{max}]"));


        // Assert
        validated.IsFailure.Should().BeTrue();
        validated.Error.Message.Should().Be("Value 150 is outside valid range [0-100]");
    }

    [Fact]
    public void Ensure_ChainedValidation_WithDifferentErrors()
    {
        // Arrange
        var password = "abc";
        var result = Result<string>.Success(password);

        // Act
        var validated = result
            .Ensure(
                p => p.Length >= 8,
                p => new Error("TOO_SHORT", $"Password '{p}' has {p.Length} chars, minimum is 8"))
            .Ensure(
                p => p.ToCharArray().Any(char.IsDigit),
                p => new Error("NO_DIGIT", $"Password must contain at least one digit"));

        // Assert - First validation fails, so second is not checked
        validated.IsFailure.Should().BeTrue();
        validated.Error.Code.Should().Be("TOO_SHORT");
        validated.Error.Message.Should().Be("Password 'abc' has 3 chars, minimum is 8");
    }

    #endregion

    private class TestUser
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }
}
