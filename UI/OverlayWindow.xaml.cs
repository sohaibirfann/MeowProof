using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MeowProof.Helpers;

namespace MeowProof.UI;

public partial class OverlayWindow : Window
{
    private readonly System.Windows.Forms.Screen _screen;
    internal bool AllowClose { get; set; }

    public OverlayWindow(System.Windows.Forms.Screen screen)
    {
        _screen = screen;
        InitializeComponent();
        SourceInitialized += (_, _) => EnableAcrylic(new WindowInteropHelper(this).Handle);
        Loaded += (_, _) => CoverScreen();
    }

    private void CoverScreen()
    {
        // Convert pixel bounds to WPF DIPs using system DPI.
        using var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
        double dpiX = g.DpiX / 96.0;
        double dpiY = g.DpiY / 96.0;
        Left   = _screen.Bounds.X / dpiX;
        Top    = _screen.Bounds.Y / dpiY;
        Width  = _screen.Bounds.Width  / dpiX;
        Height = _screen.Bounds.Height / dpiY;
    }

    private static void EnableAcrylic(IntPtr hwnd)
    {
        // 0xE0 alpha (~88% opaque dark tint), 0x101010 very dark warm grey
        var accent = new Win32.AccentPolicy { AccentState = 4, GradientColor = 0xE0101010 };
        int size = Marshal.SizeOf(accent);
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(accent, ptr, false);
            var data = new Win32.WindowCompositionAttributeData
            {
                Attribute  = 19,
                Data       = ptr,
                SizeOfData = size
            };
            Win32.SetWindowCompositionAttribute(hwnd, ref data);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!AllowClose)
            e.Cancel = true;
    }
}
