using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;
using AViewGroup = Android.Views.ViewGroup;

namespace IO.Exeshnik.BouncedTabBar.Maui;

internal static partial class NativeListInset
{
    public static partial void SetBottomInset(View list, double inset)
    {
        if (FindScrollingView(list.Handler?.PlatformView as AView) is not { } scrollingView || scrollingView.Context is not { } context)
            return;

        var bottomPadding = (int)Math.Round(context.ToPixels(inset));

        if (scrollingView.PaddingBottom != bottomPadding)
            scrollingView.SetPadding(scrollingView.PaddingLeft, scrollingView.PaddingTop, scrollingView.PaddingRight, bottomPadding);

        // Let items draw into the padding, so they scroll under the bar instead of stopping above it.
        scrollingView.SetClipToPadding(inset <= 0);
    }

    public static partial bool RespectsBottomSafeArea(View list)
    {
        return false;
    }

    private static AViewGroup? FindScrollingView(AView? view)
    {
        if (view is RecyclerView or AbsListView)
            return (AViewGroup)view;

        if (view is not AViewGroup viewGroup)
            return null;

        for (var i = 0; i < viewGroup.ChildCount; i++)
        {
            if (FindScrollingView(viewGroup.GetChildAt(i)) is { } scrollingView)
                return scrollingView;
        }

        return null;
    }
}
