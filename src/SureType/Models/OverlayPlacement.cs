using System.Drawing;

namespace SureType.Models;

public static class OverlayPlacement
{
    public static Point Calculate(Rectangle area, Point cursor, int size, int margin, OverlayPosition position)
    {
        int left, top;
        if (position == OverlayPosition.NearMouse)
        {
            left = cursor.X + margin;
            top = cursor.Y + margin;
            if (left + size > area.Right) left = cursor.X - margin - size;
            if (top + size > area.Bottom) top = cursor.Y - margin - size;
        }
        else
        {
            left = position is OverlayPosition.TopLeft or OverlayPosition.BottomLeft ? area.Left + margin : area.Right - size - margin;
            top = position is OverlayPosition.TopLeft or OverlayPosition.TopRight ? area.Top + margin : area.Bottom - size - margin;
        }
        return new(Math.Clamp(left, area.Left, Math.Max(area.Left, area.Right - size)),
            Math.Clamp(top, area.Top, Math.Max(area.Top, area.Bottom - size)));
    }
}
