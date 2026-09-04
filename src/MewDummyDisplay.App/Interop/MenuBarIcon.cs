using System.Runtime.InteropServices;

namespace Aprillz.MewDummyDisplay.Interop;

/// <summary>
/// Builds the menu bar icon from the display.2 symbol: its bezels and stands as drawn, with
/// each screen filled translucently and inset from the bezel.
/// </summary>
/// <remarks>
/// The symbol has two layers, bezel and screen. As a template image the menu bar draws both
/// in one colour, which turns the monitors into solid blocks. So the layers are separated
/// through palettes: the bezel layer is drawn as is, and the screen layer is used only as a
/// shape, rasterised, shrunk by the margin, and painted at the opacity Apple gives it in
/// hierarchical rendering. Nothing about the shape is hard coded; the fill follows whatever
/// outline the symbol has, including where the rear screen gives way to the front monitor.
/// </remarks>
internal static unsafe class MenuBarIcon
{
    private const string CORE_GRAPHICS = "/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics";

    /// <summary>Raster pixels per point. Four covers a Retina menu bar with room to spare.</summary>
    private const int SCALE = 4;

    /// <summary>Gap between the bezel and the screen fill.</summary>
    private const double MARGIN_POINTS = 0.5;

    /// <summary>The opacity Apple's hierarchical rendering gives this symbol's screen layer.</summary>
    private const double SCREEN_ALPHA = 0.30;

    private const uint ALPHA_PREMULTIPLIED_LAST = 1;
    private const long COMPOSITING_SOURCE_OVER = 2;
    private const string SYMBOL_NAME = "display.2";

    [DllImport(CORE_GRAPHICS)]
    private static extern nint CGColorSpaceCreateDeviceRGB();

    [DllImport(CORE_GRAPHICS)]
    private static extern nint CGBitmapContextCreate(nint data, nuint width, nuint height, nuint bitsPerComponent, nuint bytesPerRow, nint colorSpace, uint bitmapInfo);

    [DllImport(CORE_GRAPHICS)]
    private static extern void CGContextScaleCTM(nint context, double x, double y);

    [DllImport(CORE_GRAPHICS)]
    private static extern void CGContextDrawImage(nint context, CGRect rect, nint image);

    [DllImport(CORE_GRAPHICS)]
    private static extern nint CGBitmapContextGetData(nint context);

    [DllImport(CORE_GRAPHICS)]
    private static extern nint CGBitmapContextCreateImage(nint context);

    [DllImport(CORE_GRAPHICS)]
    private static extern void CGContextRelease(nint context);

    [DllImport(CORE_GRAPHICS)]
    private static extern void CGImageRelease(nint image);

    [DllImport(CORE_GRAPHICS)]
    private static extern void CGColorSpaceRelease(nint colorSpace);

    private static readonly nint NSGraphicsContext = ObjC.RequireClass("NSGraphicsContext");

    /// <summary>The finished template image, or 0 when the symbol is unavailable.</summary>
    internal static nint Create(string accessibilityDescription)
    {
        nint symbolName = ObjC.NewString(SYMBOL_NAME);
        nint description = ObjC.NewString(accessibilityDescription);
        try
        {
            nint symbol = AppKitInterop.SendPtr_Ptr_Ptr(
                AppKitInterop.NSImage, ObjC.Sel("imageWithSystemSymbolName:accessibilityDescription:"), symbolName, description);
            if (symbol == 0)
            {
                return 0;
            }

            nint black = ObjC.SendPtr(AppKitInterop.NSColor, ObjC.Sel("blackColor"));
            nint clear = ObjC.SendPtr(AppKitInterop.NSColor, ObjC.Sel("clearColor"));
            nint bezel = WithPalette(symbol, black, clear);
            nint screen = WithPalette(symbol, clear, black);

            CGSize size = AppKitInterop.SendSize(symbol, ObjC.Sel("size"));
            int width = (int)Math.Round(size.Width * SCALE);
            int height = (int)Math.Round(size.Height * SCALE);

            nint colorSpace = CGColorSpaceCreateDeviceRGB();
            try
            {
                byte[] screenAlpha = Rasterize(screen, size, width, height, colorSpace);
                bool[] fill = Erode(screenAlpha, width, height, (int)Math.Round(MARGIN_POINTS * SCALE));

                nint fillImage = FillImage(fill, width, height, colorSpace);
                nint composed = CGBitmapContextCreate(0, (nuint)width, (nuint)height, 8, (nuint)(width * 4), colorSpace, ALPHA_PREMULTIPLIED_LAST);
                CGContextDrawImage(composed, new CGRect(0, 0, width, height), fillImage);
                CGImageRelease(fillImage);
                Draw(composed, bezel, size);

                nint cgImage = CGBitmapContextCreateImage(composed);
                CGContextRelease(composed);

                nint image = AppKitInterop.SendPtr_Ptr_Size(
                    ObjC.SendPtr(AppKitInterop.NSImage, ObjC.Sel("alloc")), ObjC.Sel("initWithCGImage:size:"), cgImage, size);
                CGImageRelease(cgImage);

                AppKitInterop.SendVoid_Byte(image, ObjC.Sel("setTemplate:"), 1);
                ObjC.SendVoid_Ptr(image, ObjC.Sel("setAccessibilityDescription:"), description);
                return image;
            }
            finally
            {
                CGColorSpaceRelease(colorSpace);
            }
        }
        finally
        {
            ObjC.Release(symbolName);
            ObjC.Release(description);
        }
    }

