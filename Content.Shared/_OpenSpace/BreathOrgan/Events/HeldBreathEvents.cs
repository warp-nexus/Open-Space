using Content.Shared.Alert;

namespace Content.Shared._OpenSpace.BreathOrgan.Events;

[ByRefEvent]
public record struct HeldBreathEvent(EntityUid Target);

[ByRefEvent]
public record struct HeldBreathEndAttemptEvent(bool Cancelled);

public sealed partial class HeldBreathAlertEvent : BaseAlertEvent;
