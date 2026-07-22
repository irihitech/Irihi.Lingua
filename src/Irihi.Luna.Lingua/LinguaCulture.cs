using System.ComponentModel;
using System.Globalization;
using Avalonia.Metadata;

namespace Irihi.Luna.Lingua;

/// <summary>
/// A culture entry for <see cref="LinguaCultureSelector"/>, pairing a
/// <see cref="CultureInfo"/> with an optional custom display name.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="CultureName"/> property is marked <see cref="ContentAttribute"/>
/// so that XAML inline text is treated as a culture identifier:
/// <code>&lt;luna:LinguaCulture&gt;zh-Hans&lt;/luna:LinguaCulture&gt;</code>
/// </para>
/// <para>
/// When <see cref="DisplayName"/> is <c>null</c>, <see cref="DisplayText"/>
/// falls back to <see cref="CultureInfo.NativeName"/>.
/// </para>
/// </remarks>
[TypeConverter(typeof(LinguaCultureTypeConverter))]
public class LinguaCulture
{
    /// <summary>
    /// A pre-defined <see cref="LinguaCulture"/> entry for
    /// <see cref="CultureInfo.InvariantCulture"/>, displayed as "English".
    /// </summary>
    public static readonly LinguaCulture InvariantCulture = new()
    {
        Culture = CultureInfo.InvariantCulture,
        DisplayName = "English"
    };
    /// <summary>
    /// Gets or sets the culture identifier string.
    /// This is the <see cref="ContentAttribute">Content</see> property — XAML
    /// inner text such as <c>&lt;LinguaCulture&gt;en&lt;/LinguaCulture&gt;</c>
    /// maps here and is parsed into <see cref="Culture"/>.
    /// </summary>
    [Content]
    public string CultureName
    {
        get => Culture.Name;
        set => Culture = string.IsNullOrEmpty(value)
            ? CultureInfo.InvariantCulture
            : new CultureInfo(value);
    }

    /// <summary>
    /// Gets or sets the <see cref="CultureInfo"/> for this entry.
    /// </summary>
    public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;

    /// <summary>
    /// Gets or sets an optional custom display name.
    /// When <c>null</c>, <see cref="DisplayText"/> uses <see cref="CultureInfo.NativeName"/>.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets the text to display in the selector UI —
    /// <see cref="DisplayName"/> if set, otherwise <see cref="CultureInfo.NativeName"/>.
    /// </summary>
    public string DisplayText => DisplayName ?? Culture.NativeName;
}

/// <summary>
/// Type converter that allows writing <c>&lt;LinguaCulture&gt;zh-Hans&lt;/LinguaCulture&gt;</c>
/// in XAML as a shorthand for a <see cref="LinguaCulture"/> whose
/// <see cref="LinguaCulture.Culture"/> is parsed from the string.
/// </summary>
public class LinguaCultureTypeConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
    }

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is string s)
            return new LinguaCulture { CultureName = s };

        return base.ConvertFrom(context, culture, value);
    }
}
