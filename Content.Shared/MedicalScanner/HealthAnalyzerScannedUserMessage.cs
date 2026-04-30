using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Robust.Shared.Serialization;

namespace Content.Shared.MedicalScanner;

/// <summary>
/// On interacting with an entity retrieves the entity UID for use with getting the current damage of the mob.
/// </summary>
[Serializable, NetSerializable]
public sealed class HealthAnalyzerScannedUserMessage : BoundUserInterfaceMessage
{
    public HealthAnalyzerUiState State;

    public HealthAnalyzerScannedUserMessage(HealthAnalyzerUiState state)
    {
        State = state;
    }
}

[Serializable, NetSerializable]
public sealed class HealthAnalyzerClearMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class HealthAnalyzerPrintMessage : BoundUserInterfaceMessage;

// open-space edit start
[Serializable, NetSerializable]
public sealed class HealthAnalyzerReagentEntry(string reagentPrototype, FixedPoint2 quantity)
{
    public readonly string ReagentPrototype = reagentPrototype;
    public readonly FixedPoint2 Quantity = quantity;
}
// open-space edit end

/// <summary>
/// Contains the current state of a health analyzer control. Used for the health analyzer and cryo pod.
/// </summary>
[Serializable, NetSerializable]
public struct HealthAnalyzerUiState
{
    // open-space edit start
    public bool HasData;
    public readonly NetEntity? TargetEntity;
    public string PatientName;
    public string SpeciesName;
    public MobState? MobState;
    public float Condition;
    public FixedPoint2 BloodAmount;
    public FixedPoint2 TotalDamage;
    public FixedPoint2 AirlossDamage;
    public FixedPoint2 ToxinDamage;
    public FixedPoint2 BurnDamage;
    public FixedPoint2 BruteDamage;
    public int? Pulse;
    public bool? GenesStable;
    public List<HealthAnalyzerReagentEntry> Reagents;
    public List<string> Dependencies;
    // open-space edit end
    public float Temperature;
    public float BloodLevel;
    public bool? ScanMode;
    public bool? Bleeding;
    public bool? Unrevivable;

    public HealthAnalyzerUiState()
    {
        // open-space edit start
        Reagents = new();
        Dependencies = new();
        PatientName = string.Empty;
        SpeciesName = string.Empty;
        // open-space edit end
    }

    public HealthAnalyzerUiState(
        bool hasData,
        NetEntity? targetEntity,
        string patientName,
        string speciesName,
        MobState? mobState,
        float condition,
        float temperature,
        float bloodLevel,
        FixedPoint2 bloodAmount,
        FixedPoint2 totalDamage,
        FixedPoint2 airlossDamage,
        FixedPoint2 toxinDamage,
        FixedPoint2 burnDamage,
        FixedPoint2 bruteDamage,
        int? pulse,
        bool? genesStable,
        List<HealthAnalyzerReagentEntry> reagents,
        List<string> dependencies,
        bool? scanMode,
        bool? bleeding,
        bool? unrevivable)
    {
        // open-space edit start
        HasData = hasData;
        TargetEntity = targetEntity;
        PatientName = patientName;
        SpeciesName = speciesName;
        MobState = mobState;
        Condition = condition;
        Temperature = temperature;
        BloodLevel = bloodLevel;
        BloodAmount = bloodAmount;
        TotalDamage = totalDamage;
        AirlossDamage = airlossDamage;
        ToxinDamage = toxinDamage;
        BurnDamage = burnDamage;
        BruteDamage = bruteDamage;
        Pulse = pulse;
        GenesStable = genesStable;
        Reagents = reagents;
        Dependencies = dependencies;
        ScanMode = scanMode;
        Bleeding = bleeding;
        Unrevivable = unrevivable;
        // open-space edit end
    }
}
