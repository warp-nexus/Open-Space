using Robust.Shared.GameStates;

namespace Content.Shared._OpenSpace.Combat.Disarming.Components;

/// <summary>
/// Prevents the entity this is attached to from being disarmed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoDisarmComponent : Component
{
}
