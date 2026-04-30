using Content.Server.Chemistry.Components;
using System;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.Storage;
using JetBrains.Annotations;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Paper;

namespace Content.Server.Chemistry.EntitySystems
{

    /// <summary>
    /// Contains all the server-side logic for ChemMasters.
    /// <seealso cref="ChemMasterComponent"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemMasterSystem : EntitySystem
    {
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainerSystem = default!;
        [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly StorageSystem _storageSystem = default!;
        [Dependency] private readonly LabelSystem _labelSystem = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly PaperSystem _paper = default!;
        [Dependency] private readonly MetaDataSystem _metaData = default!;

        private static readonly EntProtoId PillPrototypeId = "Pill";

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ChemMasterComponent, ComponentStartup>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, SolutionContainerChangedEvent>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, EntInsertedIntoContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, EntRemovedFromContainerMessage>(SubscribeUpdateUiState);
            SubscribeLocalEvent<ChemMasterComponent, BoundUIOpenedEvent>(SubscribeUpdateUiState);

            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetModeMessage>(OnSetModeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterSetPillTypeMessage>(OnSetPillTypeMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterReagentAmountButtonMessage>(OnReagentButtonMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterCustomAmountMessage>(OnCustomAmountMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterMoveBufferToContainerMessage>(OnMoveBufferToContainerMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterEjectBeakerAndClearBufferMessage>(OnEjectBeakerAndClearBufferMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterEjectBeakerMessage>(OnEjectBeakerMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemMasterCreatePillsMessage>(OnCreatePillsMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemAnalyzeReagentMessage>(OnAnalyzeReagentMessage);
            SubscribeLocalEvent<ChemMasterComponent, ChemPrintAnalysisMessage>(OnPrintAnalysisMessage);
        }

        private void SubscribeUpdateUiState<T>(Entity<ChemMasterComponent> ent, ref T ev)
        {
            UpdateUiState(ent);
        }

        private void UpdateUiState(Entity<ChemMasterComponent> ent)
        {
            var (owner, chemMaster) = ent;
            if (!_solutionContainerSystem.TryGetSolution(owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                return;
            var inputContainer = _itemSlotsSystem.GetItemOrNull(owner, SharedChemMaster.InputSlotName);
            var outputContainer = _itemSlotsSystem.GetItemOrNull(owner, SharedChemMaster.OutputSlotName);

            var bufferReagents = bufferSolution.Contents;
            var bufferCurrentVolume = bufferSolution.Volume;

            var state = new ChemMasterBoundUserInterfaceState(
                BuildInputContainerInfo(inputContainer),
                BuildOutputContainerInfo(outputContainer),
                bufferReagents,
                bufferCurrentVolume,
                // open-space edit start
                chemMaster.Mode,
                // open-space edit end
                chemMaster.PillType,
                chemMaster.PillDosageLimit);

            _userInterfaceSystem.SetUiState(owner, ChemMasterUiKey.Key, state);
        }

        private void OnSetModeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSetModeMessage message)
        {
            // Ensure the mode is valid, either Transfer or Discard.
            if (!Enum.IsDefined(typeof(ChemMasterMode), message.ChemMasterMode))
                return;

            chemMaster.Comp.Mode = message.ChemMasterMode;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnCycleSortingTypeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSortingTypeCycleMessage message)
        {
            chemMaster.Comp.SortingType++;
            if (chemMaster.Comp.SortingType > ChemMasterSortingType.Latest)
                chemMaster.Comp.SortingType = ChemMasterSortingType.None;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnSetPillTypeMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterSetPillTypeMessage message)
        {
            // Ensure valid pill type. There are 20 pills selectable, 0-19.
            if (message.PillType > SharedChemMaster.PillTypes - 1)
                return;

            chemMaster.Comp.PillType = message.PillType;
            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnReagentButtonMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterReagentAmountButtonMessage message)
        {
            // Ensure the amount corresponds to one of the reagent amount buttons.
            if (!Enum.IsDefined(typeof(ChemMasterReagentAmount), message.Amount))
                return;

            // open-space edit start
            if (message.FromBuffer)
                DoBufferAction(chemMaster, message.ReagentId, message.Amount.GetFixedPoint());
            else
                TransferReagents(chemMaster, message.ReagentId, message.Amount.GetFixedPoint(), false);
            // open-space edit end

            ClickSound(chemMaster);
        }

        // open-space edit start
        private void OnCustomAmountMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterCustomAmountMessage message)
        {
            if (message.Amount <= 0)
                return;

            if (message.FromBuffer)
                DoBufferAction(chemMaster, message.ReagentId, message.Amount);
            else
                TransferReagents(chemMaster, message.ReagentId, message.Amount, false);

            ClickSound(chemMaster);
        }
        // open-space edit end

        private void TransferReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer)
        {
            var container = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName);
            if (container is null ||
                !_solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSoln, out var containerSolution) ||
                !_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
            {
                return;
            }

            if (fromBuffer) // Buffer to container
            {
                amount = FixedPoint2.Min(amount, containerSolution.AvailableVolume);
                amount = bufferSolution.RemoveReagent(id, amount, preserveOrder: true);
                _solutionContainerSystem.TryAddReagent(containerSoln.Value, id, amount, out var _);
            }
            else // Container to buffer
            {
                amount = FixedPoint2.Min(amount, containerSolution.GetReagentQuantity(id));
                _solutionContainerSystem.RemoveReagent(containerSoln.Value, id, amount);
                bufferSolution.AddReagent(id, amount);
            }

            UpdateUiState(chemMaster);
        }

        private void DiscardReagents(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount, bool fromBuffer)
        {
            if (fromBuffer)
            {
                if (_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                    bufferSolution.RemoveReagent(id, amount, preserveOrder: true);
                else
                    return;
            }
            else
            {
                var container = _itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName);
                if (container is not null &&
                    _solutionContainerSystem.TryGetFitsInDispenser(container.Value, out var containerSolution, out _))
                {
                    _solutionContainerSystem.RemoveReagent(containerSolution.Value, id, amount);
                }
                else
                    return;
            }

            UpdateUiState(chemMaster);
        }

        // open-space edit start
        private void DoBufferAction(Entity<ChemMasterComponent> chemMaster, ReagentId id, FixedPoint2 amount)
        {
            switch (chemMaster.Comp.Mode)
            {
                case ChemMasterMode.Transfer:
                    TransferReagents(chemMaster, id, amount, true);
                    break;
                case ChemMasterMode.Discard:
                    DiscardReagents(chemMaster, id, amount, true);
                    break;
            }
        }
        // open-space edit end

        // open-space edit start
        private void OnMoveBufferToContainerMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterMoveBufferToContainerMessage message)
        {
            if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out var bufferSolution))
                return;

            foreach (var reagent in bufferSolution.Contents.ToArray())
            {
                TransferReagents(chemMaster, reagent.Reagent, reagent.Quantity, true);
            }

            ClickSound(chemMaster);
        }

        private void OnEjectBeakerAndClearBufferMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterEjectBeakerAndClearBufferMessage message)
        {
            if (_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out var bufferEnt, out _))
                _solutionContainerSystem.RemoveAllSolution(bufferEnt.Value);

            if (_itemSlotsSystem.TryGetSlot(chemMaster.Owner, SharedChemMaster.InputSlotName, out var itemSlot))
                _itemSlotsSystem.TryEjectToHands(chemMaster.Owner, itemSlot, message.Actor, true);

            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnEjectBeakerMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterEjectBeakerMessage message)
        {
            if (_itemSlotsSystem.TryGetSlot(chemMaster.Owner, SharedChemMaster.InputSlotName, out var itemSlot))
                _itemSlotsSystem.TryEjectToHands(chemMaster.Owner, itemSlot, message.Actor, true);

            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private void OnAnalyzeReagentMessage(Entity<ChemMasterComponent> chemMaster, ref ChemAnalyzeReagentMessage message)
        {
            SendAnalysisPopup(chemMaster.Owner, ChemMasterUiKey.Key, message.Actor, message.ReagentId);
        }

        private void OnPrintAnalysisMessage(Entity<ChemMasterComponent> chemMaster, ref ChemPrintAnalysisMessage message)
        {
            PrintAnalysis(message.Actor, message.ReagentId);
        }
        // open-space edit end

        private void OnCreatePillsMessage(Entity<ChemMasterComponent> chemMaster, ref ChemMasterCreatePillsMessage message)
        {
            var user = message.Actor;
            // open-space edit start
            if (message.Dosage == 0 || message.Dosage > chemMaster.Comp.PillDosageLimit)
                return;

            if (message.Label.Length > SharedChemMaster.LabelMaxLength)
                return;

            if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out var bufferEnt, out var bufferSolution))
                return;

            var number = (uint) Math.Floor(bufferSolution.Volume.Double() / message.Dosage);
            if (number == 0)
            {
                _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-low-text"), user);
                return;
            }

            var withdrawal = bufferSolution.SplitSolution(FixedPoint2.New((int) (number * message.Dosage)));
            _solutionContainerSystem.UpdateChemicals(bufferEnt.Value);

            var coordinates = Transform(user).Coordinates;
            for (var i = 0; i < number; i++)
            {
                var item = Spawn(PillPrototypeId, coordinates);
                _solutionContainerSystem.EnsureSolutionEntity(item,
                    SharedChemMaster.PillSolutionName,
                    out var itemSolution,
                    message.Dosage);
                if (!itemSolution.HasValue)
                    return;

                _solutionContainerSystem.TryAddSolution(itemSolution.Value, withdrawal.SplitSolution(message.Dosage));
                _labelSystem.Label(item, message.Label);

                var pill = EnsureComp<PillComponent>(item);
                pill.PillType = chemMaster.Comp.PillType;
                Dirty(item, pill);

                // Log pill creation by a user
                _adminLogger.Add(LogType.Action, LogImpact.Low,
                    $"{ToPrettyString(user):user} printed {ToPrettyString(item):pill} {SharedSolutionContainerSystem.ToPrettyString(itemSolution.Value.Comp.Solution)}");
            }
            // open-space edit end

            UpdateUiState(chemMaster);
            ClickSound(chemMaster);
        }

        private bool WithdrawFromSource(
            Entity<ChemMasterComponent> chemMaster,
            FixedPoint2 neededVolume,
            EntityUid? user,
            [NotNullWhen(returnValue: true)] out Solution? outputSolution)
        {
            outputSolution = null;

            Solution? solution;
            Entity<SolutionComponent>? soln = null;

            switch (chemMaster.Comp.DrawSource)
            {
                case ChemMasterDrawSource.Internal:
                    if (!_solutionContainerSystem.TryGetSolution(chemMaster.Owner, SharedChemMaster.BufferSolutionName, out _, out solution))
                        return false;

                    if (solution.Volume == 0)
                    {
                        if (user is { } uid)
                            _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-empty-text"), uid);

                        return false;
                    }
                    if (neededVolume > solution.Volume)
                    {
                        if (user is { } uid)
                            _popupSystem.PopupCursor(Loc.GetString("chem-master-window-buffer-low-text"), uid);

                        return false;
                    }

                    break;

                case ChemMasterDrawSource.External:
                    if (_itemSlotsSystem.GetItemOrNull(chemMaster, SharedChemMaster.InputSlotName) is not {} container)
                    {
                        if (user.HasValue)
                            _popupSystem.PopupCursor(Loc.GetString("chem-master-window-no-beaker-text"), user.Value);
                        return false;
                    }

                    if (!_solutionContainerSystem.TryGetFitsInDispenser(container, out soln, out solution))
                        return false;

                    if (solution.Volume == 0)
                    {
                        if (user is { } uid)
                            _popupSystem.PopupCursor(Loc.GetString("chem-master-window-beaker-empty-text"), uid);

                        return false;
                    }
                    if (neededVolume > solution.Volume)
                    {
                        if (user is { } uid)
                            _popupSystem.PopupCursor(Loc.GetString("chem-master-window-beaker-low-text"), uid);

                        return false;
                    }

                    break;

                default:
                    return false;
            }

            outputSolution = solution.SplitSolution(neededVolume);

            if (soln.HasValue)
                _solutionContainerSystem.UpdateChemicals(soln.Value);

            return true;
        }

        private void ClickSound(Entity<ChemMasterComponent> chemMaster)
        {
            _audioSystem.PlayPvs(chemMaster.Comp.ClickSound, chemMaster, AudioParams.Default.WithVolume(-2f));
        }

        private ContainerInfo? BuildInputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            if (!TryComp(container, out FitsInDispenserComponent? fits)
                || !_solutionContainerSystem.TryGetSolution(container.Value, fits.Solution, out _, out var solution))
            {
                return null;
            }

            return BuildContainerInfo(Name(container.Value), solution);
        }

