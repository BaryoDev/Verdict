using System;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Upshot.Async;

namespace Upshot.Async.Tests;

public class AsyncResultExtensionsTests
{
    // ==================== MapAsync ====================

    [Fact]
    public async Task MapAsync_Success_ShouldTransformValue()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        var mapped = await resultTask.MapAsync(async x => 
        {
            await Task.Yield();
            return x.ToString();
        });

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be("42");
    }

    [Fact]
    public async Task MapAsync_Failure_ShouldPropagateError()
    {
        // Arrange
        var error = new Error("ERR", "Msg");
        var resultTask = Task.FromResult(Result<int>.Failure(error));

        // Act
        var mapped = await resultTask.MapAsync(async x => 
        {
            await Task.Yield();
            return x.ToString();
        });

        // Assert
        mapped.IsFailure.Should().BeTrue();
        mapped.Error.Should().Be(error);
    }

    [Fact]
    public async Task Map_SyncMapper_ShouldTransformValue()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        var mapped = await resultTask.Map(x => x * 2);

        // Assert
        mapped.IsSuccess.Should().BeTrue();
        mapped.Value.Should().Be(84);
    }

    // ==================== BindAsync ====================

    [Fact]
    public async Task BindAsync_Success_ShouldChainResults()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        var bound = await resultTask.BindAsync(async x => 
        {
            await Task.Yield();
            return Result<string>.Success(x.ToString());
        });

        // Assert
        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("42");
    }

    [Fact]
    public async Task BindAsync_Failure_ShouldPropagateError()
    {
        // Arrange
        var error = new Error("ERR", "Msg");
        var resultTask = Task.FromResult(Result<int>.Failure(error));

        // Act
        var bound = await resultTask.BindAsync(async x => 
        {
            await Task.Yield();
            return Result<string>.Success(x.ToString());
        });

        // Assert
        bound.IsFailure.Should().BeTrue();
        bound.Error.Should().Be(error);
    }

    [Fact]
    public async Task Bind_SyncBinder_ShouldChainResults()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        var bound = await resultTask.Bind(x => Result<string>.Success(x.ToString()));

        // Assert
        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be("42");
    }

    // ==================== TapAsync ====================

    [Fact]
    public async Task TapAsync_Success_ShouldExecuteAction()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        var executed = false;

        // Act
        await resultTask.TapAsync(async _ => 
        {
            await Task.Yield();
            executed = true;
        });

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task TapAsync_Failure_ShouldNotExecuteAction()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Failure("ERR", "Msg"));
        var executed = false;

        // Act
        await resultTask.TapAsync(async _ => 
        {
            await Task.Yield();
            executed = true;
        });

        // Assert
        executed.Should().BeFalse();
    }

    [Fact]
    public async Task Tap_SyncAction_ShouldExecuteAction()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));
        var executed = false;

        // Act
        await resultTask.Tap(_ => executed = true);

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task TapErrorAsync_Failure_ShouldExecuteAction()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Failure("ERR", "Msg"));
        var executed = false;

        // Act
        await resultTask.TapErrorAsync(async _ => 
        {
            await Task.Yield();
            executed = true;
        });

        // Assert
        executed.Should().BeTrue();
    }

    // ==================== EnsureAsync ====================

    [Fact]
    public async Task EnsureAsync_Valid_ShouldReturnSuccess()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(42));

        // Act
        var result = await resultTask.EnsureAsync(async x => 
        {
            await Task.Yield();
            return x > 0;
        }, "ERR", "Msg");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task EnsureAsync_Invalid_ShouldReturnFailure()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int>.Success(-1));

        // Act
        var result = await resultTask.EnsureAsync(async x => 
        {
            await Task.Yield();
            return x > 0;
        }, "ERR", "Msg");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ERR");
    }

    // ==================== Non-Generic Extensions ====================

    [Fact]
    public async Task BindAsync_NonGeneric_Success_ShouldChain()
    {
        // Arrange
        var resultTask = Task.FromResult(Result.Success());

        // Act
        var bound = await resultTask.BindAsync(async () => 
        {
            await Task.Yield();
            return Result.Success();
        });

        // Assert
        bound.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task BindAsync_NonGeneric_Generic_Success_ShouldChain()
    {
        // Arrange
        var resultTask = Task.FromResult(Result.Success());

        // Act
        var bound = await resultTask.BindAsync(async () => 
        {
            await Task.Yield();
            return Result<int>.Success(42);
        });

        // Assert
        bound.IsSuccess.Should().BeTrue();
        bound.Value.Should().Be(42);
    }

    [Fact]
    public async Task TapAsync_NonGeneric_Success_ShouldExecute()
    {
        // Arrange
        var resultTask = Task.FromResult(Result.Success());
        var executed = false;

        // Act
        await resultTask.TapAsync(async () => 
        {
            await Task.Yield();
            executed = true;
        });

        // Assert
        executed.Should().BeTrue();
    }
}
