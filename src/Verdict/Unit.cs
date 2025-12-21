using System;

namespace Verdict;

/// <summary>
/// Represents a void value, following the F# convention.
/// Used as a marker type for operations with no return value.
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>
{
    /// <summary>
    /// Gets the singleton instance of Unit.
    /// </summary>
    public static readonly Unit Value = default;

    /// <summary>
    /// Determines whether two Unit instances are equal.
    /// </summary>
    public bool Equals(Unit other) => true;

    /// <summary>
    /// Determines whether the specified object is equal to the current Unit.
    /// </summary>
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    public override int GetHashCode() => 0;

    /// <summary>
    /// Returns a string representation of Unit.
    /// </summary>
    public override string ToString() => "()";

    /// <summary>
    /// Compares this instance to another Unit.
    /// </summary>
    public int CompareTo(Unit other) => 0;

    /// <summary>
    /// Determines whether two Unit instances are equal.
    /// </summary>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>
    /// Determines whether two Unit instances are not equal.
    /// </summary>
    public static bool operator !=(Unit left, Unit right) => false;
}