        private ContainerInfo? BuildOutputContainerInfo(EntityUid? container)
        {
            if (container is not { Valid: true })
                return null;

            var name = Name(container.Value);
            {
                if (_solutionContainerSystem.TryGetSolution(
                        container.Value, SharedChemMaster.BottleSolutionName, out _, out var solution))
                {
                    return BuildContainerInfo(name, solution);
                }
            }

            if (!TryComp(container, out StorageComponent? storage))
                return null;

            var pills = storage.Container.ContainedEntities.Select((Func<EntityUid, (string, FixedPoint2 quantity)>) (pill =>
            {
                _solutionContainerSystem.TryGetSolution(pill, SharedChemMaster.PillSolutionName, out _, out var solution);
                var quantity = solution?.Volume ?? FixedPoint2.Zero;
                return (Name(pill), quantity);
            })).ToList();

            return new ContainerInfo(name, _storageSystem.GetCumulativeItemAreas((container.Value, storage)), storage.Grid.GetArea())
            {
                Entities = pills
            };
        }

        private static ContainerInfo BuildContainerInfo(string name, Solution solution)
        {
            return new ContainerInfo(name, solution.Volume, solution.MaxVolume)
            {
                Reagents = solution.Contents
            };
        }

        // open-space edit start
        private void SendAnalysisPopup(EntityUid owner, Enum uiKey, EntityUid user, ReagentId reagentId)
        {
            if (!_prototypeManager.TryIndex<ReagentPrototype>(reagentId.Prototype, out var proto))
                return;

            _userInterfaceSystem.ServerSendUiMessage(owner, uiKey, new ChemReagentAnalysisPopupMessage(reagentId, proto.LocalizedName, proto.LocalizedDescription), user);
        }

        private void PrintAnalysis(EntityUid user, ReagentId reagentId)
        {
            if (!_prototypeManager.TryIndex<ReagentPrototype>(reagentId.Prototype, out var proto))
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
        // open-space edit end
    }
}
