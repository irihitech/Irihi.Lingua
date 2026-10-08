using System.ComponentModel;

namespace Irihi.Lingua;

/// <summary>
/// A reference to an <see cref="INotifyPropertyChanged"/> property used by the
/// format combiner as a live argument: it subscribes the source's
/// <see cref="INotifyPropertyChanged.PropertyChanged"/> event directly and
/// re-reads the property through <see cref="Getter"/> on every matching
/// notification.  The property is read through the getter delegate instead of
/// reflection, keeping the API trimmer- and NativeAOT-friendly.
/// </summary>
internal sealed class PropertyChange
{
    public INotifyPropertyChanged Source { get; }

    public string PropertyName { get; }

    public Func<object?> Getter { get; }

    public PropertyChange(INotifyPropertyChanged source, string propertyName, Func<object?> getter)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        PropertyName = propertyName;
        Getter = getter ?? throw new ArgumentNullException(nameof(getter));
    }
}
