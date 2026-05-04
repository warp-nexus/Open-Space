using Content.Shared.Damage;

namespace Content.Shared._OpenSpace.Medical.Damage.Events;

public sealed class DamageBeforeApplyEvent : EntityEventArgs
{
    public required DamageSpecifier Damage { get; set; }
    public EntityUid? Origin { get; init; }
    public bool Cancelled { get; set; }
}
