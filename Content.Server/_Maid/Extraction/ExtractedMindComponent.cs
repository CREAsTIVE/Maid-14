namespace Content.Server._Maid.Extraction;

/// <summary>
/// Marks a mind whose character left the round through extraction, so they count as survived.
/// </summary>
[RegisterComponent]
public sealed partial class ExtractedMindComponent : Component;
