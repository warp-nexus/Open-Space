using Content.Shared.Administration; // AdminFlags
using Content.Shared.CCVar; // SetCVar
using Content.Shared.GameTicking; // RoundRestartCleanupEvent
using Content.Shared.Audio; // SoundCollectionPrototype
using Robust.Shared.Configuration; // IConfigurationManager
using Robust.Shared.Console; // IConsoleCommand
using Robust.Shared.Prototypes; // IPrototypeManager

namespace Content.Server.Administration.Commands;

// Resources/locale/ru_RU/_OpenSpace/commands/set-lobby-music-command.ftl
// Resources/locale/en_US/_OpenSpace/commands/set-lobby-music-command.ftl

[AdminCommand(AdminFlags.VarEdit)]
public sealed class SetLobbyMusicCommand : EntitySystem, IConsoleCommand
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public string Command => "setlobbymusic";
    public new string Description => Loc.GetString("set-lobby-music-command-description");
    public string Help => Loc.GetString("set-lobby-music-command-help");

    private bool _alreadyUsed;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart); // Прослушка для того, чтобы можно было использовать лишь один раз за раунд
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _alreadyUsed = false;
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_alreadyUsed) // За раунд можно воспользоваться только один раз
        {
            shell.WriteError(Loc.GetString("set-lobby-music-command-already-used"));
            return;
        }

        if (args.Length != 1) // Проверка существования первого аргумента
        {
            shell.WriteError(Loc.GetString("set-lobby-music-command-invalid-args"));
            return;
        }

        var newCollection = args[0];
        var current = _cfg.GetCVar(CCVars.LobbyMusicCollection);

        if (current == newCollection) // Проверка на индетичность
        {
            shell.WriteError(Loc.GetString("set-lobby-music-command-already-set", ("collection", newCollection)));
            return;
        }

        _cfg.SetCVar(CCVars.LobbyMusicCollection, newCollection);
        _alreadyUsed = true;

        shell.WriteLine(Loc.GetString("set-lobby-music-command-success", ("collection", newCollection)));
    }
}
