using Content.Shared._OpenSpace.Hands.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Lube;
using Content.Shared.Polymorph.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Shared._OpenSpace.Hands.Systems;

public sealed class SharedItemPickupSoundSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemPickupSoundComponent, InteractHandEvent>(
            OnInteractHand,
            before: [typeof(SharedItemSystem)],
            after: [typeof(SharedChameleonProjectorSystem)]);
    }

    private void OnInteractHand(Entity<ItemPickupSoundComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled
            || ent.Comp.Sound == null
            || HasComp<VirtualItemComponent>(ent.Owner)
            || TryComp<LubedComponent>(ent.Owner, out var lubed) && lubed.SlipsLeft > 0
            || !TryComp<HandsComponent>(args.User, out var hands)
            || hands.ActiveHandId == null
            || !TryComp<ItemComponent>(ent.Owner, out var item)
            || !_hands.CanPickupToHand(args.User, ent.Owner, hands.ActiveHandId, handsComp: hands, item: item))
        {
            return;
        }

        _audio.PlayPredicted(ent.Comp.Sound, ent.Owner, args.User);
    }
}
