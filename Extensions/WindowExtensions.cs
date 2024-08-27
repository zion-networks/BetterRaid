using Avalonia.Controls;

namespace BetterRaid.Extensions;

public static class WindowExtensions
{
    public static void CenterToOwner(this Window window)
    {
        var owner = window.Owner as Window;

        if (owner == null)
            return;

        window.Position = new Avalonia.PixelPoint(
            (int)(owner.Position.X + owner.Width / 2 - window.Width / 2),
            (int)(owner.Position.Y + owner.Height / 2 - window.Height / 2)
        );
    }
}