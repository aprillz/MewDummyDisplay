using Aprillz.MewDummyDisplay;

namespace Aprillz.MewDummyDisplay.Cli;

internal static class Diagnostics
{
    /// <summary>Prints the private API surface state. Returns false when dummies cannot be created.</summary>
    internal static bool ReportSurface()
    {
        if (DummyManager.IsSupported)
        {
            return true;
        }

        Console.Error.WriteLine("The private virtual display API surface is incomplete on this OS.");
        foreach (string entry in DummyManager.MissingSurface)
        {
            Console.Error.WriteLine($"  missing: {entry}");
        }
        Console.Error.WriteLine("Re-run reference/private-api/dump-private-api.m and diff against the stored dump.");
        return false;
    }

    internal static void PrintDummy(Dummy dummy)
    {
        Console.WriteLine($"Created   : {dummy.Name}");
        Console.WriteLine($"DisplayID : {dummy.DisplayId}");
        Console.WriteLine($"Serial    : {dummy.SerialNumber:X8}");
        Console.WriteLine($"HiDPI     : {dummy.Spec.HiDpi}");
        Console.WriteLine($"Ready     : {dummy.IsReady}");
    }

    internal static void PrintDisplaySnapshot(uint displayId)
    {
        bool online = DisplayCatalog.Online().Any(display => display.DisplayId == displayId);
        DisplayInfo info = DisplayCatalog.Describe(displayId);
        Console.WriteLine("As seen by CoreGraphics:");
        Console.WriteLine($"  online  : {online}");
        Console.WriteLine($"  points  : {info.Width}x{info.Height}");
        Console.WriteLine($"  pixels  : {info.PixelWidth}x{info.PixelHeight}");
        Console.WriteLine($"  refresh : {info.RefreshRate:0.##} Hz");
        Console.WriteLine($"  hidpi   : {info.IsHiDpi}");
        Console.WriteLine($"  rotation: {info.Rotation:0.##}");
        Console.WriteLine($"  vendor  : {info.VendorId:X} model: {info.ModelId:X} serial: {info.SerialNumber:X}");
    }
}
