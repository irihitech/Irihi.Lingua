using Avalonia.Interactivity;

namespace Irihi.Luna.Lingua;

/// <summary>
/// Event arguments for <see cref="CulturePicker.CultureChangedEvent"/>,
/// carrying the newly selected <see cref="LinguaCulture"/>.
/// </summary>
public class CultureChangedEventArgs : RoutedEventArgs
{
    /// <summary>
    /// The newly selected culture, or <c>null</c> when selection is cleared.
    /// </summary>
    public LinguaCulture? Culture { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="CultureChangedEventArgs"/>.
    /// </summary>
    /// <param name="routedEvent">The routed event that was raised.</param>
    /// <param name="culture">The selected culture.</param>
    public CultureChangedEventArgs(RoutedEvent routedEvent, LinguaCulture? culture)
        : base(routedEvent)
    {
        Culture = culture;
    }
}
