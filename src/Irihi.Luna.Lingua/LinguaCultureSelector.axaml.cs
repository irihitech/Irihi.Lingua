using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Irihi.Avalonia.Shared.Common;
using Irihi.Lingua;

namespace Irihi.Luna.Lingua;

/// <summary>
/// A user control that provides culture selection UI via a dropdown button,
/// driving one or more <see cref="ILinguaManager"/> instances.
/// </summary>
public partial class LinguaCultureSelector : UserControl
{
    public static readonly StyledProperty<IList<ILinguaManager>?> ManagersProperty =
        AvaloniaProperty.Register<LinguaCultureSelector, IList<ILinguaManager>?>(nameof(Managers));

    public static readonly StyledProperty<IList<LinguaCulture>?> CulturesProperty =
        AvaloniaProperty.Register<LinguaCultureSelector, IList<LinguaCulture>?>(nameof(Cultures));

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<LinguaCultureSelector, int>(nameof(SelectedIndex), defaultValue: -1);

    public static readonly StyledProperty<ICommand?> SelectionCommandProperty =
        AvaloniaProperty.Register<LinguaCultureSelector, ICommand?>(nameof(SelectionCommand));

    internal ICommand? SelectionCommand
    {
        get => GetValue(SelectionCommandProperty);
        set => SetValue(SelectionCommandProperty, value);
    }

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

    private IDisposable? _primaryCultureSubscription;
    private INotifyCollectionChanged? _observedManagers;
    private INotifyCollectionChanged? _observedCultures;
    private bool _suppressSync;
    private string _defaultButtonContent = "Select culture";

    public LinguaCultureSelector()
    {
        var defaultManagers = new ObservableCollection<ILinguaManager>();
        var defaultCultures = new ObservableCollection<LinguaCulture>();

        SetCurrentValue(ManagersProperty, defaultManagers);
        SetCurrentValue(CulturesProperty, defaultCultures);

        WireCollectionChanged(defaultManagers, ref _observedManagers, OnManagersCollectionChanged);
        WireCollectionChanged(defaultCultures, ref _observedCultures, OnCulturesCollectionChanged);

        SetCurrentValue(SelectionCommandProperty, new IRIHI_CommandBase<LinguaCulture>(OnCultureSelected));

        InitializeComponent();

        CultureButton.Content = _defaultButtonContent;
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
            OnSelectedIndexChanged(change.GetNewValue<int>());
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

    private void SyncSelectedIndexFromPrimary()
    {
        var managers = Managers;
        var cultures = Cultures;

        var primary = managers is not null && managers.Count > 0 ? managers[0] : null;
        if (primary is null || cultures is null || cultures.Count == 0)
        {
            SetCurrentValue(SelectedIndexProperty, -1);
            CultureButton.Content = _defaultButtonContent;
            return;
        }

        var current = primary.CurrentCulture;
        for (int i = 0; i < cultures.Count; i++)
        {
            if (cultures[i].Culture.Name == current.Name)
            {
                SetCurrentValue(SelectedIndexProperty, i);
                CultureButton.Content = cultures[i].DisplayText;
                return;
            }
        }

        SetCurrentValue(SelectedIndexProperty, -1);
        CultureButton.Content = _defaultButtonContent;
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
        private readonly LinguaCultureSelector _owner;

        public CultureChangeObserver(LinguaCultureSelector owner) => _owner = owner;

        public void OnCompleted() { }
        public void OnError(Exception error) { }

        public void OnNext(CultureInfo value)
        {
            if (_owner._suppressSync) return;
            _owner.SyncSelectedIndexFromPrimary();
        }
    }
}
