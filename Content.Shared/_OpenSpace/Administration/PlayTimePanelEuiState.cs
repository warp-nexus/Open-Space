using Content.Shared.Eui;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

/// <summary>
/// State for the play-time transfer panel (F7 admin menu).
/// Contains current target player info and the lists of trackers/departments
/// shown in the two panel tabs.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlayTimePanelEuiState : EuiStateBase
{
    public string PlayerName { get; }
    public bool PlayerFound { get; }
    public TimeSpan OverallPlaytime { get; }
    public List<PlayTimeRoleEntry> Roles { get; }
    public List<PlayTimeDepartmentEntry> Departments { get; }
    public string? StatusMessage { get; }

    public PlayTimePanelEuiState(
        string playerName,
        bool playerFound,
        TimeSpan overallPlaytime,
        List<PlayTimeRoleEntry> roles,
        List<PlayTimeDepartmentEntry> departments,
        string? statusMessage)
    {
        PlayerName = playerName;
        PlayerFound = playerFound;
        OverallPlaytime = overallPlaytime;
        Roles = roles;
        Departments = departments;
        StatusMessage = statusMessage;
    }
}

/// <summary>
/// A single play-time tracker entry shown on the "By Roles" tab.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlayTimeRoleEntry
{
    public string TrackerId { get; }
    public string DisplayName { get; }
    public TimeSpan CurrentTime { get; }

    public PlayTimeRoleEntry(string trackerId, string displayName, TimeSpan currentTime)
    {
        TrackerId = trackerId;
        DisplayName = displayName;
        CurrentTime = currentTime;
    }
}

/// <summary>
/// A single department entry shown on the "By Departments" tab. Adding time
/// to a department adds time to every tracker that belongs to a job in that
/// department.
/// </summary>
[Serializable, NetSerializable]
public sealed class PlayTimeDepartmentEntry
{
    public string DepartmentId { get; }
    public string DisplayName { get; }
    public Color Color { get; }
    public List<string> Trackers { get; }

    public PlayTimeDepartmentEntry(string departmentId, string displayName, Color color, List<string> trackers)
    {
        DepartmentId = departmentId;
        DisplayName = displayName;
        Color = color;
        Trackers = trackers;
    }
}

public static class PlayTimePanelEuiMsg
{
    /// <summary>
    /// Client asks the server to load data for the given username.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class LookupPlayerRequest : EuiMessageBase
    {
        public string PlayerName { get; }

        public LookupPlayerRequest(string playerName)
        {
            PlayerName = playerName;
        }
    }

    /// <summary>
    /// Client asks the server to refresh the state for the current target.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class RefreshRequest : EuiMessageBase
    {
    }

    /// <summary>
    /// Client asks the server to add the given amount of minutes to one or
    /// more trackers and optionally to the overall playtime for the current
    /// target player. Minutes may be negative to remove time.
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class AddTimeRequest : EuiMessageBase
    {
        public Dictionary<string, int> TrackerMinutes { get; }
        public int OverallMinutes { get; }

        public AddTimeRequest(Dictionary<string, int> trackerMinutes, int overallMinutes)
        {
            TrackerMinutes = trackerMinutes;
            OverallMinutes = overallMinutes;
        }
    }
}
