using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Primitives;
using Irihi.Avalonia.Shared.Common;
using Irihi.Lingua;

namespace Irihi.Luna.Lingua;

/// <summary>
/// A user control that provides culture selection UI via a dropdown button,
/// driving one or more <see cref="ILinguaManager"/> instances.
/// </summary>
public class CulturePicker : TemplatedControl
{
    public static readonly StyledProperty<IList<ILinguaManager>?> ManagersProperty =
        AvaloniaProperty.Register<CulturePicker, IList<ILinguaManager>?>(nameof(Managers));

    public static readonly StyledProperty<IList<LinguaCulture>?> CulturesProperty =
        AvaloniaProperty.Register<CulturePicker, IList<LinguaCulture>?>(nameof(Cultures));

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<CulturePicker, int>(nameof(SelectedIndex), defaultValue: -1);

    public static readonly StyledProperty<LinguaCulture?> SelectedItemProperty =
        AvaloniaProperty.Register<CulturePicker, LinguaCulture?>(nameof(SelectedItem));

    /// <summary>
    /// Command executed when a culture is selected from the dropdown.
    /// Bound from the XAML MenuItem style.
    /// </summary>
    internal ICommand? SelectionCommand { get; set; }

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

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
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

        SelectionCommand = new IRIHI_CommandBase<LinguaCulture>(OnCultureSelected);
    }

    private void OnCultureSelected(LinguaCulture? culture)
    {
        if (culture is null) return;

        var cultures = Cultures;
        if (cultures is null) return;

        for (int i = 0; i < cultures.Count; i++)
        {
            if (ReferenceEquals(cultures[i], culture))
            {
                SelectedIndex = i;
                return;
            }
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SelectedIndexProperty)
        {
            var index = change.GetNewValue<int>();
            OnSelectedIndexChanged(index);
            SyncSelectedItem(index);
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
            SyncSelectedIndexFromPrimary();
        }
    }

    private void OnSelectedIndexChanged(int index)
    {
        if (_suppressSync)
            return;

        var managers = Managers;
        var cultures = Cultures;

        if (managers is null || cultures is null || index < 0 || index >= cultures.Count)
            return;

        var culture = cultures[index].Culture;

        _suppressSync = true;
        try
        {
            foreach (var manager in managers)
            {
                manager?.UpdateCulture(culture);
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

        SyncSelectedIndexFromPrimary();
    }

    private void OnManagersCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnManagersChanged();
    }

    private void OnCulturesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SyncSelectedIndexFromPrimary();
    }

    private void SyncSelectedItem(int index)
    {
        var cultures = Cultures;
        if (cultures is not null && index >= 0 && index < cultures.Count)
            SetCurrentValue(SelectedItemProperty, cultures[index]);
        else
            SetCurrentValue(SelectedItemProperty, null);
    }

    private void SyncSelectedIndexFromPrimary()
    {
        var managers = Managers;
        var cultures = Cultures;

        var primary = managers is not null && managers.Count > 0 ? managers[0] : null;
        if (primary is null || cultures is null || cultures.Count == 0)
        {
            SetCurrentValue(SelectedIndexProperty, -1);
            return;
        }

        var current = primary.CurrentCulture;
        for (int i = 0; i < cultures.Count; i++)
        {
            if (cultures[i].Culture.Name == current.Name)
            {
                SetCurrentValue(SelectedIndexProperty, i);
                return;
            }
        }

        SetCurrentValue(SelectedIndexProperty, -1);
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

    private sealed class CultureChangeObserver : IObserver<CultureInfo>
    {
        private readonly CulturePicker _owner;

        public CultureChangeObserver(CulturePicker owner) => _owner = owner;

        public void OnCompleted() { }
        public void OnError(Exception error) { }

        public void OnNext(CultureInfo value)
        {
            if (_owner._suppressSync) return;
            _owner.SyncSelectedIndexFromPrimary();
        }
    }
}
