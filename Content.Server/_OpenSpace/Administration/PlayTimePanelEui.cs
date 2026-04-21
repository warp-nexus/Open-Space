using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.EUI;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.Players.PlayTimeTracking;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration;

/// <summary>
/// Server side of the "Time Transfer" panel used by admins to add or remove
/// play-time from individual roles or entire departments. Lists every
/// <see cref="PlayTimeTrackerPrototype"/> and <see cref="DepartmentPrototype"/>
/// defined in the game and forwards write operations to
/// <see cref="PlayTimeTrackingManager"/>.
/// </summary>
public sealed class PlayTimePanelEui : BaseEui
{
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTimeTracking = default!;

    private readonly ISawmill _sawmill;

    private string _targetName = string.Empty;
    private string? _statusMessage;

    public PlayTimePanelEui()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _log.GetSawmill("admin.playtime_panel");
    }

    /// <summary>
    /// Sets the initial target player by username (used when the panel is
    /// opened via <c>playtimepanel &lt;user&gt;</c>).
    /// </summary>
    public void SetTarget(string username)
    {
        _targetName = username;
        StateDirty();
    }

    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        var found = false;
        var overall = TimeSpan.Zero;
        Dictionary<string, TimeSpan> playTimes = new();

        if (!string.IsNullOrWhiteSpace(_targetName)
            && _playerManager.TryGetSessionByUsername(_targetName, out var session))
        {
            found = true;
            // Flush to make sure current tracker times are persisted before
            // we read them.
            _playTimeTracking.FlushTracker(session);
            overall = _playTimeTracking.GetOverallPlaytime(session);
            playTimes = new Dictionary<string, TimeSpan>(_playTimeTracking.GetTrackerTimes(session));
        }

        var roles = new List<PlayTimeRoleEntry>();
        foreach (var tracker in _prototypes.EnumeratePrototypes<PlayTimeTrackerPrototype>())
        {
            if (tracker.ID == PlayTimeTrackingShared.TrackerOverall)
                continue;

            playTimes.TryGetValue(tracker.ID, out var time);
            roles.Add(new PlayTimeRoleEntry(tracker.ID, tracker.ID, time));
        }
        roles.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));

        var departments = new List<PlayTimeDepartmentEntry>();
        foreach (var department in _prototypes.EnumeratePrototypes<DepartmentPrototype>())
        {
            var trackers = new HashSet<string>();
            foreach (var jobId in department.Roles)
            {
                if (_prototypes.TryIndex<JobPrototype>(jobId, out var job)
                    && !string.IsNullOrEmpty(job.PlayTimeTracker))
                {
                    trackers.Add(job.PlayTimeTracker);
                }
            }

            departments.Add(new PlayTimeDepartmentEntry(
                department.ID,
                department.Name,
                department.Color,
                trackers.ToList()));
        }
        departments.Sort(DepartmentEntryComparer.Instance);

        return new PlayTimePanelEuiState(
            _targetName,
            found,
            overall,
            roles,
            departments,
            _statusMessage);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        // All write operations require at least Moderator, which is the same
        // permission that guards the underlying console commands.
        if (!_admins.HasAdminFlag(Player, AdminFlags.Moderator))
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId}) tried to use the play-time panel without moderator flag.");
            return;
        }

        switch (msg)
        {
            case PlayTimePanelEuiMsg.LookupPlayerRequest lookup:
                _targetName = lookup.PlayerName;
                _statusMessage = null;
                StateDirty();
                break;
            case PlayTimePanelEuiMsg.RefreshRequest:
                _statusMessage = null;
                StateDirty();
                break;
            case PlayTimePanelEuiMsg.AddTimeRequest add:
                ApplyAddTime(add);
                break;
        }
    }

    private void ApplyAddTime(PlayTimePanelEuiMsg.AddTimeRequest request)
    {
        if (string.IsNullOrWhiteSpace(_targetName)
            || !_playerManager.TryGetSessionByUsername(_targetName, out var session))
        {
            _statusMessage = Loc.GetString("playtime-panel-status-no-player");
            StateDirty();
            return;
        }

        var trackersApplied = 0;
        foreach (var (tracker, minutes) in request.TrackerMinutes)
        {
            if (minutes == 0)
                continue;

            if (!_prototypes.HasIndex<PlayTimeTrackerPrototype>(tracker))
                continue;

            _playTimeTracking.AddTimeToTracker(session, tracker, TimeSpan.FromMinutes(minutes));
            trackersApplied++;
        }

        if (request.OverallMinutes != 0)
            _playTimeTracking.AddTimeToOverallPlaytime(session, TimeSpan.FromMinutes(request.OverallMinutes));

        _chat.DispatchServerMessage(Player, Loc.GetString(
            "playtime-panel-chat-applied",
            ("player", _targetName),
            ("trackers", trackersApplied),
            ("overall", request.OverallMinutes)));

        _sawmill.Info(
            $"{Player.Name} ({Player.UserId}) applied play-time changes to {_targetName}: " +
            $"trackers={trackersApplied}, overall={request.OverallMinutes}m");

        _statusMessage = Loc.GetString(
            "playtime-panel-status-applied",
            ("trackers", trackersApplied),
            ("overall", request.OverallMinutes));
        StateDirty();
    }

    private sealed class DepartmentEntryComparer : IComparer<PlayTimeDepartmentEntry>
    {
        public static readonly DepartmentEntryComparer Instance = new();

        public int Compare(PlayTimeDepartmentEntry? x, PlayTimeDepartmentEntry? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (y is null)
                return 1;
            if (x is null)
                return -1;

            return string.Compare(x.DepartmentId, y.DepartmentId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
