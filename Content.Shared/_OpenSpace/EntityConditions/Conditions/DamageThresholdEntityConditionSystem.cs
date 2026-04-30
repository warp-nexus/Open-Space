using Content.Shared.EntityConditions;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._OpenSpace.EntityConditions.Conditions;

public sealed partial class TotalDamageConditionSystem : EntityConditionSystem<DamageableComponent, TotalDamageCondition>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    protected override void Condition(Entity<DamageableComponent> entity, ref EntityConditionEvent<TotalDamageCondition> args)
    {
        #pragma warning disable CS0618
        var damage = _damageable.GetTotalDamage((entity.Owner, entity.Comp));
        #pragma warning restore CS0618
        args.Result = damage >= args.Condition.Min && damage <= args.Condition.Max;
    }
}

public sealed partial class DamageTypeConditionSystem : EntityConditionSystem<DamageableComponent, DamageTypeCondition>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    protected override void Condition(Entity<DamageableComponent> entity, ref EntityConditionEvent<DamageTypeCondition> args)
    {
        #pragma warning disable CS0618
        var damage = _damageable.GetAllDamage((entity.Owner, entity.Comp)).DamageDict.GetValueOrDefault(args.Condition.Type, FixedPoint2.Zero);
        #pragma warning restore CS0618
        args.Result = damage >= args.Condition.Min && damage <= args.Condition.Max;
    }
}

public sealed partial class DamageGroupConditionSystem : EntityConditionSystem<DamageableComponent, DamageGroupCondition>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    protected override void Condition(Entity<DamageableComponent> entity, ref EntityConditionEvent<DamageGroupCondition> args)
    {
        #pragma warning disable CS0618
        var damage = _damageable.GetDamagePerGroup((entity.Owner, entity.Comp)).GetValueOrDefault(args.Condition.Group, FixedPoint2.Zero);
        #pragma warning restore CS0618
        args.Result = damage >= args.Condition.Min && damage <= args.Condition.Max;
    }
}

public sealed partial class TotalDamageCondition : EntityConditionBase<TotalDamageCondition>
{
    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype) =>
        Loc.GetString("entity-condition-guidebook-total-damage",
            ("min", Min.Float()),
            ("max", Max == FixedPoint2.MaxValue ? int.MaxValue : Max.Float()));
}

public sealed partial class DamageTypeCondition : EntityConditionBase<DamageTypeCondition>
{
    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField(required: true)]
    public ProtoId<DamageTypePrototype> Type;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        if (!prototype.Resolve(Type, out var typeProto))
            return string.Empty;

        return Loc.GetString("entity-condition-guidebook-type-damage",
            ("type", typeProto.LocalizedName),
            ("min", Min.Float()),
            ("max", Max == FixedPoint2.MaxValue ? int.MaxValue : Max.Float()));
    }
}

public sealed partial class DamageGroupCondition : EntityConditionBase<DamageGroupCondition>
{
    [DataField]
    public FixedPoint2 Min = FixedPoint2.Zero;

    [DataField]
    public FixedPoint2 Max = FixedPoint2.MaxValue;

    [DataField(required: true)]
    public ProtoId<DamageGroupPrototype> Group;

    public override string EntityConditionGuidebookText(IPrototypeManager prototype)
    {
        if (!prototype.Resolve(Group, out var groupProto))
            return string.Empty;

        return Loc.GetString("entity-condition-guidebook-group-damage",
            ("type", groupProto.LocalizedName),
            ("min", Min.Float()),
            ("max", Max == FixedPoint2.MaxValue ? int.MaxValue : Max.Float()));
    }
}
