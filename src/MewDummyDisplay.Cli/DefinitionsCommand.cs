using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Cli;

internal static class DefinitionsCommand
{
    internal static int Run()
    {
        Console.WriteLine($"{"ID",-10} {"KIND",-12} {"ASPECT",-10} {"STEP",-5} {"MULTIPLIERS",-14} {"RESOLUTIONS"}");
        foreach (DummyDefinition definition in DummyDefinitionCatalog.All())
        {
            (int minWidth, int minHeight) = definition.PixelsFor(definition.MinMultiplier);
            (int maxWidth, int maxHeight) = definition.PixelsFor(definition.MaxMultiplier);
            int count = definition.MaxMultiplier - definition.MinMultiplier + 1;

            Console.WriteLine(
                $"{definition.Id,-10} " +
                $"{definition.Kind,-12} " +
                $"{$"{definition.AspectWidth}:{definition.AspectHeight}",-10} " +
                $"{definition.MultiplierStep,-5} " +
                $"{$"{definition.MinMultiplier}-{definition.MaxMultiplier} ({count})",-14} " +
                $"{minWidth}x{minHeight} .. {maxWidth}x{maxHeight}");
        }
        return 0;
    }
}
