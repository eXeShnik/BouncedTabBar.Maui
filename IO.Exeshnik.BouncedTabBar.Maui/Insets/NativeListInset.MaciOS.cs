using UIKit;

namespace IO.Exeshnik.BouncedTabBar.Maui;

internal static partial class NativeListInset
{
    // Supports MAUI 10's default CollectionViewHandler2, which never writes ContentInset. The legacy
    // CollectionViewHandler overwrites it on layout.
    public static partial void SetBottomInset(View list, double inset)
    {
        if (FindScrollView(list.Handler?.PlatformView as UIView) is not { } scrollView)
            return;

        var bottom = (nfloat)inset;

        var contentInset = scrollView.ContentInset;

        if (contentInset.Bottom != bottom)
        {
            contentInset.Bottom = bottom;
            scrollView.ContentInset = contentInset;
        }

        var indicatorInsets = scrollView.VerticalScrollIndicatorInsets;

        if (indicatorInsets.Bottom != bottom)
        {
            indicatorInsets.Bottom = bottom;
            scrollView.VerticalScrollIndicatorInsets = indicatorInsets;
        }
    }

    // With automatic adjustment UIKit adds the safe area on top of ContentInset. MAUI's CollectionView turns it
    // off (Never), so this is mainly for ListView.
    public static partial bool RespectsBottomSafeArea(View list)
    {
        return FindScrollView(list.Handler?.PlatformView as UIView) is { } scrollView
               && scrollView.ContentInsetAdjustmentBehavior != UIScrollViewContentInsetAdjustmentBehavior.Never;
    }

    private static UIScrollView? FindScrollView(UIView? view)
    {
        if (view is UIScrollView scrollView)
            return scrollView;

        if (view == null)
            return null;

        foreach (var subview in view.Subviews)
        {
            if (FindScrollView(subview) is { } found)
                return found;
        }

        return null;
    }
}
