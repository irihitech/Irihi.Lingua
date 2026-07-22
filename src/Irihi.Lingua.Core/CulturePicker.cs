using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Primitives;
using Irihi.Lingua;

namespace Irihi.Luna.Lingua;

/// <summary>
/// A TemplatedControl that provides culture selection UI via a ComboBox,
/// driving one or more <see cref="ILinguaManager"/> instances.
/// </summary>
public class CulturePicker : TemplatedControl
{
    public static readonly StyledProperty<IList<ILinguaManager>?> ManagersProperty =
        AvaloniaProperty.Register<CulturePicker, IList<ILinguaManager>?>(nameof(Managers));

    public static readonly StyledProperty<IList<LinguaCulture>?> CulturesProperty =
        AvaloniaProperty.Register<CulturePicker, IList<LinguaCulture>?>(nameof(Cultures));

    public static readonly StyledProperty<LinguaCulture?> SelectedItemProperty =
        AvaloniaProperty.Register<CulturePicker, LinguaCulture?>(nameof(SelectedItem));

    public IList<ILinguaManager>? Managers
    {
        get => GetValue(ManagersProperty);
        set => SetValue(ManagersProperty, value);
    }

    public IList<LinguaCulture>? Cultures
    {
        get => GetValue(CulturesProperty);
        set => SetValue(CulturesProperty, value);
    }

    public LinguaCulture? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    private IDisposable? _primaryCultureSubscription;
    private INotifyCollectionChanged? _observedManagers;
    private INotifyCollectionChanged? _observedCultures;
    private bool _suppressSync;

    public CulturePicker()
    {
        var defaultManagers = new ObservableCollection<ILinguaManager>();
        var defaultCultures = new ObservableCollection<LinguaCulture>();

        SetCurrentValue(ManagersProperty, defaultManagers);
        SetCurrentValue(CulturesProperty, defaultCultures);

        WireCollectionChanged(defaultManagers, ref _observedManagers, OnManagersCollectionChanged);
        WireCollectionChanged(defaultCultures, ref _observedCultures, OnCulturesCollectionChanged);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedItemProperty)
        {
            OnSelectedItemChanged(change.GetNewValue<LinguaCulture?>());
        }
        else if (change.Property == ManagersProperty)
        {
            var newList = change.GetNewValue<IList<ILinguaManager>?>();
            if (!ReferenceEquals(change.GetOldValue<IList<ILinguaManager>?>(), newList))
                WireCollectionChanged(newList, ref _observedManagers, OnManagersCollectionChanged);
            OnManagersChanged();
        }
        else if (change.Property == CulturesProperty)
        {
            var newList = change.GetNewValue<IList<LinguaCulture>?>();
            if (!ReferenceEquals(change.GetOldValue<IList<LinguaCulture>?>(), newList))
                WireCollectionChanged(newList, ref _observedCultures, OnCulturesCollectionChanged);
            SyncSelectedItemFromPrimary();
        }
    }

    private void OnSelectedItemChanged(LinguaCulture? item)
    {
        if (_suppressSync || item is null)
            return;

        var managers = Managers;
        if (managers is null) return;

        _suppressSync = true;
        try
        {
            foreach (var manager in managers)
            {
                manager.UpdateCulture(item.Culture);
            }
        }
        finally
        {
            _suppressSync = false;
        }
    }

    private void OnManagersChanged()
    {
        var managers = Managers;

        _primaryCultureSubscription?.Dispose();
        _primaryCultureSubscription = null;

        var primary = managers is not null && managers.Count > 0 ? managers[0] : null;
        if (primary is not null)
        {
            _primaryCultureSubscription = primary.CultureChanges.Subscribe(
                new CultureChangeObserver(this));
        }

        SyncSelectedItemFromPrimary();
    }

    private void OnManagersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnManagersChanged();
    }

    private void OnCulturesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncSelectedItemFromPrimary();
    }

    private void SyncSelectedItemFromPrimary()
    {
        var managers = Managers;
        var cultures = Cultures;

        var primary = managers is not null && managers.Count > 0 ? managers[0] : null;
        if (primary is null || cultures is null || cultures.Count == 0)
        {
            SetCurrentValue(SelectedItemProperty, null);
            return;
        }

        var current = primary.CurrentCulture;
        foreach (var item in cultures)
        {
            if (!Equals(item.Culture, current)) continue;
            SetCurrentValue(SelectedItemProperty, item);
            return;
        }

        SetCurrentValue(SelectedItemProperty, null);
    }

    private static void WireCollectionChanged<T>(
        IList<T>? newList,
        ref INotifyCollectionChanged? field,
        NotifyCollectionChangedEventHandler handler)
    {
        if (ReferenceEquals(field, newList))
            return;

        if (field is not null)
            field.CollectionChanged -= handler;

        field = newList as INotifyCollectionChanged;

        if (field is not null)
            field.CollectionChanged += handler;
    }

    private sealed class CultureChangeObserver(CulturePicker owner) : IObserver<CultureInfo>
    {
        public void OnCompleted() { }
        public void OnError(Exception error) { }

        public void OnNext(CultureInfo value)
        {
            if (owner._suppressSync) return;
            owner.SyncSelectedItemFromPrimary();
        }
    }
}
