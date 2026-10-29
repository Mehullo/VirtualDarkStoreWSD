using VirtualDarkStore.Environment;

namespace VirtualDarkStore.Interaction
{
    /// <summary>
    /// Shared abstraction for picker interaction implementations across Desktop and VR modes.
    /// Ensures 100% architectural parity for UI feedback and activity classification.
    /// </summary>
    public interface IPickerInteraction
    {
        PickableItemVisual HoveredItem { get; }
        PackingStation HoveredPackingStation { get; }
        string CurrentPrompt { get; }
        float ReachDistance { get; }
    }
}
