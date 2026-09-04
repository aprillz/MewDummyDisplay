using System.Runtime.InteropServices;
using System.Text;

namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>
/// Private CoreDisplay entry point for a display's descriptive information.
/// </summary>
/// <remarks>
/// CoreGraphics has no public call that returns the name a display reports about itself;
/// NSScreen.localizedName covers only active screens. CoreDisplay_DisplayCreateInfoDictionary
/// is what System Settings and BetterDummy read, and it answers for any online display.
/// </remarks>
internal static class CoreDisplayInterop
{
    private const string CORE_DISPLAY = "/System/Library/Frameworks/CoreDisplay.framework/CoreDisplay";
    private const string CORE_FOUNDATION = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const uint CF_STRING_ENCODING_UTF8 = 0x08000100;
    private const string PRODUCT_NAME_KEY = "DisplayProductName";
    private const string PREFERRED_LOCALE = "en_US";

    private static bool? _available;

    [DllImport(CORE_DISPLAY)]
    private static extern nint CoreDisplay_DisplayCreateInfoDictionary(uint display);

    [DllImport(CORE_FOUNDATION)]
    private static extern nint CFStringCreateWithCString(nint allocator, string value, uint encoding);

    [DllImport(CORE_FOUNDATION)]
    private static extern nint CFDictionaryGetValue(nint dictionary, nint key);

    [DllImport(CORE_FOUNDATION)]
    private static extern nint CFDictionaryGetCount(nint dictionary);

    [DllImport(CORE_FOUNDATION)]
    private static extern void CFDictionaryGetKeysAndValues(nint dictionary, nint[] keys, nint[] values);

    [DllImport(CORE_FOUNDATION)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool CFStringGetCString(nint value, byte[] buffer, nint bufferSize, uint encoding);

    /// <summary>Reports whether the private entry point exists.</summary>
    internal static bool IsAvailable()
    {
        if (_available is bool known)
        {
            return known;
        }

        nint library = ObjC.dlopen(CORE_DISPLAY, 2);
        _available = library != 0 && ObjC.dlsym(library, "CoreDisplay_DisplayCreateInfoDictionary") != 0;
        return _available.Value;
    }

    /// <summary>
    /// The product name the display reports, or null when it has none or the call is unavailable.
    /// </summary>
    /// <remarks>
    /// The name is keyed by locale. English is preferred so the same display reads the same
    /// everywhere; when it has no English entry, whichever one is there is used.
    /// </remarks>
    internal static string? DisplayName(uint displayId)
    {
        if (!IsAvailable())
        {
            return null;
        }

        nint info = CoreDisplay_DisplayCreateInfoDictionary(displayId);
        if (info == 0)
        {
            return null;
        }

        try
        {
            nint key = CFStringCreateWithCString(0, PRODUCT_NAME_KEY, CF_STRING_ENCODING_UTF8);
            try
            {
                nint names = CFDictionaryGetValue(info, key);
                if (names == 0)
                {
                    return null;
                }

                nint preferredKey = CFStringCreateWithCString(0, PREFERRED_LOCALE, CF_STRING_ENCODING_UTF8);
                try
                {
                    string? preferred = ReadString(CFDictionaryGetValue(names, preferredKey));
                    if (preferred is not null)
                    {
                        return preferred;
                    }
                }
                finally
                {
                    CoreGraphicsInterop.CFRelease(preferredKey);
                }

                int count = (int)CFDictionaryGetCount(names);
                if (count == 0)
                {
                    return null;
                }

                nint[] keys = new nint[count];
                nint[] values = new nint[count];
                CFDictionaryGetKeysAndValues(names, keys, values);
                return ReadString(values[0]);
            }
            finally
            {
                CoreGraphicsInterop.CFRelease(key);
            }
        }
        finally
        {
            CoreGraphicsInterop.CFRelease(info);
        }
    }

    private static string? ReadString(nint value)
    {
        if (value == 0)
        {
            return null;
        }

        byte[] buffer = new byte[256];
        if (!CFStringGetCString(value, buffer, buffer.Length, CF_STRING_ENCODING_UTF8))
        {
            return null;
        }

        int length = Array.IndexOf(buffer, (byte)0);
        string text = Encoding.UTF8.GetString(buffer, 0, length < 0 ? buffer.Length : length).Trim();
        return text.Length > 0 ? text : null;
    }
}
