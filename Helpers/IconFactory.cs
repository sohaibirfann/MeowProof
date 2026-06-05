using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace MeowProof.Helpers;

/// <summary>
/// Draws the tray icons at runtime so we don't need .ico asset files yet.
/// Later (polish step) these can be replaced with real icon-locked.ico /
/// icon-unlocked.ico files from the Assets folder.
/// </summary>
public static class IconFactory
{
    // Palette from the UI mockup (teal accent was the chosen direction).
    private static readonly Color Accent = ColorTranslator.FromHtml("#4ECDC4"); // soft teal (locked)
    private static readonly Color Muted = ColorTranslator.FromHtml("#CFD3DA");  // light grey (unlocked)

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    /// <summary>Creates a simple cat-head tray icon. Amber when locked, grey when unlocked.</summary>
    public static Icon CreateTrayIcon(bool locked)
    {
        Color color = locked ? Accent : Muted;

        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var brush = new SolidBrush(color);

            // Ears (two triangles).
            Point[] leftEar = { new(6, 12), new(11, 4), new(15, 13) };
            Point[] rightEar = { new(26, 12), new(21, 4), new(17, 13) };
            g.FillPolygon(brush, leftEar);
            g.FillPolygon(brush, rightEar);

            // Head.
            g.FillEllipse(brush, 6, 9, 20, 18);

            // Lock badge when locked: accent dot at lower-right with a bg-colored padlock.
            if (locked)
            {
                using var badge = new SolidBrush(Accent);
                g.FillEllipse(badge, 19, 19, 12, 12);
                using var dark = new SolidBrush(ColorTranslator.FromHtml("#15171b"));
                g.FillRectangle(dark, 23, 25, 5, 4);        // lock body
                using var shackle = new Pen(ColorTranslator.FromHtml("#15171b"), 1.3f);
                g.DrawArc(shackle, 23.5f, 22.5f, 4, 4, 180, 180); // shackle
            }
        }

        // Bitmap.GetHicon() leaks a GDI handle; clean it up after building the Icon.
        IntPtr hIcon = bmp.GetHicon();
        try
        {
            using var tmp = Icon.FromHandle(hIcon);
            return (Icon)tmp.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }
}
