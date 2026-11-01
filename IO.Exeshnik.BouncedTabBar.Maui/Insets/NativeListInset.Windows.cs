using WControl = Microsoft.UI.Xaml.Controls.Control;
using WThickness = Microsoft.UI.Xaml.Thickness;

namespace IO.Exeshnik.BouncedTabBar.Maui;

internal static partial class NativeListInset
{
    // The WinUI list templates apply Padding inside their ScrollViewer, so items scroll through it.
    public static partial void SetBottomInset(View list, double inset)
    {
        if (list.Handler?.PlatformView is not WControl control || control.Padding.Bottom.Equals(inset))
            return;

        var padding = control.Padding;
        control.Padding = new WThickness(padding.Left, padding.Top, padding.Right, inset);
    }

    public static partial bool RespectsBottomSafeArea(View list)
    {
        return false;
    }
}
