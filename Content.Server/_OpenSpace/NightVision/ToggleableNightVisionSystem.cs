using Content.Shared._OpenSpace.NightVision;
using Content.Shared._OpenSpace.NightVision.Components;
using Content.Shared._OpenSpace.NightVision.Events;
using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Server._OpenSpace.NightVision;

public sealed class ToggleableNightVisionSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableNightVisionComponent, ComponentInit>(OnVisionInit);
        SubscribeLocalEvent<ToggleableNightVisionComponent, ComponentShutdown>(OnVisionShutdown);
        SubscribeLocalEvent<ToggleableNightVisionComponent, ToggleNightVisionEvent>(OnToggleNightVision);
    }

    private void OnVisionInit(Entity<ToggleableNightVisionComponent> ent, ref ComponentInit args)
    {
        _actionsSystem.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnVisionShutdown(Entity<ToggleableNightVisionComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Comp.ActionEntity);
        RemComp<NightVisionComponent>(ent);
    }

    private void OnToggleNightVision(Entity<ToggleableNightVisionComponent> ent, ref ToggleNightVisionEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.Active = !ent.Comp.Active;

        if (ent.Comp.Active)
            ToggleOn(ent, ent.Comp);
        else
            RemComp<NightVisionComponent>(ent);

        args.Handled = true;
    }

    private void ToggleOn(EntityUid uid, ToggleableNightVisionComponent comp)
    {
        EnsureComp<NightVisionComponent>(uid, out var vision);

        vision.Effect = comp.Effect;
    }
}
