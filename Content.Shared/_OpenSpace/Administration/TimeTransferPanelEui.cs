using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._OpenSpace.Administration;

[Serializable, NetSerializable]
public enum TimeTransferPanelStatusKind : byte
{
    None,
    Info,
    Success,
    Error,
}

[Serializable, NetSerializable]
public sealed class TimeTransferPanelEuiState : EuiStateBase
{
    public bool HasAccess;
    public string Status = string.Empty;
    public TimeTransferPanelStatusKind StatusKind;

    public TimeTransferPanelEuiState()
    {
    }

    public TimeTransferPanelEuiState(bool hasAccess, string status, TimeTransferPanelStatusKind statusKind)
    {
        HasAccess = hasAccess;
        Status = status;
        StatusKind = statusKind;
    }
}

public static class TimeTransferPanelEuiMsg
{
    [Serializable, NetSerializable]
    public sealed class AddTime : EuiMessageBase
    {
        public string Player = string.Empty;
        public TimeTransferPanelEntry[] Entries = Array.Empty<TimeTransferPanelEntry>();
    }
}

[Serializable, NetSerializable]
public sealed class TimeTransferPanelEntry
{
    public string Tracker = string.Empty;
    public int Minutes;

    public TimeTransferPanelEntry()
    {
    }

    public TimeTransferPanelEntry(string tracker, int minutes)
    {
        Tracker = tracker;
        Minutes = minutes;
    }
}
