using Content.Client.UserInterface.Controls;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.SmartFridge;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client.SmartFridge;

public sealed class SmartFridgeBoundUserInterface : BoundUserInterface
{
    // open-space edit start
    [Dependency] private readonly IPlayerManager _player = default!;
    private readonly AccessReaderSystem _accessReader;
    // open-space edit end

    private SmartFridgeMenu? _menu;

    public SmartFridgeBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        // open-space edit start
        IoCManager.InjectDependencies(this);
        _accessReader = EntMan.System<AccessReaderSystem>();
        // open-space edit end
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<SmartFridgeMenu>();
        // open-space edit start
        _menu.OnDispenseOne += data => SendPredictedMessage(new SmartFridgeDispenseItemMessage(data.Entry, 1));
        _menu.OnDispenseAmount += (data, amount) => SendPredictedMessage(new SmartFridgeDispenseItemMessage(data.Entry, (uint) amount));
        _menu.OnDispenseAll += data => SendPredictedMessage(new SmartFridgeDispenseItemMessage(data.Entry, dispenseAll: true));
        // open-space edit end
        Refresh();
    }

    public void Refresh()
    {
        if (_menu is not { } menu || !EntMan.TryGetComponent(Owner, out SmartFridgeComponent? fridge))
            return;

        // open-space edit start
        menu.SetWindowTitle(Loc.GetString(fridge.WindowTitle));
        menu.SetFlavorText(Loc.GetString(fridge.FlavorText));
        menu.SetAccessState(ShouldShowAccessBanner(fridge), fridge.AccessDeniedBanner is { } banner
            ? Loc.GetString(banner)
            : string.Empty);
        menu.Populate((Owner, fridge));
        // open-space edit end
    }

    // open-space edit start
    private bool ShouldShowAccessBanner(SmartFridgeComponent fridge)
    {
        if (fridge.AccessDeniedBanner == null)
            return false;

        if (!EntMan.TryGetComponent(Owner, out AccessReaderComponent? accessReader))
            return false;

        if (_player.LocalEntity is not { } user)
            return true;

        var accessItems = _accessReader.FindPotentialAccessItems(user);
        var accessTags = _accessReader.FindAccessTags(user, accessItems);
        _accessReader.FindStationRecordKeys(user, out var stationKeys, accessItems);
        return !_accessReader.IsAllowed(accessTags, stationKeys, Owner, accessReader);
    }
    // open-space edit end
}
