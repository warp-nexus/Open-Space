using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.Mapping;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server._OpenSpace.Mapping;

[AdminCommand(AdminFlags.Server | AdminFlags.Mapping)]
public sealed class MappingUiCommand : LocalizedEntityCommands
{
    [Dependency] private readonly MappingSystem _mapping = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    public override string Command => "mappingui";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint(Loc.GetString("cmd-hint-mappingui-id")),
            _ => CompletionResult.Empty
        };
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        if (args.Length > 1)
        {
            shell.WriteLine(Help);
            return;
        }

        MapId mapId;
        if (args.Length == 1)
        {
            if (!int.TryParse(args[0], out var intMapId))
            {
                shell.WriteError(Loc.GetString("cmd-mappingui-failure-integer", ("arg", args[0])));
                return;
            }

            mapId = new MapId(intMapId);

            if (mapId == MapId.Nullspace)
            {
                shell.WriteError(Loc.GetString("cmd-mappingui-nullspace"));
                return;
            }

            if (_map.MapExists(mapId))
            {
                shell.WriteError(Loc.GetString("cmd-mappingui-exists", ("mapId", mapId)));
                return;
            }

            _map.CreateMap(mapId, runMapInit: false);
        }
        else
        {
            _map.CreateMap(out mapId, runMapInit: false);
        }

        if (!_map.MapExists(mapId))
        {
            shell.WriteError(Loc.GetString("cmd-mappingui-error"));
            return;
        }

        if (player.AttachedEntity is { Valid: true } playerEntity &&
            (EntityManager.GetComponent<MetaDataComponent>(playerEntity).EntityPrototype is not { } proto ||
             proto != GameTicker.AdminObserverPrototypeName))
        {
            shell.ExecuteCommand("aghost");
        }

        shell.ExecuteCommand("changecvar events.enabled false");
        shell.ExecuteCommand("changecvar shuttle.auto_call_time 0");

        _mapping.ToggleAutosave(mapId, $"MAPPINGUI-{(int) mapId}");

        shell.ExecuteCommand($"tp 0 0 {mapId}");
        shell.RemoteExecuteCommand("mappinguiclientsidesetup");

        DebugTools.Assert(_map.IsPaused(mapId));
        shell.WriteLine(Loc.GetString("cmd-mappingui-success", ("mapId", mapId)));
    }
}
