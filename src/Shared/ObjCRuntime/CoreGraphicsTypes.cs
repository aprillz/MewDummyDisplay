using System.Runtime.InteropServices;

namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>CoreGraphics CGPoint. Two doubles, passed in registers on arm64 and x86-64.</summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CGPoint(double x, double y)
{
    internal readonly double X = x;
    internal readonly double Y = y;
}

/// <summary>CoreGraphics CGSize.</summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CGSize(double width, double height)
{
    internal readonly double Width = width;
    internal readonly double Height = height;
}

/// <summary>CoreGraphics CGRect.</summary>
[StructLayout(LayoutKind.Sequential)]
internal readonly struct CGRect(double x, double y, double width, double height)
{
    internal readonly CGPoint Origin = new(x, y);
    internal readonly CGSize Size = new(width, height);
}
