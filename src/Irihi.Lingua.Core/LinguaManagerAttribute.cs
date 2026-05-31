namespace Irihi.Lingua;

/// <summary>
/// Marks a static partial class for i18n code generation.
/// The source generator will populate the class with <see cref="System.IObservable{T}"/>
/// properties for each resource key and an <c>UpdateCulture</c> method.
/// </summary>
/// <remarks>
/// Apply this attribute to a <c>static partial class</c> and provide the relative path
/// to the base <c>.resx</c> file.  Culture-specific variants (e.g.
/// <c>Strings.zh-Hans.resx</c>) placed alongside the base file and included as
/// <c>AdditionalFiles</c> in the project are picked up automatically.
/// </remarks>
/// <example>
/// <code>
/// [LinguaManager("./Resources/Strings.resx")]
/// public static partial class LanguageManager { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class LinguaManagerAttribute : Attribute
{
    /// <summary>
    /// Gets the relative path to the base <c>.resx</c> resource file.
    /// </summary>
    public string ResourcePath { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="LinguaManagerAttribute"/>.
    /// </summary>
    /// <param name="resourcePath">Relative path to the base <c>.resx</c> file (e.g. <c>./Resources/Strings.resx</c>).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="resourcePath"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    public LinguaManagerAttribute(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            throw new ArgumentException("Resource path must not be null, empty, or whitespace.", nameof(resourcePath));
        }

        ResourcePath = resourcePath;
    }
}
