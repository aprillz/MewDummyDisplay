namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>Verifies that the private virtual display API surface exists on this OS.</summary>
internal static class SurfaceCheck
{
    private static readonly string[] _requiredClasses =
    [
        "CGVirtualDisplay",
        "CGVirtualDisplayDescriptor",
        "CGVirtualDisplayMode",
        "CGVirtualDisplaySettings",
    ];

    /// <summary>Returns the missing classes and selectors. An empty result means the surface is intact.</summary>
    internal static IReadOnlyList<string> FindMissing()
    {
        List<string> missing = [];

        foreach (string name in _requiredClasses)
        {
            if (ObjC.TryClass(name) == 0)
            {
                missing.Add($"class {name}");
            }
        }

        nint display = ObjC.TryClass("CGVirtualDisplay");
        nint descriptor = ObjC.TryClass("CGVirtualDisplayDescriptor");
        nint mode = ObjC.TryClass("CGVirtualDisplayMode");
        nint settings = ObjC.TryClass("CGVirtualDisplaySettings");

        Require(missing, display, "CGVirtualDisplay", "initWithDescriptor:");
        Require(missing, display, "CGVirtualDisplay", "applySettings:");
        Require(missing, display, "CGVirtualDisplay", "displayID");
        Require(missing, descriptor, "CGVirtualDisplayDescriptor", "setName:");
        Require(missing, descriptor, "CGVirtualDisplayDescriptor", "setQueue:");
        Require(missing, descriptor, "CGVirtualDisplayDescriptor", "setMaxPixelsWide:");
        Require(missing, mode, "CGVirtualDisplayMode", "initWithWidth:height:refreshRate:");
        Require(missing, settings, "CGVirtualDisplaySettings", "setHiDPI:");
        Require(missing, settings, "CGVirtualDisplaySettings", "setModes:");

        return missing;
    }

    private static void Require(List<string> missing, nint cls, string className, string selector)
    {
        if (cls != 0 && !ObjC.HasInstanceMethod(cls, selector))
        {
            missing.Add($"-[{className} {selector}]");
        }
    }
}
