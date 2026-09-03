namespace Aprillz.MewDummyDisplay.App;

internal static class FluentExtensions
{
    /// <summary>Runs an action and returns the receiver, so refs captured mid-tree can be stored.</summary>
    internal static T Also<T>(this T value, Action action)
    {
        action();
        return value;
    }
}
