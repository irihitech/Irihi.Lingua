using System;

namespace Irihi.Lolita;

/// <summary>
/// Configures i18n code generation for a .resx resource file.
/// Applied at the assembly level to customize how the source generator
/// produces i18n accessor types.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class GenerateI18nAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the resource file (without extension).
    /// </summary>
    public string ResourceName { get; }

    /// <summary>
    /// Gets or sets the namespace for the generated i18n class.
    /// Defaults to <c>Irihi.Lolita.Generated</c> when not specified.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Initializes a new instance of <see cref="GenerateI18nAttribute"/>.
    /// </summary>
    /// <param name="resourceName">The name of the resource file (without extension).</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="resourceName"/> is <c>null</c>, empty, or whitespace.
    /// </exception>
    public GenerateI18nAttribute(string resourceName)
    {
        if (string.IsNullOrWhiteSpace(resourceName))
        {
            throw new ArgumentException("Resource name must not be null, empty, or whitespace.", nameof(resourceName));
        }

        ResourceName = resourceName;
    }
}
