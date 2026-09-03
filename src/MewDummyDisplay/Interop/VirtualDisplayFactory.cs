namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>The only place that calls the private CGVirtualDisplay classes.</summary>
internal static class VirtualDisplayFactory
{
    // Chromaticity coordinates taken from the Generic RGB color profile.
    private static readonly CGPoint _whitePoint = new(0.950, 1.000);
    private static readonly CGPoint _redPrimary = new(0.454, 0.242);
    private static readonly CGPoint _greenPrimary = new(0.353, 0.674);
    private static readonly CGPoint _bluePrimary = new(0.157, 0.084);

    private static readonly nint _clsDisplay = ObjC.RequireClass("CGVirtualDisplay");
    private static readonly nint _clsDescriptor = ObjC.RequireClass("CGVirtualDisplayDescriptor");
    private static readonly nint _clsMode = ObjC.RequireClass("CGVirtualDisplayMode");
    private static readonly nint _clsSettings = ObjC.RequireClass("CGVirtualDisplaySettings");

    private static readonly nint _selSetQueue = ObjC.Sel("setQueue:");
    private static readonly nint _selSetName = ObjC.Sel("setName:");
    private static readonly nint _selSetWhitePoint = ObjC.Sel("setWhitePoint:");
    private static readonly nint _selSetRedPrimary = ObjC.Sel("setRedPrimary:");
    private static readonly nint _selSetGreenPrimary = ObjC.Sel("setGreenPrimary:");
    private static readonly nint _selSetBluePrimary = ObjC.Sel("setBluePrimary:");
    private static readonly nint _selSetSizeInMillimeters = ObjC.Sel("setSizeInMillimeters:");
    private static readonly nint _selSetMaxPixelsWide = ObjC.Sel("setMaxPixelsWide:");
    private static readonly nint _selSetMaxPixelsHigh = ObjC.Sel("setMaxPixelsHigh:");
    private static readonly nint _selSetSerialNum = ObjC.Sel("setSerialNum:");
    private static readonly nint _selSetProductId = ObjC.Sel("setProductID:");
    private static readonly nint _selSetVendorId = ObjC.Sel("setVendorID:");
    private static readonly nint _selInitWithDescriptor = ObjC.Sel("initWithDescriptor:");
    private static readonly nint _selInitMode = ObjC.Sel("initWithWidth:height:refreshRate:");
    private static readonly nint _selSetHiDpi = ObjC.Sel("setHiDPI:");
    private static readonly nint _selSetModes = ObjC.Sel("setModes:");
    private static readonly nint _selApplySettings = ObjC.Sel("applySettings:");
    private static readonly nint _selDisplayId = ObjC.Sel("displayID");

    /// <summary>Creates a virtual display, or returns null when the system refuses it.</summary>
    internal static VirtualDisplay? Create(VirtualDisplayRequest request)
    {
        nint queue = CoreGraphicsInterop.dispatch_get_global_queue(CoreGraphicsInterop.QOS_CLASS_USER_INTERACTIVE, 0);

        ObjCHandle descriptor = new(ObjC.AllocInit(_clsDescriptor));
        ObjCHandle? name = null;
        ObjCHandle? modeArray = null;
        ObjCHandle? settings = null;
        List<ObjCHandle> modes = [];
        ObjCHandle? display = null;

        try
        {
            name = new ObjCHandle(ObjC.NewString(request.Name));

            ObjC.SendVoid_Ptr(descriptor.Value, _selSetQueue, queue);
            ObjC.SendVoid_Ptr(descriptor.Value, _selSetName, name.Value);
            ObjC.SendVoid_Point(descriptor.Value, _selSetWhitePoint, _whitePoint);
            ObjC.SendVoid_Point(descriptor.Value, _selSetRedPrimary, _redPrimary);
            ObjC.SendVoid_Point(descriptor.Value, _selSetGreenPrimary, _greenPrimary);
            ObjC.SendVoid_Point(descriptor.Value, _selSetBluePrimary, _bluePrimary);
            ObjC.SendVoid_Size(descriptor.Value, _selSetSizeInMillimeters, request.PhysicalSize);
            ObjC.SendVoid_UInt(descriptor.Value, _selSetMaxPixelsWide, request.MaxPixelsWide);
            ObjC.SendVoid_UInt(descriptor.Value, _selSetMaxPixelsHigh, request.MaxPixelsHigh);
            ObjC.SendVoid_UInt(descriptor.Value, _selSetSerialNum, request.SerialNumber);
            ObjC.SendVoid_UInt(descriptor.Value, _selSetProductId, request.ProductId);
            ObjC.SendVoid_UInt(descriptor.Value, _selSetVendorId, request.VendorId);

            nint created = ObjC.SendPtr_Ptr(ObjC.SendPtr(_clsDisplay, ObjC.SelAlloc), _selInitWithDescriptor, descriptor.Value);
            if (created == 0)
            {
                return null;
            }
            display = new ObjCHandle(created);

            Span<nint> modeHandles = stackalloc nint[request.Modes.Count];
            for (int index = 0; index < request.Modes.Count; index++)
            {
                VirtualDisplayMode mode = request.Modes[index];
                nint modeHandle = ObjC.SendPtr_UInt_UInt_Double(
                    ObjC.SendPtr(_clsMode, ObjC.SelAlloc), _selInitMode, mode.Width, mode.Height, mode.RefreshRate);
                if (modeHandle == 0)
                {
                    return null;
                }
                modes.Add(new ObjCHandle(modeHandle));
                modeHandles[index] = modeHandle;
            }

            modeArray = new ObjCHandle(ObjC.NewArray(modeHandles));

            settings = new ObjCHandle(ObjC.AllocInit(_clsSettings));
            ObjC.SendVoid_UInt(settings.Value, _selSetHiDpi, request.HiDpi ? 1u : 0u);
            ObjC.SendVoid_Ptr(settings.Value, _selSetModes, modeArray.Value);

            if (ObjC.SendBool_Ptr(display.Value, _selApplySettings, settings.Value) == 0)
            {
                return null;
            }

            uint displayId = ObjC.SendUInt(display.Value, _selDisplayId);
            VirtualDisplay result = new(display, request, displayId);

            // Ownership moved into the result, so the finally block must not release it.
            display = null;
            return result;
        }
        finally
        {
            display?.Dispose();
            settings?.Dispose();
            modeArray?.Dispose();
            foreach (ObjCHandle mode in modes)
            {
                mode.Dispose();
            }
            name?.Dispose();
            descriptor.Dispose();
        }
    }
}

/// <summary>A fully resolved request to create one virtual display.</summary>
internal sealed record VirtualDisplayRequest(
    string Name,
    uint SerialNumber,
    uint VendorId,
    uint ProductId,
    CGSize PhysicalSize,
    uint MaxPixelsWide,
    uint MaxPixelsHigh,
    IReadOnlyList<VirtualDisplayMode> Modes,
    bool HiDpi);

/// <summary>One selectable mode on a virtual display.</summary>
internal readonly record struct VirtualDisplayMode(uint Width, uint Height, double RefreshRate);

/// <summary>A live virtual display. Disposing it removes the display from the system.</summary>
internal sealed class VirtualDisplay(ObjCHandle handle, VirtualDisplayRequest request, uint displayId) : IDisposable
{
    private readonly ObjCHandle _handle = handle;

    internal VirtualDisplayRequest Request { get; } = request;

    internal uint DisplayId { get; } = displayId;

    public void Dispose() => _handle.Dispose();
}
