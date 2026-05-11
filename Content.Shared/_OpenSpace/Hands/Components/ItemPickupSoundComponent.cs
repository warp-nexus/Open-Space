using Content.Shared._OpenSpace.Hands.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._OpenSpace.Hands.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedItemPickupSoundSystem))]
public sealed partial class ItemPickupSoundComponent : Component
{
    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;
}
