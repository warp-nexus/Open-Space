using Content.Shared.Decals;
using Content.Shared.Maps;
using Robust.Shared.Prototypes;

namespace Content.Client.Mapping;

/// <summary>
///     Used to represent a button's data in the mapping editor.
/// </summary>
public sealed class MappingPrototype
{
    /// <summary>
    ///     The prototype instance, if any.
    ///     Can be one of <see cref="EntityPrototype"/>, <see cref="ContentTileDefinition"/> or <see cref="DecalPrototype"/>
    ///     If null, this is a top-level button (such as Entities, Tiles or Decals)
    /// </summary>
    public readonly IPrototype? Prototype;

    /// <summary>
    ///     The text to display on the UI for this button.
    /// </summary>
    public readonly string Name;

    // open-space edit start
    /// <summary>
    ///     Extra searchable text for localized or aliased prototype names.
    /// </summary>
    public readonly string SearchText;
    // open-space edit end

    /// <summary>
    ///     Which other prototypes (buttons) this one is nested inside of.
    /// </summary>
    public List<MappingPrototype>? Parents;

    /// <summary>
    ///     Which other prototypes (buttons) are nested inside this one.
    /// </summary>
    public List<MappingPrototype>? Children;

    // open-space edit start
    public MappingPrototype(IPrototype? prototype, string name, string? searchText = null)
    // open-space edit end
    {
        Prototype = prototype;
        Name = name;
        // open-space edit start
        SearchText = searchText ?? name;
        // open-space edit end
    }
}
