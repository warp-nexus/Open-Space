using System.Linq;
using System.Threading.Tasks;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Server.Chat.Managers;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared._OpenSpace.Administration;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Content.Shared.Players.PlayTimeTracking;
using Robust.Server.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Administration;

public sealed class TimeTransferPanelEui : BaseEui
{
    private const int MaxMinutes = 5259600;

    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly ILogManager _log = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly PlayTimeTrackingManager _playTime = default!;

    private readonly ISawmill _sawmill;
    private string _status = string.Empty;
    private TimeTransferPanelStatusKind _statusKind = TimeTransferPanelStatusKind.None;

    public TimeTransferPanelEui()
    {
        IoCManager.InjectDependencies(this);
        _sawmill = _log.GetSawmill("admin.time_transfer");
    }

    public override EuiStateBase GetNewState()
    {
        return new TimeTransferPanelEuiState(
            _admins.HasAdminFlag(Player, AdminFlags.Moderator),
            _status,
            _statusKind);
    }

    public override void Opened()
    {
        base.Opened();
        _admins.OnPermsChanged += OnPermsChanged;
        StateDirty();
    }

    public override void Closed()
    {
        base.Closed();
        _admins.OnPermsChanged -= OnPermsChanged;
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is TimeTransferPanelEuiMsg.AddTime addTime)
            AddTime(addTime);
    }

    private async void AddTime(TimeTransferPanelEuiMsg.AddTime msg)
    {
        if (!_admins.HasAdminFlag(Player, AdminFlags.Moderator))
        {
            _sawmill.Warning($"{Player.Name} ({Player.UserId}) tried to edit playtime without Moderator flag.");
            SetStatus(Loc.GetString("time-transfer-panel-error-no-access"), TimeTransferPanelStatusKind.Error);
            Close();
            return;
        }

        var target = msg.Player.Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            SetStatus(Loc.GetString("time-transfer-panel-error-no-player"), TimeTransferPanelStatusKind.Error);
            return;
        }

        var messageEntries = msg.Entries ?? Array.Empty<TimeTransferPanelEntry>();
        var minutesByTracker = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in messageEntries)
        {
            if (entry == null)
                continue;

            var tracker = entry.Tracker.Trim();
            if (string.IsNullOrWhiteSpace(tracker))
                continue;

            if (entry.Minutes is <= 0 or > MaxMinutes)
            {
                SetStatus(Loc.GetString("time-transfer-panel-error-no-minutes"), TimeTransferPanelStatusKind.Error);
                return;
            }

            if (!_prototypes.HasIndex<PlayTimeTrackerPrototype>(tracker))
            {
                SetStatus(
                    Loc.GetString("time-transfer-panel-error-invalid-tracker", ("tracker", tracker)),
                    TimeTransferPanelStatusKind.Error);
                return;
            }

            minutesByTracker.TryGetValue(tracker, out var existing);
            minutesByTracker[tracker] = existing + entry.Minutes;
        }

        if (minutesByTracker.Count == 0)
        {
            SetStatus(Loc.GetString("time-transfer-panel-error-no-roles"), TimeTransferPanelStatusKind.Error);
            return;
        }

        SetStatus(Loc.GetString("time-transfer-panel-status-applying"), TimeTransferPanelStatusKind.Info);

        var located = await _playerLocator.LookupIdByNameOrIdAsync(target);
        if (located == null)
        {
            SetStatus(
                Loc.GetString("time-transfer-panel-error-player-not-found", ("player", target)),
                TimeTransferPanelStatusKind.Error);
            return;
        }

        var targetWasOnline = false;

        if (_players.TryGetSessionById(located.UserId, out var session))
        {
            targetWasOnline = true;
            if (!_playTime.TryGetTrackerTimes(session, out _))
            {
                SetStatus(
                    Loc.GetString("time-transfer-panel-error-playtime-loading", ("player", located.Username)),
                    TimeTransferPanelStatusKind.Error);
                return;
            }

            foreach (var entry in minutesByTracker)
            {
                _playTime.AddTimeToTracker(session, entry.Key, TimeSpan.FromMinutes(entry.Value));
            }

            _playTime.QueueSendTimers(session);
            _playTime.SaveSession(session);
        }
        else
        {
            await AddOfflineTime(located.UserId, minutesByTracker);
        }

        var totalMinutes = 0;
        foreach (var entry in minutesByTracker)
        {
            totalMinutes += entry.Value;
        }

        var summary = FormatEntrySummary(minutesByTracker);
        _sawmill.Info(
            $"{Player.Name} ({Player.UserId}) transferred playtime to {located.Username} ({located.UserId}); " +
            $"targetOnline={targetWasOnline}; trackers={minutesByTracker.Count}; totalMinutes={totalMinutes}; entries=[{summary}].");

        _chat.SendAdminAnnouncement(Loc.GetString(
            "time-transfer-panel-admin-announcement",
            ("admin", $"{Player.Name} ({Player.UserId})"),
            ("player", $"{located.Username} ({located.UserId})"),
            ("summary", summary)));

        SetStatus(
            Loc.GetString(
                "time-transfer-panel-success-add",
                ("minutes", totalMinutes),
                ("count", minutesByTracker.Count),
                ("player", located.Username)),
            TimeTransferPanelStatusKind.Success);
    }

    private async Task AddOfflineTime(NetUserId userId, IReadOnlyDictionary<string, int> minutesByTracker)
    {
        var current = await _db.GetPlayTimes(userId.UserId);
        var currentByTracker = current.ToDictionary(time => time.Tracker, time => time.TimeSpent);
        var updates = new List<PlayTimeUpdate>(minutesByTracker.Count);

        foreach (var entry in minutesByTracker)
        {
            currentByTracker.TryGetValue(entry.Key, out var existing);
            updates.Add(new PlayTimeUpdate(userId, entry.Key, existing + TimeSpan.FromMinutes(entry.Value)));
        }

        await _db.UpdatePlayTimes(updates);
    }

    private static string FormatEntrySummary(IReadOnlyDictionary<string, int> minutesByTracker)
    {
        var parts = new List<string>(minutesByTracker.Count);
        foreach (var entry in minutesByTracker.OrderBy(entry => entry.Key))
        {
            parts.Add($"{entry.Key}: {entry.Value}m");
        }

        return string.Join(", ", parts);
    }

    private void SetStatus(string status, TimeTransferPanelStatusKind kind)
    {
        _status = status;
        _statusKind = kind;
        StateDirty();
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player)
            StateDirty();
    }
}
