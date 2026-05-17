using System.Globalization;

namespace Irihi.Lolita;

/// <summary>
/// Defines the contract for an i18n manager produced by the
/// <see cref="LolitaManagerAttribute"/> source generator.
/// </summary>
/// <remarks>
/// Each generated static partial class exposes a <c>Current</c> static field
/// that implements this interface, allowing keys (<see cref="LolitaKey"/>) to
/// hold a typed reference back to their owning manager.
/// </remarks>
public interface ILolitaManager
{
    /// <summary>
    /// Switches all observable properties to the values for <paramref name="culture"/>.
    /// Falls back to the parent culture, then to the default (invariant) culture.
    /// </summary>
    /// <param name="culture">The target culture. <c>null</c> is treated as <see cref="CultureInfo.InvariantCulture"/>.</param>
    void UpdateCulture(CultureInfo culture);
}
