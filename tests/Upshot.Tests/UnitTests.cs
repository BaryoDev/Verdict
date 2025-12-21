using FluentAssertions;
using Xunit;

namespace Upshot.Tests;

/// <summary>
/// Tests for Unit type.
/// </summary>
public class UnitTests
{
    [Fact]
    public void Unit_Value_ShouldReturnSingleton()
    {
        // Arrange & Act
        var unit1 = Unit.Value;
        var unit2 = Unit.Value;

        // Assert
        unit1.Should().Be(unit2);
    }

    [Fact]
    public void Unit_Equality_ShouldBeEqual()
    {
        // Arrange
        var unit1 = new Unit();
        var unit2 = new Unit();

        // Act & Assert
        unit1.Should().Be(unit2);
        (unit1 == unit2).Should().BeTrue();
    }

    [Fact]
    public void Unit_ToString_ShouldReturnUnit()
    {
        // Arrange
        var unit = Unit.Value;

        // Act
        var stringValue = unit.ToString();

        // Assert
        stringValue.Should().Be("()");
    }
}
