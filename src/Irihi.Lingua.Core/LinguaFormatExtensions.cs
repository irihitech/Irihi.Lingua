namespace Irihi.Lingua;

/// <summary>
/// Provides the code-side counterpart of the XAML <c>FormatTranslate</c> markup
/// extension: it combines a localized format template with dynamic arguments
/// into a single observable string.
/// </summary>
/// <remarks>
/// <para>
/// The returned observable behaves like a behavior subject: subscribing
/// immediately emits the formatted result computed from the current values,
/// and every subsequent change re-emits a recomputed string.  A recompute is
/// triggered whenever the format template changes (e.g. because the active
/// culture changed), whenever one of the dynamic arguments changes, and
/// whenever the manager's active culture changes.
/// </para>
/// <para>
/// Formatting always uses the owning manager's
/// <see cref="ILinguaManager.CurrentCulture"/> rather than the current thread
/// culture, so number and date formatting stays consistent with the language
/// selected through the manager.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public IObservable&lt;string?&gt; PageText =&gt;
///     LanguageManager.Keys.Page_Template.Format(
///         this.Property(nameof(Page), () =&gt; Page),
///         this.Property(nameof(TotalPages), () =&gt; TotalPages));
/// </code>
/// </example>
public static class LinguaFormatExtensions
{
    /// <summary>
    /// Formats the localized template identified by <paramref name="formatKey"/>
    /// with the given arguments and returns the result as an observable string.
    /// </summary>
    /// <param name="formatKey">
    /// The <see cref="LinguaKey"/> of the format template (e.g.
    /// <c>LanguageManager.Instance.Keys.Page_Template</c> for a template like
    /// <c>"Page {0} of {1}"</c>).  Its manager supplies both the template text
    /// and the culture used for formatting.
    /// </param>
    /// <param name="args">
    /// Zero or more arguments.  Each argument is one of:
    /// <list type="bullet">
    ///   <item>a constant value;</item>
    ///   <item>any <see cref="IObservable{T}"/> that supplies the argument
    ///   dynamically — for example another resource-key observable such as
    ///   <c>LanguageManager.Instance.SomeKey</c>;</item>
    ///   <item>a live property reference created with
    ///   <see cref="PropertyChangeExtensions.Property"/> — the combiner
    ///   subscribes the source's <c>PropertyChanged</c> event directly.</item>
    /// </list>
    /// Observable arguments are recognized covariantly, so
    /// <c>IObservable&lt;int&gt;</c> and friends work without manual boxing.
    /// </param>
    /// <returns>
    /// An <see cref="IObservable{T}"/> of string with behavior-subject semantics
    /// that re-formats whenever the template, any dynamic argument, or the
    /// manager's active culture changes.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="formatKey"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the manager of <paramref name="formatKey"/> does not contain
    /// an observable for the key.
    /// </exception>
    public static IObservable<string?> Format(this LinguaKey formatKey, params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(formatKey);

        var format = formatKey.Manager.GetObservable(formatKey.Key)
            ?? throw new ArgumentException(
                $"The manager does not contain an observable for the resource key '{formatKey.Key}'.",
                nameof(formatKey));

        // No CultureChanges subscription: UpdateCulture pushes every key
        // observable after switching CurrentCulture, so the template push
        // alone re-triggers the recompute on culture changes.
        return new LinguaFormatObservable(formatKey.Manager, format, args, subscribeCultureChanges: false);
    }

    /// <summary>
    /// Formats the given format-template observable with the given arguments
    /// and returns the result as an observable string, using
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
    /// <param name="args">
    /// See <see cref="Format(LinguaKey,object?[])"/> for the accepted argument kinds.
    /// </param>
    /// <returns>
    /// An <see cref="IObservable{T}"/> of string with behavior-subject semantics.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="manager"/> or <paramref name="format"/> is <c>null</c>.
    /// </exception>
    public static IObservable<string?> Format(
        this ILinguaManager manager,
        IObservable<string?> format,
        params object?[] args)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(format);

        return new LinguaFormatObservable(manager, format, args, subscribeCultureChanges: true);
    }
}