    /// <summary>The symbol with its two layers coloured as given; clear drops a layer.</summary>
    private static nint WithPalette(nint symbol, nint primary, nint secondary)
    {
        nint[] palette = [primary, secondary];
        nint colors;
        fixed (nint* items = palette)
        {
            colors = ObjC.SendPtr_Ptr_UIntPtr(AppKitInterop.NSArray, ObjC.Sel("arrayWithObjects:count:"), (nint)items, (nuint)palette.Length);
        }
        nint configuration = ObjC.SendPtr_Ptr(AppKitInterop.NSImageSymbolConfiguration, ObjC.Sel("configurationWithPaletteColors:"), colors);
        return ObjC.SendPtr_Ptr(symbol, ObjC.Sel("imageWithSymbolConfiguration:"), configuration);
    }

    /// <summary>Draws the image, scaled from points to raster pixels, into a bitmap context.</summary>
    private static void Draw(nint context, nint image, CGSize size)
    {
        nint graphics = AppKitInterop.SendPtr_Ptr_Bool(NSGraphicsContext, ObjC.Sel("graphicsContextWithCGContext:flipped:"), context, 0);
        ObjC.SendVoid(NSGraphicsContext, ObjC.Sel("saveGraphicsState"));
        ObjC.SendVoid_Ptr(NSGraphicsContext, ObjC.Sel("setCurrentContext:"), graphics);
        CGContextScaleCTM(context, SCALE, SCALE);
        AppKitInterop.SendVoid_Rect_Rect_Long_Double(
            image, ObjC.Sel("drawInRect:fromRect:operation:fraction:"),
            new CGRect(0, 0, size.Width, size.Height), new CGRect(0, 0, 0, 0), COMPOSITING_SOURCE_OVER, 1.0);
        ObjC.SendVoid(NSGraphicsContext, ObjC.Sel("restoreGraphicsState"));
    }

    /// <summary>The image's alpha channel at raster size.</summary>
    private static byte[] Rasterize(nint image, CGSize size, int width, int height, nint colorSpace)
    {
        nint context = CGBitmapContextCreate(0, (nuint)width, (nuint)height, 8, (nuint)(width * 4), colorSpace, ALPHA_PREMULTIPLIED_LAST);
        try
        {
            Draw(context, image, size);
            byte* pixels = (byte*)CGBitmapContextGetData(context);
            byte[] alpha = new byte[width * height];
            for (int index = 0; index < alpha.Length; index++)
            {
                alpha[index] = pixels[index * 4 + 3];
            }
            return alpha;
        }
        finally
        {
            CGContextRelease(context);
        }
    }

    /// <summary>Keeps a pixel only when every pixel within the radius is inside the shape.</summary>
    private static bool[] Erode(byte[] alpha, int width, int height, int radius)
    {
        bool[] kept = new bool[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (alpha[y * width + x] <= 128)
                {
                    continue;
                }

                bool inside = true;
                for (int dy = -radius; dy <= radius && inside; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (dx * dx + dy * dy > radius * radius)
                        {
                            continue;
                        }
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx < 0 || ny < 0 || nx >= width || ny >= height || alpha[ny * width + nx] <= 128)
                        {
                            inside = false;
                            break;
                        }
                    }
                }
                kept[y * width + x] = inside;
            }
        }
        return kept;
    }

    /// <summary>A translucent black image covering the kept pixels.</summary>
    private static nint FillImage(bool[] kept, int width, int height, nint colorSpace)
    {
        byte alpha = (byte)Math.Round(SCREEN_ALPHA * 255);
        byte[] pixels = new byte[width * height * 4];
        for (int index = 0; index < kept.Length; index++)
        {
            if (kept[index])
            {
                // Premultiplied black: colour channels stay zero, only alpha is set.
                pixels[index * 4 + 3] = alpha;
            }
        }

        fixed (byte* data = pixels)
        {
            nint context = CGBitmapContextCreate((nint)data, (nuint)width, (nuint)height, 8, (nuint)(width * 4), colorSpace, ALPHA_PREMULTIPLIED_LAST);
            nint image = CGBitmapContextCreateImage(context);
            CGContextRelease(context);
            return image;
        }
    }
}
