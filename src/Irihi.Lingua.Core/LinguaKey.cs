namespace Irihi.Lingua;

/// <summary>
/// Represents a resource key together with a reference to the
/// <see cref="ILinguaManager"/> singleton that owns it.
/// </summary>
/// <remarks>
/// Instances of this type are generated as <c>public static readonly</c>
/// members of the nested <c>Keys</c> class inside every manager class
/// annotated with <see cref="LinguaManagerAttribute"/>.  They can be used
/// wherever a plain <see cref="string"/> key would be used, because an
/// implicit conversion to <see cref="string"/> is provided.
/// </remarks>
public sealed class LinguaKey
{
    /// <summary>Gets the raw string resource key.</summary>
    public string Key { get; }

    /// <summary>
    /// Gets the <see cref="ILinguaManager"/> singleton instance that contains
    /// this key.
    /// </summary>
    public ILinguaManager Manager { get; }

    /// <summary>
    /// Initializes a new <see cref="LinguaKey"/>.
    /// </summary>
    /// <param name="key">The raw resource key string.</param>
    /// <param name="manager">The manager singleton that owns this key.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="key"/> or <paramref name="manager"/> is <c>null</c>.
    /// </exception>
    public LinguaKey(string key, ILinguaManager manager)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        Manager = manager ?? throw new ArgumentNullException(nameof(manager));
    }

    /// <summary>
    /// Implicitly converts a <see cref="LinguaKey"/> to its underlying string key,
    /// allowing it to be used wherever a plain <see cref="string"/> is expected.
    /// Returns <c>null</c> when the input is <c>null</c>.
    /// </summary>
    public static implicit operator string?(LinguaKey? linguaKey) => linguaKey?.Key;

    /// <inheritdoc/>
    public override string ToString() => Key;
}
