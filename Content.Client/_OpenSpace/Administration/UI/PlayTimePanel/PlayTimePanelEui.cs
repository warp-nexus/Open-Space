using Content.Client.Eui;
using Content.Shared.Administration;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._OpenSpace.Administration.UI.PlayTimePanel;

/// <summary>
/// Client side of the "Time Transfer" admin panel. Forwards user actions as
/// EUI messages to the server and receives the current state back.
/// </summary>
[UsedImplicitly]
public sealed class PlayTimePanelEui : BaseEui
{
    private PlayTimePanel Panel { get; }

    public PlayTimePanelEui()
    {
        Panel = new PlayTimePanel();
        Panel.OnClose += () => SendMessage(new CloseEuiMessage());
        Panel.LookupPlayerRequested += name =>
            SendMessage(new PlayTimePanelEuiMsg.LookupPlayerRequest(name));
        Panel.RefreshRequested += () =>
            SendMessage(new PlayTimePanelEuiMsg.RefreshRequest());
        Panel.AddTimeRequested += (trackerMinutes, overallMinutes) =>
            SendMessage(new PlayTimePanelEuiMsg.AddTimeRequest(trackerMinutes, overallMinutes));
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not PlayTimePanelEuiState s)
            return;

        Panel.UpdateState(s);
    }

    public override void Opened()
    {
        Panel.OpenCentered();
    }

    public override void Closed()
    {
        Panel.Close();
        Panel.Dispose();
    }
}
