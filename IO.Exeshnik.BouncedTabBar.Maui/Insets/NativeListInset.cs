namespace IO.Exeshnik.BouncedTabBar.Maui;

internal static partial class NativeListInset
{
    // Sets the bottom inset of the native scrolling view behind a CollectionView or ListView to inset
    // (device-independent units), so items can scroll under the tab bar. MAUI leaves this inset at zero (and
    // resets it on Android), so it is written as an absolute value. Does nothing while there is no native view.
    public static partial void SetBottomInset(View list, double inset);

    // Whether the native scrolling view already adds the bottom safe area to its content inset by itself.
    public static partial bool RespectsBottomSafeArea(View list);
}
