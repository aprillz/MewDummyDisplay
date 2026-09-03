namespace Aprillz.MewDummyDisplay.Cli;

// macOS identifies a display by its serial, generates a ColorSync profile for each new
// identity, and keeps that profile forever. Command line runs that draw a random serial
// therefore leave a profile behind every single time; a few hundred test runs left a few
// hundred files. So the tool derives a stable serial from the definition id instead, and
// reuses one identity per ratio no matter how often it runs.
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
