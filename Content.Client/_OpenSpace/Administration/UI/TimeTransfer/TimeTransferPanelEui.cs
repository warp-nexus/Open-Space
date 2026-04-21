using Content.Client.Eui;
using Content.Shared._OpenSpace.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._OpenSpace.Administration.UI.TimeTransfer;

[UsedImplicitly]
public sealed class TimeTransferPanelEui : BaseEui
{
    private readonly TimeTransferPanelWindow _window;

    public TimeTransferPanelEui()
    {
        _window = new TimeTransferPanelWindow();
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
        _window.AddTimeRequested += (player, entries) =>
        {
            SendMessage(new TimeTransferPanelEuiMsg.AddTime
            {
                Player = player,
                Entries = entries,
            });
        };
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is TimeTransferPanelEuiState timeTransferState)
            _window.ApplyState(timeTransferState);
    }
}
