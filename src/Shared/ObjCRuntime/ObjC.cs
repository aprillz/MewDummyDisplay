using System.Runtime.InteropServices;

namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>Entry points into the Objective-C runtime.</summary>
/// <remarks>
/// objc_msgSend uses a different calling convention depending on argument types,
/// so every signature gets its own declaration. Never cast one general purpose
/// declaration and reuse it across signatures.
/// </remarks>
internal static class ObjC
{
    private const string LIBOBJC = "/usr/lib/libobjc.A.dylib";
    private const string LIBSYSTEM = "/usr/lib/libSystem.B.dylib";

    [DllImport(LIBOBJC)]
    internal static extern nint objc_getClass([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(LIBOBJC)]
    internal static extern nint sel_registerName([MarshalAs(UnmanagedType.LPUTF8Str)] string name);

    [DllImport(LIBOBJC)]
    internal static extern nint class_getInstanceMethod(nint cls, nint selector);

    [DllImport(LIBSYSTEM)]
    internal static extern nint dlopen([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int mode);

    [DllImport(LIBSYSTEM)]
    internal static extern nint dlsym(nint handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string symbol);

    // objc_msgSend overloads. Names follow Send<return>_<arguments>.

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern nint SendPtr(nint receiver, nint selector);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern nint SendPtr_Ptr(nint receiver, nint selector, nint arg);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid(nint receiver, nint selector);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid_Ptr(nint receiver, nint selector, nint arg);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid_UInt(nint receiver, nint selector, uint arg);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid_Double(nint receiver, nint selector, double arg);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid_Point(nint receiver, nint selector, CGPoint arg);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern void SendVoid_Size(nint receiver, nint selector, CGSize arg);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern byte SendBool_Ptr(nint receiver, nint selector, nint arg);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern uint SendUInt(nint receiver, nint selector);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern double SendDouble(nint receiver, nint selector);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern nint SendPtr_UInt_UInt_Double(nint receiver, nint selector, uint width, uint height, double refreshRate);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern nint SendPtr_Ptr_UIntPtr(nint receiver, nint selector, nint objects, nuint count);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern nint SendPtr_Utf8(nint receiver, nint selector, [MarshalAs(UnmanagedType.LPUTF8Str)] string arg);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern nint SendPtr_Ptr_Ptr(nint receiver, nint selector, nint first, nint second);

    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    internal static extern nint SendPtr_Bool(nint receiver, nint selector, byte arg);

    /// <summary>Registers a selector. Callers cache the result in a static readonly field.</summary>
    internal static nint Sel(string name)
    {
        nint selector = sel_registerName(name);
        if (selector == 0)
        {
            throw new InvalidOperationException($"Failed to register selector: {name}");
        }
        return selector;
    }

    /// <summary>Looks up a class, returning 0 when it does not exist.</summary>
    internal static nint TryClass(string name) => objc_getClass(name);

    /// <summary>Looks up a class and throws when it does not exist.</summary>
    internal static nint RequireClass(string name)
    {
        nint cls = objc_getClass(name);
        if (cls == 0)
        {
            throw new PlatformNotSupportedException($"Objective-C class not found: {name}");
        }
        return cls;
    }

    /// <summary>Reports whether the class implements the named instance method.</summary>
    internal static bool HasInstanceMethod(nint cls, string selectorName)
        => cls != 0 && class_getInstanceMethod(cls, sel_registerName(selectorName)) != 0;

    // Selectors shared across the interop layer.

    internal static readonly nint SelAlloc = Sel("alloc");
    internal static readonly nint SelInit = Sel("init");
    internal static readonly nint SelRelease = Sel("release");
    internal static readonly nint SelRetain = Sel("retain");

    /// <summary>Sends alloc then init, yielding an object the caller owns.</summary>
    internal static nint AllocInit(nint cls) => SendPtr(SendPtr(cls, SelAlloc), SelInit);

    /// <summary>Sends release when the handle is not null.</summary>
    internal static void Release(nint handle)
    {
        if (handle != 0)
        {
            SendVoid(handle, SelRelease);
        }
    }

    /// <summary>Creates an owned NSString from UTF-8 text.</summary>
    internal static nint NewString(string value)
    {
        // stringWithUTF8String: returns an autoreleased object, so retain it to take ownership.
        nint autoreleased = SendPtr_Utf8(_nsString, _selStringWithUtf8, value);
        return SendPtr(autoreleased, SelRetain);
    }

    /// <summary>Reads an NSString as a managed string.</summary>
    internal static string ReadString(nint nsString)
    {
        if (nsString == 0)
        {
            return "";
        }
        nint utf8 = SendPtr(nsString, _selUtf8String);
        return utf8 == 0 ? "" : Marshal.PtrToStringUTF8(utf8) ?? "";
    }

    /// <summary>Creates an owned NSArray from a span of object pointers.</summary>
    internal static nint NewArray(ReadOnlySpan<nint> items)
    {
        unsafe
        {
            fixed (nint* pointer = items)
            {
                // arrayWithObjects:count: returns an autoreleased object, so retain it.
                nint autoreleased = SendPtr_Ptr_UIntPtr(_nsArray, _selArrayWithObjects, (nint)pointer, (nuint)items.Length);
                return SendPtr(autoreleased, SelRetain);
            }
        }
    }

    /// <summary>Creates an owned single entry NSDictionary.</summary>
    internal static nint NewDictionary(nint key, nint value)
    {
        // dictionaryWithObject:forKey: returns an autoreleased object, so retain it.
        nint autoreleased = SendPtr_Ptr_Ptr(_nsDictionary, _selDictionaryWithObject, value, key);
        return SendPtr(autoreleased, SelRetain);
    }

    /// <summary>Creates an owned NSNumber holding a boolean.</summary>
    internal static nint NewBoolean(bool value)
    {
        nint autoreleased = SendPtr_Bool(_nsNumber, _selNumberWithBool, value ? (byte)1 : (byte)0);
        return SendPtr(autoreleased, SelRetain);
    }

    private static readonly nint _nsString = RequireClass("NSString");
    private static readonly nint _nsArray = RequireClass("NSArray");
    private static readonly nint _selStringWithUtf8 = Sel("stringWithUTF8String:");
    private static readonly nint _selUtf8String = Sel("UTF8String");
    private static readonly nint _nsDictionary = RequireClass("NSDictionary");
    private static readonly nint _nsNumber = RequireClass("NSNumber");
    private static readonly nint _selArrayWithObjects = Sel("arrayWithObjects:count:");
    private static readonly nint _selDictionaryWithObject = Sel("dictionaryWithObject:forKey:");
    private static readonly nint _selNumberWithBool = Sel("numberWithBool:");
}
