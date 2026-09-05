namespace Aprillz.MewDummyDisplay.Cli;

// Derives a serial from the definition id so that repeated runs reuse one identity per
// ratio. See DummyRecord.SerialNumber for why that matters.
internal static class ToolSerials
{
    /// <summary>High bit set, to keep tool identities away from the app's random ones.</summary>
    private const uint TOOL_MARKER = 0x7D000000;

    internal static uint For(string definitionId)
    {
        uint hash = 2166136261;
        foreach (char character in definitionId)
        {
            hash = (hash ^ character) * 16777619;
        }
        return TOOL_MARKER | (hash & 0x00FFFFFF);
    }
}
