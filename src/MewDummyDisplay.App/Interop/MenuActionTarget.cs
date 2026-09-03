using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>
/// Receives NSMenuItem actions in C#.
/// </summary>
/// <remarks>
/// NSMenuItem dispatches through a selector on a target object, and there is no
/// Objective-C class to point at from managed code. So one is built at runtime with
/// objc_allocateClassPair and given a single method backed by an UnmanagedCallersOnly
/// function pointer, which is also the only shape NativeAOT accepts.
///
/// The callback cannot close over managed state, so each menu item carries an integer
/// tag and the handlers live in a static table keyed by that tag.
/// </remarks>
internal static unsafe class MenuActionTarget
{
    private const string LIBOBJC = "/usr/lib/libobjc.A.dylib";
    private const string CLASS_NAME = "MddMenuActionTarget";

    [DllImport(LIBOBJC)]
    private static extern nint objc_allocateClassPair(nint superclass, [MarshalAs(UnmanagedType.LPUTF8Str)] string name, nuint extraBytes);

    [DllImport(LIBOBJC)]
    private static extern byte class_addMethod(nint cls, nint selector, nint implementation, [MarshalAs(UnmanagedType.LPUTF8Str)] string types);

    [DllImport(LIBOBJC)]
    private static extern void objc_registerClassPair(nint cls);

    private static readonly Lock _gate = new();
    private static readonly Dictionary<long, Action> _handlers = [];
    private static long _nextTag = 1;
    private static nint _instance;

    /// <summary>The selector menu items must use as their action.</summary>
    internal static nint Selector { get; } = ObjC.Sel("mddInvoke:");

    /// <summary>The shared target instance, created on first use.</summary>
    internal static nint Instance
    {
        get
        {
            lock (_gate)
            {
                if (_instance == 0)
                {
                    _instance = CreateInstance();
                }
                return _instance;
            }
        }
    }

    /// <summary>Registers a handler and returns the tag to put on the menu item.</summary>
    internal static long Register(Action handler)
    {
        lock (_gate)
        {
            long tag = _nextTag++;
            _handlers[tag] = handler;
            return tag;
        }
    }

    /// <summary>Forgets every handler. Called before a menu is rebuilt.</summary>
    internal static void Clear()
    {
        lock (_gate)
        {
            _handlers.Clear();
            _nextTag = 1;
        }
    }

    private static nint CreateInstance()
    {
        nint existing = ObjC.TryClass(CLASS_NAME);
        if (existing == 0)
        {
            nint cls = objc_allocateClassPair(ObjC.RequireClass("NSObject"), CLASS_NAME, 0);
            if (cls == 0)
            {
                throw new InvalidOperationException($"Could not allocate the Objective-C class {CLASS_NAME}.");
            }

            // "v@:@" is void return, self, selector, one object argument.
            delegate* unmanaged<nint, nint, nint, void> callback = &Invoke;
            if (class_addMethod(cls, Selector, (nint)callback, "v@:@") == 0)
            {
                throw new InvalidOperationException("Could not add the action method to the target class.");
            }

            objc_registerClassPair(cls);
            existing = cls;
        }

        return ObjC.AllocInit(existing);
    }

    [UnmanagedCallersOnly]
    private static void Invoke(nint self, nint selector, nint sender)
    {
        // This runs on the main thread from AppKit, so exceptions must not escape into
        // Objective-C. Anything thrown here would unwind through frames that cannot
        // handle it and take the process down.
        try
        {
            long tag = AppKitInterop.SendLong(sender, AppKitInterop.SelTag);
            Action? handler;
            lock (_gate)
            {
                _handlers.TryGetValue(tag, out handler);
            }
            handler?.Invoke();
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Menu action failed: {error}");
        }
    }
}
