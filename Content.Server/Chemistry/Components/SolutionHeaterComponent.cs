namespace Content.Server.Chemistry.Components;

[RegisterComponent]
public sealed partial class SolutionHeaterComponent : Component
{
    /// <summary>
    /// How much heat is added per second to the solution, taking upgrades into account.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HeatPerSecond;

    // open-space edit start
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TargetTemperature = 273.15f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool AutoEject;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TemperatureTolerance = 0.5f;
    // open-space edit end
}
