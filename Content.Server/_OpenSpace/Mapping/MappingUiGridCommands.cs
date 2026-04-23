using System.Numerics;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Shuttles.Systems;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

namespace Content.Server._OpenSpace.Mapping;

[AdminCommand(AdminFlags.Mapping)]
public sealed class MappingUiGridTeleportCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override string Command => "mappinguigridtp";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint(Loc.GetString("cmd-hint-mappinguigrid")),
            _ => CompletionResult.Empty
        };
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        if (shell.Player?.AttachedEntity is not { Valid: true } playerEntity)
        {
            shell.WriteError(Loc.GetString("cmd-mappinguigrid-no-player"));
            return;
        }

        if (!TryGetGrid(shell, args[0], out var grid, out var gridComp))
            return;

        _transform.SetCoordinates(playerEntity, new EntityCoordinates(grid, gridComp.LocalAABB.Center));
        _transform.AttachToGridOrMap(playerEntity);

        if (EntityManager.TryGetComponent(playerEntity, out PhysicsComponent? physics))
            _physics.SetLinearVelocity(playerEntity, Vector2.Zero, body: physics);
    }

    private bool TryGetGrid(IConsoleShell shell, string value, out EntityUid grid, out MapGridComponent gridComp)
    {
        grid = default;
        gridComp = default!;

        if (!NetEntity.TryParse(value, out var netEntity) ||
            !EntityManager.TryGetEntity(netEntity, out var gridUid) ||
            !EntityManager.TryGetComponent(gridUid, out MapGridComponent? component))
        {
            shell.WriteError(Loc.GetString("cmd-mappinguigrid-invalid", ("grid", value)));
            return false;
        }

        grid = gridUid.Value;
        gridComp = component;
        return true;
    }
}

[AdminCommand(AdminFlags.Mapping)]
public sealed class MappingUiGridSetCommand : LocalizedEntityCommands
{
    [Dependency] private readonly MetaDataSystem _metadata = default!;
    [Dependency] private readonly SharedShuttleSystem _shuttle = default!;

    public override string Command => "mappinguigridset";

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint(Loc.GetString("cmd-hint-mappinguigrid")),
            2 => CompletionResult.FromHint("#RRGGBB"),
            3 => CompletionResult.FromHint(Loc.GetString("cmd-hint-mappinguigrid-name")),
            _ => CompletionResult.Empty
        };
    }

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 2 or > 3)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!TryGetGrid(shell, args[0], out var grid))
            return;

        var colorText = args[1].Trim();
        if (!colorText.StartsWith('#'))
            colorText = $"#{colorText}";

        var color = Color.TryFromHex(colorText);
        if (color == null)
        {
            shell.WriteError(Loc.GetString("cmd-mappinguigrid-color-invalid", ("color", args[1])));
            return;
        }

        _shuttle.SetIFFColor(grid, color.Value);

        if (args.Length == 3)
        {
            var name = args[2].Trim();
            _metadata.SetEntityName(grid, name);
        }

        shell.WriteLine(Loc.GetString("cmd-mappinguigridset-success"));
    }

    private bool TryGetGrid(IConsoleShell shell, string value, out EntityUid grid)
    {
        grid = default;

        if (!NetEntity.TryParse(value, out var netEntity) ||
            !EntityManager.TryGetEntity(netEntity, out var gridUid) ||
            !EntityManager.HasComponent<MapGridComponent>(gridUid))
        {
            shell.WriteError(Loc.GetString("cmd-mappinguigrid-invalid", ("grid", value)));
            return false;
        }

        grid = gridUid.Value;
        return true;
    }
}