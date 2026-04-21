using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
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
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._OpenSpace.Administration;

public sealed class TimeTransferPanelEui : BaseEui
{
    private const int MaxMinutes = TimeTransferPanelEuiMsg.MaxMinutes;

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
    private bool _inProgress;

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

        if (msg is not TimeTransferPanelEuiMsg.AddTime addTime)
            return;

        if (_inProgress)
        {
            SetStatus(Loc.GetString("time-transfer-panel-status-in-progress"), TimeTransferPanelStatusKind.Info);
            return;
        }

        _ = AddTimeSafe(addTime);
    }

    private async Task AddTimeSafe(TimeTransferPanelEuiMsg.AddTime msg)
    {
        _inProgress = true;

        try
        {
            await AddTime(msg);
        }
        catch (Exception ex)
        {
            _sawmill.Error($"Unhandled exception in time transfer panel: {ex}");
            SetStatus(Loc.GetString("time-transfer-panel-error-unhandled"), TimeTransferPanelStatusKind.Error);
        }
        finally
        {
            _inProgress = false;
        }
    }

    private async Task AddTime(TimeTransferPanelEuiMsg.AddTime msg)
    {
        if (!TryRequireModerator(out var admin))
        {
            _sawmill.Warning("A disconnected or unauthorized player tried to edit playtime.");
            Close();
            return;
        }

        var rawTarget = msg.Player ?? string.Empty;
        if (rawTarget.Length > TimeTransferPanelEuiMsg.MaxPlayerLength)
        {
            SetStatus(Loc.GetString("time-transfer-panel-error-invalid-payload"), TimeTransferPanelStatusKind.Error);
            return;
        }

        var target = rawTarget.Trim();
        if (string.IsNullOrWhiteSpace(target))
        {
            SetStatus(Loc.GetString("time-transfer-panel-error-no-player"), TimeTransferPanelStatusKind.Error);
            return;
        }

        var messageEntries = msg.Entries ?? Array.Empty<TimeTransferPanelEntry>();
        if (messageEntries.Length > TimeTransferPanelEuiMsg.MaxEntriesCount)
        {
            SetStatus(Loc.GetString("time-transfer-panel-error-invalid-payload"), TimeTransferPanelStatusKind.Error);
            return;
        }

        var minutesByTracker = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in messageEntries)
        {
            if (entry == null)
                continue;

            var rawTracker = entry.Tracker ?? string.Empty;
            if (rawTracker.Length > TimeTransferPanelEuiMsg.MaxTrackerLength)
            {
                SetStatus(Loc.GetString("time-transfer-panel-error-invalid-payload"), TimeTransferPanelStatusKind.Error);
                return;
            }

            var tracker = rawTracker.Trim();
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
        if (!TryRequireModerator(out admin))
            return;

        if (located == null)
        {
            SetStatus(
                Loc.GetString("time-transfer-panel-error-player-not-found", ("player", target)),
                TimeTransferPanelStatusKind.Error);
            return;
        }

        var targetWasOnline = false;

        if (!TryApplyOnlineTime(located.UserId, located.Username, minutesByTracker, out targetWasOnline))
            return;

        if (!targetWasOnline)
        {
            var offlineResult = await AddOfflineTime(located.UserId, located.Username, minutesByTracker);
            if (!TryRequireModerator(out admin) || offlineResult == TimeTransferApplyResult.Aborted)
                return;

            targetWasOnline = offlineResult == TimeTransferApplyResult.Online;
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

    private bool TryApplyOnlineTime(
        NetUserId userId,
        string username,
        IReadOnlyDictionary<string, int> minutesByTracker,
        out bool targetWasOnline)
    {
        targetWasOnline = false;

        if (!_players.TryGetSessionById(userId, out var session))
            return true;

        targetWasOnline = true;
        if (!_playTime.TryGetTrackerTimes(session, out _))
        {
            SetStatus(
                Loc.GetString("time-transfer-panel-error-playtime-loading", ("player", username)),
                TimeTransferPanelStatusKind.Error);
            return false;
        }

        foreach (var entry in minutesByTracker)
        {
            _playTime.AddTimeToTracker(session, entry.Key, TimeSpan.FromMinutes(entry.Value));
        }

        _playTime.QueueSendTimers(session);
        _playTime.SaveSession(session);
        return true;
    }

    private async Task<TimeTransferApplyResult> AddOfflineTime(
        NetUserId userId,
        string username,
        IReadOnlyDictionary<string, int> minutesByTracker)
    {
        var current = await _db.GetPlayTimes(userId.UserId);
        if (!TryRequireModerator(out _))
            return TimeTransferApplyResult.Aborted;

        if (!TryApplyOnlineTime(userId, username, minutesByTracker, out var targetWasOnline))
            return TimeTransferApplyResult.Aborted;

        if (targetWasOnline)
            return TimeTransferApplyResult.Online;

        var currentByTracker = current.ToDictionary(time => time.Tracker, time => time.TimeSpent);
        var updates = new List<PlayTimeUpdate>(minutesByTracker.Count);

        foreach (var entry in minutesByTracker)
        {
            currentByTracker.TryGetValue(entry.Key, out var existing);
            updates.Add(new PlayTimeUpdate(userId, entry.Key, existing + TimeSpan.FromMinutes(entry.Value)));
        }

        await _db.UpdatePlayTimes(updates);
        if (!TryRequireModerator(out _))
            return TimeTransferApplyResult.Aborted;

        return TimeTransferApplyResult.Offline;
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

    private bool TryRequireModerator([NotNullWhen(true)] out ICommonSession? admin)
    {
        admin = Player;
        if (admin != null && _admins.HasAdminFlag(admin, AdminFlags.Moderator))
            return true;

        SetStatus(Loc.GetString("time-transfer-panel-error-no-access"), TimeTransferPanelStatusKind.Error);
        return false;
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player == Player)
            StateDirty();
    }

    private enum TimeTransferApplyResult : byte
    {
        Aborted,
        Offline,
        Online,
    }
}
