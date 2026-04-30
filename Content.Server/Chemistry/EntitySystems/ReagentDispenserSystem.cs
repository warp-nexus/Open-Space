using System;
using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Paper;

namespace Content.Server.Chemistry.EntitySystems
{
    /// <summary>
    /// Contains all the server-side logic for reagent dispensers.
    /// <seealso cref="ReagentDispenserComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ReagentDispenserSystem : EntitySystem
    {
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        [Dependency] private readonly PuddleSystem _puddle = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly PaperSystem _paper = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ReagentDispenserComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ReagentDispenserComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserSetDispenseAmountMessage>(OnSetDispenseAmountMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserDispenseReagentMessage>(OnDispenseReagentMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserClearContainerSolutionMessage>(OnClearContainerSolutionMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ReagentDispenserContainerActionMessage>(OnContainerActionMessage);
            SubscribeLocalEvent<ReagentDispenserComponent, ChemPrintAnalysisMessage>(OnPrintAnalysisMessage);

            SubscribeLocalEvent<ReagentDispenserComponent, MapInitEvent>(OnMapInit, before: new[] { typeof(ItemSlotsSystem) });
        }

        // open-space edit start
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ReagentDispenserComponent>();
            while (query.MoveNext(out var uid, out var dispenser))
            {
                if (dispenser.CurrentEnergy >= dispenser.EnergyCapacity)
                    continue;

                dispenser.RechargeAccumulator += frameTime;
                var updated = false;
                while (dispenser.RechargeAccumulator >= 0.5f && dispenser.CurrentEnergy < dispenser.EnergyCapacity)
                {
                    dispenser.RechargeAccumulator -= 0.5f;
                    dispenser.CurrentEnergy = MathF.Min(dispenser.EnergyCapacity, dispenser.CurrentEnergy + 1f);
                    updated = true;
                }

                if (updated)
                    UpdateUiState((uid, dispenser));
            }
        }
        // open-space edit end

        private void SubscribeUpdateUiState<T>(Entity<ReagentDispenserComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
        }

        private void UpdateUiState(Entity<ReagentDispenserComponent> reagentDispenser)
        {
            var outputContainer = _itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedReagentDispenser.OutputSlotName);
            var outputContainerInfo = BuildOutputContainerInfo(outputContainer);

            var inventory = GetInventory(reagentDispenser);

            var state = new ReagentDispenserBoundUserInterfaceState(
                outputContainerInfo,
                GetNetEntity(outputContainer),
                inventory,
                reagentDispenser.Comp.DispenseAmount,
                reagentDispenser.Comp.CurrentEnergy,
                reagentDispenser.Comp.EnergyCapacity);
            _userInterfaceSystem.SetUiState(reagentDispenser.Owner, ReagentDispenserUiKey.Key, state);
        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out _, out var solution))
            {
                return new ContainerInfo(Name(container.Value), solution.Volume, solution.MaxVolume)
                {
                    Reagents = solution.Contents
                };
            }

