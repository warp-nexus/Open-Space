using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry;

// open-space edit start
[Serializable, NetSerializable]
public sealed class ChemAnalyzeReagentMessage(ReagentId reagentId) : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId = reagentId;
}

[Serializable, NetSerializable]
public sealed class ChemPrintAnalysisMessage(ReagentId reagentId) : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId = reagentId;
}

[Serializable, NetSerializable]
public sealed class ChemReagentAnalysisPopupMessage(ReagentId reagentId, string title, string description) : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId = reagentId;
    public readonly string Title = title;
    public readonly string Description = description;
}
// open-space edit end
