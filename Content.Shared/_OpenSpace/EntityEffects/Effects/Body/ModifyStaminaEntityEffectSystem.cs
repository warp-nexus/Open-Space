using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Shared._OpenSpace.EntityEffects.Effects.Body;

public sealed partial class ModifyStaminaEntityEffectSystem : EntityEffectSystem<StaminaComponent, ModifyStamina>
{
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;

    protected override void Effect(Entity<StaminaComponent> entity, ref EntityEffectEvent<ModifyStamina> args)
    {
        _stamina.TakeStaminaDamage(entity.Owner, args.Effect.Amount * args.Scale, entity.Comp);
    }
}

public sealed partial class ModifyStamina : EntityEffectBase<ModifyStamina>
{
    [DataField]
    public float Amount = -1f;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("entity-effect-guidebook-modify-stamina",
            ("chance", Probability),
            ("amount", MathF.Abs(Amount)),
            ("deltasign", MathF.Sign(Amount)));
}
