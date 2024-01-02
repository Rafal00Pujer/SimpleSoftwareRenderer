using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace SimpleSoftwareRenderer;

internal class Window : IDisposable
{
    private const string className = nameof(Window);

    private readonly Kernel32.SafeHINSTANCE hInstance;
    private readonly Gdi32.SafeHDC frameDeviceContext;
    private readonly User32.SafeHWND windowHandle;
    private readonly GCHandle WindowProcPin;

    private Frame frame;
    private Gdi32.BITMAPINFO frameBitmapInfo;
    private Gdi32.SafeHBITMAP frameBitmap = default!;

    public bool Quit { get; private set; } = false;

    public (int width, int height) FrameSize => (frame.Width, frame.Height);

    public Window(string title, int xPosition, int yPosition,
        int width, int height)
    {
        hInstance = Kernel32.GetModuleHandle(null);
        frameDeviceContext = Gdi32.CreateCompatibleDC(nint.Zero);

        var bmiHeader = Gdi32.BITMAPINFOHEADER.Default;
        bmiHeader.biPlanes = 1;
        bmiHeader.biBitCount = 32;
        bmiHeader.biCompression = Gdi32.BitmapCompressionMode.BI_RGB;

        frameBitmapInfo = new Gdi32.BITMAPINFO
        {
            bmiHeader = bmiHeader
        };

        User32.WindowProc windowProcDelegate = WindowProcessMessage;
        WindowProcPin = GCHandle.Alloc(windowProcDelegate);

        var windowClass = new User32.WNDCLASS
        {
            lpszClassName = className,
            hInstance = hInstance,
            hIcon = WindowClass.StdAppIcon,
            hCursor = WindowClass.StdArrowCursor,
            lpfnWndProc = windowProcDelegate
        };

        User32.RegisterClass(windowClass);

        var style = User32.WindowStyles.WS_CAPTION
            | User32.WindowStyles.WS_SYSMENU
            | User32.WindowStyles.WS_VISIBLE;

        var rect = RECT.Empty;
        rect.Left = xPosition;
        rect.Top = yPosition;
        rect.Width = width;
        rect.Height = height;

        User32.AdjustWindowRect(ref rect, style, false);

        windowHandle = User32.CreateWindow(className, title,
            style,
            rect.left, rect.top, rect.Width, rect.Height,
            nint.Zero, nint.Zero, hInstance, nint.Zero);

        User32.ShowWindow(windowHandle, ShowWindowCommand.SW_SHOWDEFAULT);
    }

    public void Run()
    {
        while (User32.PeekMessage(out MSG message, nint.Zero, 0u, 0u, User32.PM.PM_REMOVE))
        {
            User32.TranslateMessage(in message);
            User32.DispatchMessage(in message);
        }
    }

    private uint p = 0;

    public void Draw(byte[,,] pixelsValues)
    {
        if (pixelsValues.GetLength(0) != frame.Height
            || pixelsValues.GetLength(1) != frame.Width
            || pixelsValues.GetLength(2) != 3)
        {
            throw new IndexOutOfRangeException();
        }

        Span<uint> pixels;

        unsafe
        {
            pixels = new Span<uint>(frame.Pixels.ToPointer(), frame.Width * frame.Height);
        }

        for (var h = 0; h < frame.Height; h++)
        {
            for (var w = 0; w < frame.Width; w++)
            {
                uint pixelValue = 0;

                for (var i = 0; i < 3; i++)
                {
                    pixelValue <<= 8;
                    pixelValue |= pixelsValues[h, w, i];
                }

                pixels[h * frame.Width + w] = pixelValue;
            }
        }

        //for (var i = 0; i < 1; i++)
        //{
        //    pixels[(int)(p++) % (frame.Width * frame.Height)] = (uint)Random.Shared.Next();
        //    pixels[(int)(uint)Random.Shared.Next() % (frame.Width * frame.Height)] = (uint)0;
        //}

        User32.InvalidateRect(windowHandle, null, false);
        User32.UpdateWindow(windowHandle);
    }

    private nint WindowProcessMessage(HWND hwnd, uint msg, nint wParam, nint lParam)
    {
        switch ((User32.WindowMessage)msg)
        {
            case User32.WindowMessage.WM_QUIT:
            case User32.WindowMessage.WM_DESTROY:
                Quit = true;
                break;

            case User32.WindowMessage.WM_PAINT:

                var deviceContext = User32.BeginPaint(windowHandle, out User32.PAINTSTRUCT paint);

                Gdi32.BitBlt(deviceContext,
                    paint.rcPaint.left, paint.rcPaint.top,
                    paint.rcPaint.right - paint.rcPaint.left, paint.rcPaint.bottom - paint.rcPaint.top,
                    frameDeviceContext,
                    paint.rcPaint.left, paint.rcPaint.top,
                    Gdi32.RasterOperationMode.SRCCOPY);

                User32.EndPaint(windowHandle, in paint);

                break;

            case User32.WindowMessage.WM_SIZE:

                frameBitmapInfo.bmiHeader.biWidth = Macros.LOWORD(lParam);
                frameBitmapInfo.bmiHeader.biHeight = Macros.HIWORD(lParam);

                frameBitmap?.Dispose();

                frameBitmap = Gdi32.CreateDIBSection(nint.Zero,
                    in frameBitmapInfo,
                    Gdi32.DIBColorMode.DIB_RGB_COLORS,
                    out nint pixels,
                    nint.Zero, 0);

                Gdi32.SelectObject(frameDeviceContext, frameBitmap);

                frame.Width = Macros.LOWORD(lParam);
                frame.Height = Macros.HIWORD(lParam);
                frame.Pixels = pixels;

                break;

            default:
                return User32.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        return 0;
    }

    private bool _disposed;

    public void Dispose()
    {
        // Dispose of unmanaged resources.
        Dispose(true);
        // Suppress finalization.
        GC.SuppressFinalize(this);
    }

    ~Window() => Dispose(false);

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            // dispose managed state (managed objects).
            hInstance.Dispose();
            frameDeviceContext.Dispose();
            windowHandle.Dispose();
            frameBitmap.Dispose();
        }

        // free unmanaged resources (unmanaged objects) and override a finalizer below.
        User32.UnregisterClass(className, hInstance);
        WindowProcPin.Free();

        // set large fields to null.

        _disposed = true;
    }

    public struct Frame
    {
        public int Width { get; set; }

        public int Height { get; set; }

        public nint Pixels { get; set; }
    }
}
