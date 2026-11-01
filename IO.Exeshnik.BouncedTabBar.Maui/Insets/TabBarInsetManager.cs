namespace IO.Exeshnik.BouncedTabBar.Maui;

// Keeps tab content clear of the tab bar. The bar's curve shows the content underneath it, so pages keep their
// full height and get bottom padding instead: on a trailing scroll view, so its content scrolls under the bar,
// or otherwise on the page itself.
internal sealed class TabBarInsetManager
{
    private readonly Dictionary<Page, TabContentInset> _tabs = new Dictionary<Page, TabContentInset>();

    private double _tabBarHeight;
    private double _safeAreaBottom;

    public void Update(double tabBarHeight, double safeAreaBottom)
    {
        _tabBarHeight = tabBarHeight;
        _safeAreaBottom = safeAreaBottom;

        foreach (var tab in _tabs.Values)
            tab.Apply(_tabBarHeight, _safeAreaBottom);
    }

    public void Attach(Page tabPage)
    {
        if (_tabs.ContainsKey(tabPage))
            return;

        var tab = new TabContentInset(tabPage);
        _tabs[tabPage] = tab;
        tab.Attach(_tabBarHeight, _safeAreaBottom);
    }

    public void Detach(Page tabPage)
    {
        if (_tabs.Remove(tabPage, out var tab))
            tab.Detach();
    }
}
