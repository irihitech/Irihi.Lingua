namespace Irihi.Lingua;

/// <summary>
/// Entry points for the code-side counterpart of the XAML <c>FormatTranslate</c>
/// markup extension: a fluent builder that combines a localized format template
/// with dynamic arguments into a single observable string.
/// </summary>
/// <remarks>
/// <para>
/// See <see cref="LinguaFormatBuilder"/> for the argument overloads and the
/// observable semantics.
/// </para>
/// </remarks>
public static class LinguaFormatExtensions
{
    /// <summary>
    /// Starts a format chain for the localized template identified by
    /// <paramref name="formatKey"/>.  Its manager supplies both the template
    /// text and the culture used for formatting.
    /// </summary>
    /// <remarks>
    /// The chain does not subscribe the manager's culture stream:
    /// <c>UpdateCulture</c> pushes every key observable after switching the
    /// culture, so the template push alone re-triggers the recompute.  One
    /// boundary to know about: if a culture variant omits this key (reported
    /// as <c>LINGUA002</c> at build time), switching culture does not push the
    /// template and the number formatting for this entry stays on the previous
    /// culture until the template next changes — call
    /// <see cref="LinguaFormatObservable.Refresh"/> to force a recompute with
    /// the current culture.
    /// </remarks>
    /// <param name="formatKey">
    /// The <see cref="LinguaKey"/> of the format template (e.g.
    /// <c>LanguageManager.Keys.Page_Template</c> for a template like
    /// <c>"Page {0} of {1}"</c>).
    /// </param>
    /// <example>
    /// <code>
    /// public IObservable&lt;string?&gt; PageText =&gt;
    ///     LanguageManager.Keys.Page_Template.CreateFormat()
    ///         .Arg(this, nameof(Page), s =&gt; s.Page)
    ///         .Arg(this, nameof(TotalPages), s =&gt; s.TotalPages)
    ///         .Build();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="formatKey"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the manager of <paramref name="formatKey"/> does not contain
    /// an observable for the key.
    /// </exception>
    public static LinguaFormatBuilder CreateFormat(this LinguaKey formatKey)
    {
        ArgumentNullException.ThrowIfNull(formatKey);

        var format = formatKey.Manager.GetObservable(formatKey.Key)
            ?? throw new ArgumentException(
                $"The manager does not contain an observable for the resource key '{formatKey.Key}'.",
                nameof(formatKey));

        // No CultureChanges subscription: UpdateCulture pushes every key
        // observable after switching CurrentCulture, so the template push
        // alone re-triggers the recompute on culture changes.
        return new LinguaFormatBuilder(formatKey.Manager, format, subscribeCultureChanges: false);
    }

    /// <summary>
    /// Starts a format chain with a custom format-template observable, using
    /// <paramref name="manager"/>'s active culture for formatting.
    /// </summary>
    /// <param name="manager">
    /// The manager whose <see cref="ILinguaManager.CurrentCulture"/> is used for
    /// formatting and whose <see cref="ILinguaManager.CultureChanges"/> stream
    /// triggers recomputes (needed because a custom template source does not
    /// react to culture changes on its own).
    /// </param>
    /// <param name="format">
    /// The format template source (e.g. a resource-key observable or
    /// <see cref="LinguaObservableString.FromLiteral"/> for a hard-coded template).
    /// </param>
    /// <remarks>
    /// For templates that come from the manager's own resources, prefer
    /// <see cref="CreateFormat(LinguaKey)"/> — it starts from the template key and
    /// avoids a redundant culture subscription.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="manager"/> or <paramref name="format"/> is <c>null</c>.
    /// </exception>
    public static LinguaFormatBuilder CreateFormat(
        this ILinguaManager manager,
        IObservable<string?> format)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(format);

        return new LinguaFormatBuilder(manager, format, subscribeCultureChanges: true);
    }

    /// <summary>
    /// Starts a format chain with a constant format template, using
    /// <paramref name="manager"/>'s active culture for formatting.
    /// </summary>
    /// <param name="manager">
    /// The manager whose <see cref="ILinguaManager.CurrentCulture"/> is used for
    /// formatting and whose <see cref="ILinguaManager.CultureChanges"/> stream
    /// triggers recomputes.
    /// </param>
    /// <param name="format">The constant template, e.g. <c>"Page {0} of {1}"</c>.</param>
    /// <example>
    /// <code>
    /// LinguaManager.Instance.CreateFormat("Page {0} of {1}")
    ///     .Arg(this, nameof(Page), s =&gt; s.Page)
    ///     .Build();
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="manager"/> or <paramref name="format"/> is <c>null</c>.
    /// </exception>
    public static LinguaFormatBuilder CreateFormat(
        this ILinguaManager manager,
        string format)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(format);

        return new LinguaFormatBuilder(
            manager,
            LinguaObservableString.FromLiteral(format),
            subscribeCultureChanges: true);
    }
}
