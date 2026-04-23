using Content.Client.Mapping;
using Content.Client.Markers;
using Content.Client.Movement.Systems;
using Content.Client.SubFloor;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Console;

namespace Content.Client._OpenSpace.Commands;

internal sealed class MappingUiClientSideSetupCommand : LocalizedEntityCommands
{
    [Dependency] private readonly ContentEyeSystem _contentEye = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IEyeManager _eye = default!;
    [Dependency] private readonly ILightManager _light = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly MarkerSystem _marker = default!;
    [Dependency] private readonly SubFloorHideSystem _subfloor = default!;

    public override string Command => "mappinguiclientsidesetup";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (_light.LockConsoleAccess)
            return;

        _state.RequestStateChange<MappingState>();

        _marker.MarkersVisible = true;
        _light.Enabled = false;
        _subfloor.ShowAll = true;

        if (_player.LocalEntity is { } player && _entity.HasComponent<EyeComponent>(player))
        {
            _contentEye.RequestEye(false, false);
            return;
        }

        _eye.CurrentEye.DrawFov = false;
        _eye.CurrentEye.DrawLight = false;
    }
}
