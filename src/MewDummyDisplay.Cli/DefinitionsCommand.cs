using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Cli;

internal static class DefinitionsCommand
{
    internal static int Run()
    {
        Console.WriteLine($"{"ID",-10} {"KIND",-12} {"ASPECT",-10} {"ALL",-6} {"SHOWN",-6} {"COMMON RESOLUTIONS"}");
        foreach (DummyDefinition definition in DummyDefinitionCatalog.All())
        {
            IReadOnlyList<(int Width, int Height)> common = definition.CommonResolutions();
            int all = definition.MaxMultiplier - definition.MinMultiplier + 1;

            string preview = string.Join(", ", common.Take(4).Select(size => $"{size.Width}x{size.Height}"));
            if (common.Count > 4)
            {
                preview += $", ... {common[^1].Width}x{common[^1].Height}";
            }

            Console.WriteLine(
                $"{definition.Id,-10} " +
                $"{definition.Kind,-12} " +
                $"{$"{definition.AspectWidth}:{definition.AspectHeight}",-10} " +
                $"{all,-6} " +
                $"{common.Count,-6} " +
                preview);
        }
        return 0;
    }
}
