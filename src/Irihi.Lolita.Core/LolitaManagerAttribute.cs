using System;

namespace Irihi.Lolita;

/// <summary>
/// Marks a partial class for i18n code generation as a singleton.
/// The source generator will populate the class with a static <c>Instance</c>
/// property, <see cref="System.IObservable{T}"/> instance properties for each
/// resource key, and a static <c>UpdateCulture</c> method.
/// </summary>
/// <remarks>
/// Apply this attribute to a <c>partial class</c> (not static) and provide the relative path
/// to the base <c>.resx</c> file.  Culture-specific variants (e.g.
/// <c>Strings.zh-Hans.resx</c>) placed alongside the base file and included as
/// <c>AdditionalFiles</c> in the project are picked up automatically.
/// <para>
/// The generated <c>Instance</c> singleton can be used for direct XAML bindings,
/// for example in Avalonia via <c>{x:Static local:LanguageManager.Instance}</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [LolitaManager("./Resources/Strings.resx")]
/// public partial class LanguageManager { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class LolitaManagerAttribute : Attribute
{
    /// <summary>
    /// Gets the relative path to the base <c>.resx</c> resource file.
    /// </summary>
    public string ResourcePath { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="LolitaManagerAttribute"/>.
    /// </summary>
    /// <param name="resourcePath">Relative path to the base <c>.resx</c> file (e.g. <c>./Resources/Strings.resx</c>).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="resourcePath"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    public LolitaManagerAttribute(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            throw new ArgumentException("Resource path must not be null, empty, or whitespace.", nameof(resourcePath));
        }

        ResourcePath = resourcePath;
    }
}
