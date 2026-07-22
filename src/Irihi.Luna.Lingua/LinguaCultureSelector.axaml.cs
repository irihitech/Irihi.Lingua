using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Irihi.Lingua;

namespace Irihi.Luna.Lingua;

/// <summary>
/// A user control that provides culture selection UI, driving one or more
/// <see cref="ILinguaManager"/> instances.
/// </summary>
/// <remarks>
/// The first manager in <see cref="Managers"/> is treated as the <em>primary</em>
/// manager: its <see cref="ILinguaManager.CultureChanges"/> stream is observed
/// to keep the selector in sync when the culture is changed externally.
/// All managers receive <see cref="ILinguaManager.UpdateCulture"/> when the
/// user makes a selection.
/// </remarks>
/// <example>
/// <strong>XAML inline:</strong>
/// <code><![CDATA[
/// <luna:LinguaCultureSelector AvailableCultures="{Binding SupportedCultures}">
///   <luna:LinguaCultureSelector.Managers>
///     <local:AppManager.Instance />
///     <local:PluginManager.Instance />
///   </luna:LinguaCultureSelector.Managers>
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
    /// Identifies the <see cref="AvailableCultures"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IReadOnlyList<CultureInfo>> AvailableCulturesProperty =
        AvaloniaProperty.Register<LinguaCultureSelector, IReadOnlyList<CultureInfo>>(
            nameof(AvailableCultures),
            defaultValue: new List<CultureInfo>());

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
    public IReadOnlyList<CultureInfo> AvailableCultures
    {
        get => GetValue(AvailableCulturesProperty);
        set => SetValue(AvailableCulturesProperty, value);
    }

    /// <summary>
    /// Gets or sets the index of the currently selected culture in <see cref="AvailableCultures"/>.
    /// </summary>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    private IDisposable? _primaryCultureSubscription;
    private bool _suppressSync;

    public LinguaCultureSelector()
    {
        SetCurrentValue(ManagersProperty, new List<ILinguaManager>());
        InitializeComponent();
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
            OnManagersChanged();
        }
        else if (change.Property == AvailableCulturesProperty)
        {
            SyncSelectedIndexFromPrimary();
        }
    }

    private void OnSelectedIndexChanged(int index)
    {
        if (_suppressSync)
            return;

        var managers = Managers;
        var cultures = AvailableCultures;

        if (managers is null || cultures is null || index < 0 || index >= cultures.Count)
            return;

        var culture = cultures[index];

        // Suppress the CultureChanges callback while we update managers,
        // avoiding a feedback loop.
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

        // Dispose previous subscription (if any).
        _primaryCultureSubscription?.Dispose();
        _primaryCultureSubscription = null;

        // Subscribe to the primary manager's CultureChanges.
        var primary = managers is not null && managers.Count > 0 ? managers[0] : null;
        if (primary is not null)
        {
            _primaryCultureSubscription = primary.CultureChanges.Subscribe(
                new CultureChangeObserver(this));
        }

        // Sync the selector against the (possibly new) primary manager.
        SyncSelectedIndexFromPrimary();
    }

    private void SyncSelectedIndexFromPrimary()
    {
        var managers = Managers;
        var cultures = AvailableCultures;

        var primary = managers is not null && managers.Count > 0 ? managers[0] : null;
        if (primary is null || cultures is null || cultures.Count == 0)
        {
            SelectedIndex = -1;
            return;
        }

        var current = primary.CurrentCulture;
        for (int i = 0; i < cultures.Count; i++)
        {
            if (cultures[i].Name == current.Name)
            {
                SelectedIndex = i;
                return;
            }
        }

        SelectedIndex = -1;
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
            // Ignore changes we triggered ourselves to avoid feedback loop.
            if (_owner._suppressSync)
                return;

            _owner.SyncSelectedIndexFromPrimary();
        }
    }
}
