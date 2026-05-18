namespace Irihi.Lolita;

/// <summary>
/// Represents a resource key together with a reference to the
/// <see cref="ILolitaManager"/> singleton that owns it.
/// </summary>
/// <remarks>
/// Instances of this type are generated as <c>public static readonly</c>
/// members of the nested <c>Keys</c> class inside every manager class
/// annotated with <see cref="LolitaManagerAttribute"/>.  They can be used
/// wherever a plain <see cref="string"/> key would be used, because an
/// implicit conversion to <see cref="string"/> is provided.
/// </remarks>
public sealed class LolitaKey
{
    /// <summary>Gets the raw string resource key.</summary>
    public string Key { get; }

    /// <summary>
    /// Gets the <see cref="ILolitaManager"/> singleton instance that contains
    /// this key.
    /// </summary>
    public ILolitaManager Manager { get; }

    /// <summary>
    /// Initializes a new <see cref="LolitaKey"/>.
    /// </summary>
    /// <param name="key">The raw resource key string.</param>
    /// <param name="manager">The manager singleton that owns this key.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key"/> or <paramref name="manager"/> is <c>null</c>.
    /// </exception>
    public LolitaKey(string key, ILolitaManager manager)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// Implicitly converts a <see cref="LolitaKey"/> to its underlying string key,
    /// allowing it to be used wherever a plain <see cref="string"/> is expected.
    /// Returns <c>null</c> when the input is <c>null</c>.
    /// </summary>
    public static implicit operator string?(LolitaKey? lolitaKey) => lolitaKey?.Key;

    /// <inheritdoc/>
    public override string ToString() => Key;
}
