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
    /// Each <see cref="LinguaCulture"/> wraps a <see cref="CultureInfo"/>
    /// with an optional custom <see cref="LinguaCulture.DisplayName"/>.
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

    private IDisposable? _primaryCultureSubscription;
    private bool _suppressSync;

    public LinguaCultureSelector()
    {
        SetCurrentValue(ManagersProperty, new List<ILinguaManager>());
        SetCurrentValue(CulturesProperty, new List<LinguaCulture>());
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
        else if (change.Property == CulturesProperty)
        {
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

    private void SyncSelectedIndexFromPrimary()
    {
        var managers = Managers;
        var cultures = Cultures;

        var primary = managers is not null && managers.Count > 0 ? managers[0] : null;
        if (primary is null || cultures is null || cultures.Count == 0)
        {
            SelectedIndex = -1;
            return;
        }

        var current = primary.CurrentCulture;
        for (int i = 0; i < cultures.Count; i++)
        {
            if (cultures[i].Culture.Name == current.Name)
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
            if (_owner._suppressSync)
                return;

            _owner.SyncSelectedIndexFromPrimary();
        }
    }
}
