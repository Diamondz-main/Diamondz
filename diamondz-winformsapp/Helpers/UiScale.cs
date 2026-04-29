namespace DiamondzWinForms.Helpers;

internal static class UiScale
{
    public static int Dpi(Control control, int value)
    {
        var dpi = control.DeviceDpi > 0 ? control.DeviceDpi : 96;
        return Math.Max(1, (int)Math.Round(value * dpi / 96f));
    }

    public static Size Dpi(Control control, int width, int height)
    {
        return new Size(Dpi(control, width), Dpi(control, height));
    }

    public static Padding Dpi(Control control, int left, int top, int right, int bottom)
    {
        return new Padding(Dpi(control, left), Dpi(control, top), Dpi(control, right), Dpi(control, bottom));
    }

    public static Font FitFont(
        Control control,
        string text,
        string familyName,
        float preferredSize,
        float minimumSize,
        FontStyle style,
        int maxWidth,
        int maxHeight)
    {
        var availableWidth = Math.Max(1, maxWidth);
        var availableHeight = Math.Max(1, maxHeight);
        for (var size = preferredSize; size >= minimumSize; size -= 0.5f)
        {
            using var testFont = new Font(familyName, size, style, GraphicsUnit.Point);
            var measured = TextRenderer.MeasureText(
                text,
                testFont,
                new Size(10000, 10000),
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            if (measured.Width <= availableWidth && measured.Height <= availableHeight)
                return new Font(familyName, size, style, GraphicsUnit.Point);
        }

        return new Font(familyName, minimumSize, style, GraphicsUnit.Point);
    }
}
