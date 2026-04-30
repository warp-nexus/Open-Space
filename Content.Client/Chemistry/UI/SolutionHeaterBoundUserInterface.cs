using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Content.Shared.Chemistry;

namespace Content.Client.Chemistry.UI;

[UsedImplicitly]
public sealed class SolutionHeaterBoundUserInterface : BoundUserInterface
{
    private SolutionHeaterWindow? _window;

    public SolutionHeaterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        // open-space edit start
        _window = this.CreateWindow<SolutionHeaterWindow>();
        _window.Title = EntMan.GetComponent<MetaDataComponent>(Owner).EntityName;

        _window.OnTargetTemperatureChanged += value => SendMessage(new SolutionHeaterSetTargetTemperatureMessage(value));
        _window.OnToggleAutoEjectPressed += () => SendMessage(new SolutionHeaterToggleAutoEjectMessage());
        _window.OnToggleEnabledPressed += () => SendMessage(new SolutionHeaterToggleEnabledMessage());
        _window.OnEjectPressed += () => SendMessage(new SolutionHeaterEjectContainerMessage());
        // open-space edit end
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        // open-space edit start
        _window?.UpdateState((SolutionHeaterBoundUserInterfaceState) state);
        // open-space edit end
    }
}
