namespace IO.Exeshnik.BouncedTabBar.Maui;

// The element of a tab page that receives the bottom inset.
internal abstract class InsetTarget
{
    private readonly ContentPage _page;

    protected InsetTarget(ContentPage page, Element element)
    {
        _page = page;
        Element = element;
    }

    protected Element Element { get; }

    public static InsetTarget Create(ContentPage page)
    {
        return FindTrailingScrollable(page.Content) switch
        {
            ScrollView scrollView => new PaddingInsetTarget(page, scrollView, ScrollView.PaddingProperty),
            { } list => new NativeListInsetTarget(page, list),
            null => new PaddingInsetTarget(page, page, Page.PaddingProperty)
        };
    }

    public abstract void Apply(double tabBarHeight, double safeAreaBottom);

    public abstract void Restore();

    // The tab bar height includes the bottom safe area. When the target or one of its ancestors already keeps clear
    // of the safe area, only the part of the bar above it is left to inset. Descendants don't count: once the target
    // is padded, they no longer reach the bottom edge, so they stop applying the safe area.
    protected double GetInset(double tabBarHeight, double safeAreaBottom)
    {
        return Math.Max(0, tabBarHeight - (RespectsBottomSafeArea() ? safeAreaBottom : 0));
    }

    protected virtual bool RespectsBottomSafeArea()
    {
        for (var element = Element; element != null; element = element.Parent)
        {
            if (KeepsClearOfSafeArea(GetBottomSafeAreaRegions(element)))
                return true;

            if (element == _page)
                break;
        }

        return false;
    }

    // MAUI's own resolution (ISafeAreaView2) is internal, so this mirrors its per-control defaults in MAUI 10.
    private static SafeAreaRegions GetBottomSafeAreaRegions(Element element)
    {
        return element switch
        {
            ContentPage page when page.IsSet(ContentPage.SafeAreaEdgesProperty) => page.SafeAreaEdges.Bottom,
#if IOS || MACCATALYST
            ContentPage page => ((ISafeAreaView)page).IgnoreSafeArea ? SafeAreaRegions.None : SafeAreaRegions.Container,
#else
            ContentPage => SafeAreaRegions.None,
#endif
            Layout layout => ((ISafeAreaView)layout).IgnoreSafeArea ? SafeAreaRegions.None : DefaultTo(layout.SafeAreaEdges.Bottom, SafeAreaRegions.Container),
            ScrollView scrollView => scrollView.SafeAreaEdges.Bottom,
            ContentView contentView => DefaultTo(contentView.SafeAreaEdges.Bottom, SafeAreaRegions.None),
            Border border => DefaultTo(border.SafeAreaEdges.Bottom, SafeAreaRegions.None),
            _ => SafeAreaRegions.None
        };
    }

    private static SafeAreaRegions DefaultTo(SafeAreaRegions regions, SafeAreaRegions fallback)
    {
        return regions == SafeAreaRegions.Default ? fallback : regions;
    }

    // Matches how MAUI applies regions on Android and iOS: everything but None and keyboard-only. A ScrollView's
    // Default counts too: it adds the safe area to its content inset.
    private static bool KeepsClearOfSafeArea(SafeAreaRegions regions)
    {
        return regions != SafeAreaRegions.None && regions != SafeAreaRegions.SoftInput;
    }

    // The page content itself, or the bottom-most child of a content layout.
    private static View? FindTrailingScrollable(View? content)
    {
        return AsScrollable(content) ?? (content is Layout layout ? AsScrollable(GetBottomChild(layout) as View) : null);
    }

    private static View? AsScrollable(View? view)
    {
#pragma warning disable CS0618 // ListView is obsolete in MAUI 10, but still supported.
        return view switch
        {
            ScrollView { Orientation: not ScrollOrientation.Horizontal } => view,
            CollectionView { ItemsLayout: not ItemsLayout { Orientation: ItemsLayoutOrientation.Horizontal } } => view,
            ListView => view,
            RefreshView refreshView => AsScrollable(refreshView.Content),
            _ => null
        };
#pragma warning restore CS0618
    }

    // Hidden children are skipped. In a Grid the last child in markup can sit in any row, so take the child reaching
    // the last row, preferring a scrollable one over overlays in the same cell (loading indicators, floating buttons).
    private static IView? GetBottomChild(Layout layout)
    {
        var children = layout.Children.Where(child => child.Visibility == Visibility.Visible);

        if (layout is not Grid grid)
            return children.LastOrDefault();

        IView? bottomChild = null;
        var bottomEdge = -1;

        foreach (var child in children)
        {
            var edge = grid.GetRow(child) + grid.GetRowSpan(child);

            if (edge < bottomEdge
                || (edge == bottomEdge && AsScrollable(child as View) == null && AsScrollable(bottomChild as View) != null))
                continue;

            bottomChild = child;
            bottomEdge = edge;
        }

        return bottomChild;
    }
}
