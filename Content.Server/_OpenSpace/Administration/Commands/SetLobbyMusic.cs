using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Robust.Shared.Configuration;
using Robust.Shared.Console;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.VarEdit)]
public sealed class SetLobbyMusicCommand : EntitySystem, IConsoleCommand
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public string Command => "setlobbymusic";
    public string Description => "Меняет коллекцию музыки в лобби (один раз за раунд).";
    public string Help => "Использование: setlobbymusic <название_коллекции>";

    private static bool _alreadyUsed;

    public override void Initialize()
    {
        base.Initialize();
        // Подписываемся на очистку раунда для сброса флага
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _alreadyUsed = false;
    }

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_alreadyUsed)
        {
            shell.WriteError("Эту команду можно использовать только один раз за раунд!");
            return;
        }

        if (args.Length != 1)
        {
            shell.WriteError("Укажите ровно один аргумент: название коллекции.");
            return;
        }

        var newCollection = args[0];
        var current = _cfg.GetCVar(CCVars.LobbyMusicCollection);

        if (current == newCollection)
        {
            shell.WriteError("Эта коллекция уже установлена.");
            return;
        }

        // Применяем изменение
        _cfg.SetCVar(CCVars.LobbyMusicCollection, newCollection);
        _alreadyUsed = true;

        shell.WriteLine($"Коллекция музыки изменена на '{newCollection}'. Повторное использование заблокировано до нового раунда.");
    }
}
