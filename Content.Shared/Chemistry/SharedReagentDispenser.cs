using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    /// <summary>
    /// This class holds constants that are shared between client and server.
    /// </summary>
    public sealed class SharedReagentDispenser
    {
        public const string OutputSlotName = "beakerSlot";

        // open-space edit start
        public const float DefaultEnergyCapacity = 1000f;
        public const float DefaultRechargePerSecond = 2f;
        // open-space edit end
    }

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserSetDispenseAmountMessage : BoundUserInterfaceMessage
    {
        public readonly ReagentDispenserDispenseAmount ReagentDispenserDispenseAmount;

        public ReagentDispenserSetDispenseAmountMessage(ReagentDispenserDispenseAmount amount)
        {
            ReagentDispenserDispenseAmount = amount;
        }

        /// <summary>
        ///     Create a new instance from interpreting a String as an integer,
        ///     throwing an exception if it is unable to parse.
        /// </summary>
        public ReagentDispenserSetDispenseAmountMessage(String s)
        {
            switch (s)
            {
                case "1":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U1;
                    break;
                case "5":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U5;
                    break;
                case "10":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U10;
                    break;
                case "15":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U15;
                    break;
                case "20":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U20;
                    break;
                case "25":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U25;
                    break;
                case "30":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U30;
                    break;
                case "50":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U50;
                    break;
                case "100":
                    ReagentDispenserDispenseAmount = ReagentDispenserDispenseAmount.U100;
                    break;
                default:
                    throw new Exception($"Cannot convert the string `{s}` into a valid ReagentDispenser DispenseAmount");
            }
        }
    }

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserDispenseReagentMessage : BoundUserInterfaceMessage
    {
        // open-space edit start
        public readonly ReagentId ReagentId;

        public ReagentDispenserDispenseReagentMessage(ReagentId reagentId)
        {
            ReagentId = reagentId;
        }
        // open-space edit end
    }

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserContainerActionMessage : BoundUserInterfaceMessage
    {
        // open-space edit start
        public readonly ReagentId ReagentId;
        public readonly ReagentDispenserContainerAction Action;
        public readonly FixedPoint2 Quantity;

        public ReagentDispenserContainerActionMessage(ReagentId reagentId, ReagentDispenserContainerAction action, FixedPoint2 quantity = default)
        {
            ReagentId = reagentId;
            Action = action;
            Quantity = quantity;
        }
        // open-space edit end
    }

    // open-space edit start
    [Serializable, NetSerializable]
    public enum ReagentDispenserContainerAction : byte
    {
        Spill,
        SpillAll,
        Delete,
        Analyze,
    }
    // open-space edit end

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserClearContainerSolutionMessage : BoundUserInterfaceMessage
    {
    }

    public enum ReagentDispenserDispenseAmount
    {
        U1 = 1,
        U5 = 5,
        U10 = 10,
        U15 = 15,
        U20 = 20,
        U25 = 25,
        U30 = 30,
        U50 = 50,
        U100 = 100,
    }

    // open-space edit start
    [Serializable, NetSerializable]
    public sealed class ReagentDispenserInventoryItem(ReagentId reagentId, string reagentLabel, Color reagentColor)
    {
        public ReagentId ReagentId = reagentId;
        public string ReagentLabel = reagentLabel;
        public Color ReagentColor = reagentColor;
    }
    // open-space edit end

    [Serializable, NetSerializable]
    public sealed class ReagentDispenserBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly ContainerInfo? OutputContainer;

        public readonly NetEntity? OutputContainerEntity;

        /// <summary>
        /// A list of the reagents which this dispenser can dispense.
        /// </summary>
        // open-space edit start
        public readonly List<ReagentDispenserInventoryItem> Inventory;
        public readonly float CurrentEnergy;
        public readonly float MaxEnergy;
        // open-space edit end

        public readonly ReagentDispenserDispenseAmount SelectedDispenseAmount;

        public ReagentDispenserBoundUserInterfaceState(
            ContainerInfo? outputContainer,
            NetEntity? outputContainerEntity,
            List<ReagentDispenserInventoryItem> inventory,
            ReagentDispenserDispenseAmount selectedDispenseAmount,
            float currentEnergy,
            float maxEnergy)
        {
            OutputContainer = outputContainer;
            OutputContainerEntity = outputContainerEntity;
            Inventory = inventory;
            SelectedDispenseAmount = selectedDispenseAmount;
            CurrentEnergy = currentEnergy;
            MaxEnergy = maxEnergy;
        }
    }

    [Serializable, NetSerializable]
    public enum ReagentDispenserUiKey
    {
        Key
    }
}
