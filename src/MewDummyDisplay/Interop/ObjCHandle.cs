using System.Runtime.InteropServices;

namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>An Objective-C object the caller owns. Dispose sends release.</summary>
/// <remarks>
/// C# has no ARC. Objects obtained through alloc/init carry a +1 retain count and
/// must be released explicitly, and a double release crashes immediately. Ownership
/// is expressed only through this type so raw handles are never passed around.
/// </remarks>
internal sealed class ObjCHandle : SafeHandle
{
    internal ObjCHandle(nint value) : base(0, ownsHandle: true) => SetHandle(value);

    public override bool IsInvalid => handle == 0;

    internal nint Value => handle;

    protected override bool ReleaseHandle()
    {
        ObjC.Release(handle);
        return true;
    }
}
