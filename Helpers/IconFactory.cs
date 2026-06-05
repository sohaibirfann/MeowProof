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
    // Palette from the UI mockup (warm clay/terracotta accent direction).
    private static readonly Color Accent = ColorTranslator.FromHtml("#CC7C57"); // warm clay (locked)
    private static readonly Color Muted = ColorTranslator.FromHtml("#8C8478");  // warm grey (unlocked)

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr handle);

    /// <summary>Creates a cat-loaf tray icon. Clay when locked, grey when unlocked.</summary>
    public static Icon CreateTrayIcon(bool locked)
    {
        Color color = locked ? Accent : Muted;

        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var brush = new SolidBrush(color);
            var cream = ColorTranslator.FromHtml("#FBF7F1");

            // Draw the loaf in its native 116-unit space, scaled to fit 32px.
            var state = g.Save();
            g.TranslateTransform(1f, 2.5f);
            g.ScaleTransform(0.25f, 0.25f);

            // Ears.
            g.FillPolygon(brush, new[] { new PointF(30, 56), new PointF(33, 18), new PointF(58, 50) });
            g.FillPolygon(brush, new[] { new PointF(58, 50), new PointF(83, 18), new PointF(86, 56) });

            // Loaf body.
            using (var body = new GraphicsPath())
            {
                body.AddBezier(12, 100, 12, 54, 30, 42, 58, 42);
                body.AddBezier(58, 42, 86, 42, 104, 54, 104, 100);
                body.CloseFigure();
                g.FillPath(brush, body);
            }

            // Tail curl.
            using (var tail = new Pen(color, 11f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                g.DrawBezier(tail, 104, 92, 119, 93, 118, 74, 105, 75);

            // Calm face (cream eyes + nose).
            using (var facePen = new Pen(cream, 4f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawBezier(facePen, 40, 74, 44, 77.33f, 48, 77.33f, 52, 74);
                g.DrawBezier(facePen, 64, 74, 68, 77.33f, 72, 77.33f, 76, 74);
            }
            using (var nose = new SolidBrush(cream))
                g.FillPolygon(nose, new[] { new PointF(58, 79), new PointF(54, 83), new PointF(62, 83) });

            g.Restore(state);

            // Lock badge when locked: accent dot at lower-right with a bg-colored padlock.
            if (locked)
            {
                using var badge = new SolidBrush(Accent);
                g.FillEllipse(badge, 19, 19, 12, 12);
                using var dark = new SolidBrush(ColorTranslator.FromHtml("#FBF7F1"));
                g.FillRectangle(dark, 23, 25, 5, 4);        // lock body
                using var shackle = new Pen(ColorTranslator.FromHtml("#FBF7F1"), 1.3f);
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
