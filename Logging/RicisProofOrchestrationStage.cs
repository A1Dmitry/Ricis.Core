namespace Ricis.Core.Logging;

/// <summary>
/// Marker type for events emitted by generic proof orchestration before control
/// passes to concrete RICIS visitors or specialised handlers. The type exists
/// because a static extension container cannot be used as a generic argument.
/// </summary>
public sealed class RicisProofOrchestrationStage
{
    private RicisProofOrchestrationStage()
    {
    }
}
