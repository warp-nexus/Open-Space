using Content.Shared.Chemistry;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client.Chemistry.UI
{
    /// <summary>
    /// Initializes a <see cref="ChemMasterWindow"/> and updates it when new server messages are received.
    /// </summary>
    [UsedImplicitly]
    public sealed class ChemMasterBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ChemMasterWindow? _window;
        private ChemAnalysisPopup? _analysisPopup;

        public ChemMasterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        /// <summary>
        /// Called each time a chem master UI instance is opened. Generates the window and fills it with
        /// relevant info. Sets the actions for static buttons.
        /// </summary>
        protected override void Open()
        {
            base.Open();

            // Setup window layout/elements
            _window = this.CreateWindow<ChemMasterWindow>();
            _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

            // Setup static button actions.
            _window.InputEjectButton.OnPressed += _ => SendMessage(new ChemMasterEjectBeakerMessage());
            _window.OutputEjectButton.OnPressed += _ => SendMessage(
                new ItemSlotButtonPressedEvent(SharedChemMaster.OutputSlotName));
            // open-space edit start
            _window.OnBufferModeChanged += mode => SendMessage(new ChemMasterSetModeMessage(mode));
            // open-space edit end
            _window.CreatePillButton.OnPressed += _ => SendMessage(
                new ChemMasterCreatePillsMessage((uint) _window.PillDosage.Value, _window.PillLabelLine));

            for (uint i = 0; i < _window.PillTypeButtons.Length; i++)
            {
                var pillType = i;
                _window.PillTypeButtons[i].OnPressed += _ => SendMessage(new ChemMasterSetPillTypeMessage(pillType));
            }

            _window.OnReagentAmountPressed += (reagent, amount, fromBuffer) =>
                SendMessage(new ChemMasterReagentAmountButtonMessage(reagent, amount, fromBuffer));
            _window.OnCustomAmountPressed += (reagent, amount, fromBuffer) =>
                SendMessage(new ChemMasterCustomAmountMessage(reagent, amount, fromBuffer));
            _window.OnAnalyzePressed += reagent => SendMessage(new ChemAnalyzeReagentMessage(reagent));
        }

        /// <summary>
        /// Update the ui each time new state data is sent from the server.
        /// </summary>
        /// <param name="state">
        /// Data of the <see cref="SharedReagentDispenserComponent"/> that this ui represents.
        /// Sent from the server.
        /// </param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ChemMasterBoundUserInterfaceState) state;

            _window?.UpdateState(castState); // Update window state
        }

        protected override void ReceiveMessage(BoundUserInterfaceMessage message)
        {
            base.ReceiveMessage(message);

            if (message is not ChemReagentAnalysisPopupMessage popup)
                return;

            _analysisPopup?.Close();
            _analysisPopup = new ChemAnalysisPopup(popup);
            _analysisPopup.OnPrintPressed += reagent => SendMessage(new ChemPrintAnalysisMessage(reagent));
            _analysisPopup.OpenCentered();
        }
    }
}
