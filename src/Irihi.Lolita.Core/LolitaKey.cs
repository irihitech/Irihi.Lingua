using System;

namespace Irihi.Lolita;

/// <summary>
/// Represents a strongly-typed resource key produced by the Lolita source generator.
/// Each instance carries both the raw string key and a reference to the
/// <see cref="ILolitaManager"/> that owns it.
/// </summary>
public readonly struct LolitaKey : IEquatable<LolitaKey>
{
    /// <summary>
    /// Gets the raw string resource key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the <see cref="ILolitaManager"/> that owns this key.
    /// </summary>
    /// <remarks>This value is never <c>null</c>; the constructor enforces non-null.</remarks>
    public ILolitaManager Manager { get; }

    /// <summary>
    /// Initializes a new <see cref="LolitaKey"/> with the given key and owning manager.
    /// </summary>
    /// <param name="key">The raw string resource key.</param>
    /// <param name="manager">The manager that owns this key.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is <c>null</c>.</exception>
    public LolitaKey(string key, ILolitaManager manager)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// Implicitly converts a <see cref="LolitaKey"/> to its raw string key.
    /// </summary>
    public static implicit operator string(LolitaKey lolitaKey) => lolitaKey.Key;

    /// <inheritdoc/>
    public override string ToString() => Key;

    /// <summary>
    /// Determines equality based on the <see cref="Key"/> string and manager identity.
    /// </summary>
    public bool Equals(LolitaKey other) =>
        string.Equals(Key, other.Key, StringComparison.Ordinal) &&
        ReferenceEquals(Manager, other.Manager);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is LolitaKey other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            return ((Key?.GetHashCode() ?? 0) * 397) ^ Manager.GetHashCode();
        }
    }

    /// <summary>Determines whether two <see cref="LolitaKey"/> values are equal.</summary>
    public static bool operator ==(LolitaKey left, LolitaKey right) => left.Equals(right);

    /// <summary>Determines whether two <see cref="LolitaKey"/> values are not equal.</summary>
    public static bool operator !=(LolitaKey left, LolitaKey right) => !left.Equals(right);
}
