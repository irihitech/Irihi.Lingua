using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Irihi.Lingua;

namespace Irihi.Luna.Lingua;

/// <summary>
/// A user control that provides culture selection UI via a dropdown button,
/// driving one or more <see cref="ILinguaManager"/> instances.
/// </summary>
/// <remarks>
/// The first manager in <see cref="Managers"/> is treated as the <em>primary</em>
/// manager: its <see cref="ILinguaManager.CultureChanges"/> stream is observed
/// to keep the selector in sync when the culture is changed externally.
/// All managers receive <see cref="ILinguaManager.UpdateCulture"/> when the
/// user makes a selection.
/// </remarks>
/// <example>
/// <strong>XAML (inline cultures + managers):</strong>
/// <code><![CDATA[
/// <luna:LinguaCultureSelector>
///   <luna:LinguaCultureSelector.Managers>
///     <local:AppManager.Instance />
///   </luna:LinguaCultureSelector.Managers>
///   <luna:LinguaCultureSelector.Cultures>
///     <luna:LinguaCulture>zh-Hans</luna:LinguaCulture>
///     <luna:LinguaCulture DisplayName="English">en</luna:LinguaCulture>
///   </luna:LinguaCultureSelector.Cultures>
/// </luna:LinguaCultureSelector>
/// ]]></code>
/// </example>
public partial class LinguaCultureSelector : UserControl
{
    /// <summary>
    /// Identifies the <see cref="Managers"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IList<ILinguaManager>?> ManagersProperty =
        AvaloniaProperty.Register<LinguaCultureSelector, IList<ILinguaManager>?>(nameof(Managers));

    /// <summary>
    /// Identifies the <see cref="Cultures"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IList<LinguaCulture>?> CulturesProperty =
        AvaloniaProperty.Register<LinguaCultureSelector, IList<LinguaCulture>?>(nameof(Cultures));

    /// <summary>
    /// Identifies the <see cref="SelectedIndex"/> styled property.
    /// </summary>
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<LinguaCultureSelector, int>(nameof(SelectedIndex), defaultValue: -1);

    /// <summary>
    /// Gets or sets the collection of <see cref="ILinguaManager"/> instances to control.
    /// The first manager is treated as the primary — its <see cref="ILinguaManager.CultureChanges"/>
    /// stream is observed, and its <see cref="ILinguaManager.CurrentCulture"/> seeds the
    /// initial selection.
    /// </summary>
    public IList<ILinguaManager>? Managers
    {
        get => GetValue(ManagersProperty);
        set => SetValue(ManagersProperty, value);
    }

    /// <summary>
    /// Gets or sets the list of cultures available for selection.
    /// </summary>
    public IList<LinguaCulture>? Cultures
    {
        get => GetValue(CulturesProperty);
        set => SetValue(CulturesProperty, value);
    }

    /// <summary>
    /// Gets or sets the index of the currently selected culture in <see cref="Cultures"/>.
    /// </summary>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    private readonly MenuFlyout _cultureMenuFlyout;
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

        _cultureMenuFlyout = new MenuFlyout();
        CultureButton.Flyout = _cultureMenuFlyout;

        InitializeComponent();

        CultureButton.Content = _defaultButtonContent;
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
            RebuildMenu();
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
        RebuildMenu();
    }

    private void RebuildMenu()
    {
        var cultures = Cultures;
        var menu = _cultureMenuFlyout;

        // Unhook old item clicks before clearing.
        foreach (MenuItem? item in menu.Items)
        {
            if (item is not null)
                item.Click -= OnMenuItemClick;
        }

        menu.Items.Clear();

        if (cultures is null || cultures.Count == 0)
        {
            CultureButton.Content = _defaultButtonContent;
            return;
        }

        for (int i = 0; i < cultures.Count; i++)
        {
            var culture = cultures[i];
            var menuItem = new MenuItem
            {
                Header = culture.DisplayText,
                Tag = i
            };
            menuItem.Click += OnMenuItemClick;
            menu.Items.Add(menuItem);
        }

        SyncSelectedIndexFromPrimary();
    }

    private void OnMenuItemClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is int index)
        {
            SelectedIndex = index;
        }
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
                UpdateMenuCheck(i);
                return;
            }
        }

        SetCurrentValue(SelectedIndexProperty, -1);
        CultureButton.Content = _defaultButtonContent;
    }

    private void UpdateMenuCheck(int selectedIndex)
    {
        var items = _cultureMenuFlyout.Items;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] is MenuItem mi)
            {
                mi.IsChecked = i == selectedIndex;
            }
        }
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

        public CultureChangeObserver(LinguaCultureSelector owner)
        {
            _owner = owner;
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(CultureInfo value)
        {
            if (_owner._suppressSync)
                return;

            _owner.SyncSelectedIndexFromPrimary();
        }
    }
}
