using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

/// <summary>
/// Opens the "Time Transfer" panel for the executing admin. Optionally takes
/// a player name which is pre-populated in the panel's target field.
/// </summary>
[AdminCommand(AdminFlags.Moderator)]
public sealed class PlayTimePanelCommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _euis = default!;

    public override string Command => "playtimepanel";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        var ui = new PlayTimePanelEui();
        _euis.OpenEui(ui, player);

        if (args.Length >= 1)
            ui.SetTarget(args[0]);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.SessionNames(),
                Loc.GetString("cmd-playtimepanel-arg-user"));
        }

        return CompletionResult.Empty;
    }
}
