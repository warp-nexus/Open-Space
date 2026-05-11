using Content.Shared.Administration;
using Content.Shared.Doors.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Console;

namespace Content.Client._OpenSpace.Administration
{
    public sealed class DoorMasterSystem : EntitySystem
    {
        [Dependency] private readonly SpriteSystem _spriteSystem = default!; // Для перекраски энтити

        public bool Enabled { get; set; } = false; // Для Toggle

        public override void Update(float frameTime)
        {
            base.Update(frameTime); // по кадровое обновление

            if (!Enabled) return;

            // Кадровая проверка новых энтити в PVS
            var query = EntityQueryEnumerator<SpriteComponent>();
            while (query.MoveNext(out var uid, out var sprite))
            {
                if (HasComp<OccluderComponent>(uid) || HasComp<DoorComponent>(uid))
                {
                    // Если это дерьмо не было уже полупрозрачным, то вызываем перекраску
                    if (sprite.Color.A > 0.31f)
                    {
                        _spriteSystem.SetColor(uid, sprite.Color.WithAlpha(0.3f));
                    }
                }
            }
        }

        public void Toggle()
        {
            Enabled = !Enabled;

            var enumerator = EntityQueryEnumerator<SpriteComponent>();
            while (enumerator.MoveNext(out var uid, out var sprite))
            {
                if (HasComp<OccluderComponent>(uid) || HasComp<DoorComponent>(uid)) // Проверяем компачи
                {
                    var alpha = Enabled ? 0.3f : 1.0f; // 30%
                    _spriteSystem.SetColor(uid, sprite.Color.WithAlpha(alpha)); // Применяем
                }
            }
        }
    }

    [AnyCommand]
    public sealed class ToggleDoorsCommand : IConsoleCommand
    {
        public string Command => "toggledoors";
        public string Description => Loc.GetString("cmd-toggledoors-desc");
        public string Help => Loc.GetString("cmd-toggledoors-help");

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var sys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<DoorMasterSystem>(); // It's time to take it boy
            sys.Toggle();
        }
    }
}
