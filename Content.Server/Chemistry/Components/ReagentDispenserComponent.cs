using Content.Shared.Whitelist;
using Content.Shared.Containers.ItemSlots;
using Content.Server.Chemistry.EntitySystems;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.Components
{
    /// <summary>
    /// A machine that dispenses reagents into a solution container from containers in its storage slots.
    /// </summary>
    [RegisterComponent]
    [Access(typeof(ReagentDispenserSystem))]
    public sealed partial class ReagentDispenserComponent : Component
    {
        [DataField]
        public ItemSlot BeakerSlot = new();

        // open-space edit start
        [DataField]
        public List<ProtoId<ReagentPrototype>> DispensedReagents = new();

        [DataField]
        public float EnergyCapacity = SharedReagentDispenser.DefaultEnergyCapacity;

        [DataField]
        public float RechargePerSecond = SharedReagentDispenser.DefaultRechargePerSecond;

        [DataField]
        public float CurrentEnergy = SharedReagentDispenser.DefaultEnergyCapacity;

        [ViewVariables]
        public float RechargeAccumulator;
        // open-space edit end

        [DataField("clickSound"), ViewVariables(VVAccess.ReadWrite)]
        public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

        [ViewVariables(VVAccess.ReadWrite)]
        public ReagentDispenserDispenseAmount DispenseAmount = ReagentDispenserDispenseAmount.U10;
    }
}