            return null;
        }

        // open-space edit start
        private List<ReagentDispenserInventoryItem> GetInventory(Entity<ReagentDispenserComponent> reagentDispenser)
        {
            var inventory = new List<ReagentDispenserInventoryItem>();
            foreach (var reagentId in reagentDispenser.Comp.DispensedReagents)
            {
                if (!_prototypeManager.TryIndex(reagentId, out ReagentPrototype? proto))
                    continue;

                inventory.Add(new ReagentDispenserInventoryItem(new ReagentId(proto.ID, null), proto.LocalizedName, proto.SubstanceColor));
            }

            return inventory;
        }
        // open-space edit end

        private void OnSetDispenseAmountMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserSetDispenseAmountMessage message)
        {
            reagentDispenser.Comp.DispenseAmount = message.ReagentDispenserDispenseAmount;
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnDispenseReagentMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserDispenseReagentMessage message)
        {
            if (!_prototypeManager.TryIndex(message.ReagentId.Prototype, out ReagentPrototype? _))
                return;

            if (!TryGetOutputSolution(reagentDispenser, out _, out var outputSolution))
            {
                _popup.PopupCursor(Loc.GetString("reagent-dispenser-window-no-beaker-text"), message.Actor);
                return;
            }

            if (!reagentDispenser.Comp.DispensedReagents.Contains(message.ReagentId.Prototype))
                return;

            var amount = FixedPoint2.New((int) reagentDispenser.Comp.DispenseAmount);
            if (reagentDispenser.Comp.CurrentEnergy < amount.Float())
            {
                _popup.PopupCursor(Loc.GetString("reagent-dispenser-window-no-energy-text"), message.Actor);
                UpdateUiState(reagentDispenser);
                return;
            }

            if (!_solutionContainerSystem.TryAddReagent(outputSolution, message.ReagentId, amount, out var accepted))
            {
                _popup.PopupCursor(Loc.GetString("reagent-dispenser-window-container-full-text"), message.Actor);
                UpdateUiState(reagentDispenser);
                return;
            }

            reagentDispenser.Comp.CurrentEnergy = MathF.Max(0f, reagentDispenser.Comp.CurrentEnergy - accepted.Float());
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnContainerActionMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserContainerActionMessage message)
        {
            if (!TryGetOutputSolution(reagentDispenser, out _, out var outputSolution))
                return;

            if (message.Action == ReagentDispenserContainerAction.Analyze)
            {
                SendAnalysisPopup(reagentDispenser.Owner, ReagentDispenserUiKey.Key, message.Actor, message.ReagentId);
                return;
            }

            var solution = outputSolution.Comp.Solution;
            var available = solution.GetReagentQuantity(message.ReagentId);
            if (available <= 0)
                return;

            var amount = message.Action == ReagentDispenserContainerAction.SpillAll
                ? available
                : FixedPoint2.Min(available, message.Quantity);

            switch (message.Action)
            {
                case ReagentDispenserContainerAction.Spill:
                case ReagentDispenserContainerAction.SpillAll:
                    _solutionContainerSystem.RemoveReagent(outputSolution, message.ReagentId, amount);
                    var spilled = new Solution(1)
                    {
                        Temperature = solution.Temperature
                    };
                    spilled.AddReagent(message.ReagentId, amount);
                    _puddle.TrySpillAt(Transform(reagentDispenser).Coordinates, spilled, out _);
                    break;
                case ReagentDispenserContainerAction.Delete:
                    _solutionContainerSystem.RemoveReagent(outputSolution, message.ReagentId, available);
                    break;
            }

            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnClearContainerSolutionMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ReagentDispenserClearContainerSolutionMessage message)
        {
            if (!TryGetOutputSolution(reagentDispenser, out _, out var outputSolution))
                return;

            _solutionContainerSystem.RemoveAllSolution(outputSolution);
            UpdateUiState(reagentDispenser);
            ClickSound(reagentDispenser);
        }

        private void OnPrintAnalysisMessage(Entity<ReagentDispenserComponent> reagentDispenser, ref ChemPrintAnalysisMessage message)
        {
            PrintAnalysis(message.Actor, message.ReagentId);
        }

        private void ClickSound(Entity<ReagentDispenserComponent> reagentDispenser)
        {
            _audioSystem.PlayPvs(reagentDispenser.Comp.ClickSound, reagentDispenser, AudioParams.Default.WithVolume(-2f));
        }

        /// <summary>
        /// Initializes the beaker slot
        /// </summary>
        private void OnMapInit(Entity<ReagentDispenserComponent> ent, ref MapInitEvent args)
        {
            _itemSlotsSystem.AddItemSlot(ent.Owner, SharedReagentDispenser.OutputSlotName, ent.Comp.BeakerSlot);
            ent.Comp.CurrentEnergy = Math.Clamp(ent.Comp.CurrentEnergy, 0f, ent.Comp.EnergyCapacity);
        }

        private bool TryGetOutputSolution(
            Entity<ReagentDispenserComponent> reagentDispenser,
            out EntityUid outputContainer,
            out Entity<SolutionComponent> outputSolution)
        {
            outputSolution = default;
            if (_itemSlotsSystem.GetItemOrNull(reagentDispenser, SharedReagentDispenser.OutputSlotName) is not { Valid: true } output)
            {
                outputContainer = EntityUid.Invalid;
                return false;
            }

            if (!_solutionContainerSystem.TryGetFitsInDispenser(output, out Entity<SolutionComponent>? solutionEntity, out _)
                || solutionEntity == null)
            {
                outputContainer = EntityUid.Invalid;
                return false;
            }

            outputContainer = output;
            outputSolution = solutionEntity.Value;

            return true;
        }

        private void SendAnalysisPopup(EntityUid owner, Enum uiKey, EntityUid user, ReagentId reagentId)
        {
            if (!_prototypeManager.TryIndex(reagentId.Prototype, out ReagentPrototype? proto))
                return;

            _userInterfaceSystem.ServerSendUiMessage(owner, uiKey, new ChemReagentAnalysisPopupMessage(reagentId, proto.LocalizedName, proto.LocalizedDescription), user);
        }

        private void PrintAnalysis(EntityUid user, ReagentId reagentId)
        {
            if (!_prototypeManager.TryIndex(reagentId.Prototype, out ReagentPrototype? proto))
                return;

            var printed = Spawn("Paper", Transform(user).Coordinates);
            _hands.PickupOrDrop(user, printed, checkActionBlocker: false);

            if (!TryComp<PaperComponent>(printed, out var paperComp))
                return;

            _metaData.SetEntityName(printed, Loc.GetString("chem-analysis-paper-name", ("reagent", proto.LocalizedName)));

            var text =
                $"{Loc.GetString("chem-analysis-paper-title", ("reagent", proto.LocalizedName))}\n\n" +
                $"{Loc.GetString("chem-analysis-paper-name-line", ("reagent", proto.LocalizedName))}\n" +
                $"{Loc.GetString("chem-analysis-paper-description-line", ("description", proto.LocalizedDescription))}\n\n" +
                $"{Loc.GetString("chem-analysis-paper-notes-line")}";

            _paper.SetContent((printed, paperComp), text);
        }
    }
}
