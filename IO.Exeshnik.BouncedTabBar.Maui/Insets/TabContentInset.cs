using System.ComponentModel;

namespace IO.Exeshnik.BouncedTabBar.Maui;

// Inset for one tab. A NavigationPage tab has no padding of its own, so the inset follows its current page
// through pushes and pops.
internal sealed class TabContentInset
{
    private readonly Page _tabPage;

    private ContentPage? _contentPage;
    private InsetTarget? _target;

    private double _tabBarHeight;
    private double _safeAreaBottom;

    public TabContentInset(Page tabPage)
    {
        _tabPage = tabPage;
    }

    public void Attach(double tabBarHeight, double safeAreaBottom)
    {
        _tabBarHeight = tabBarHeight;
        _safeAreaBottom = safeAreaBottom;

        if (_tabPage is NavigationPage navigationPage)
        {
            navigationPage.PropertyChanged += OnNavigationPagePropertyChanged;
            SetContentPage(navigationPage.CurrentPage as ContentPage);
        }
        else
        {
            SetContentPage(_tabPage as ContentPage);
        }
    }

    public void Apply(double tabBarHeight, double safeAreaBottom)
    {
        _tabBarHeight = tabBarHeight;
        _safeAreaBottom = safeAreaBottom;

        _target?.Apply(_tabBarHeight, _safeAreaBottom);
    }

    public void Detach()
    {
        if (_tabPage is NavigationPage navigationPage)
            navigationPage.PropertyChanged -= OnNavigationPagePropertyChanged;

        SetContentPage(null);
    }

    private void OnNavigationPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == NavigationPage.CurrentPageProperty.PropertyName && sender is NavigationPage navigationPage)
            SetContentPage(navigationPage.CurrentPage as ContentPage);
    }

    private void SetContentPage(ContentPage? contentPage)
    {
        if (_contentPage == contentPage)
            return;

        if (_contentPage != null)
            _contentPage.PropertyChanged -= OnContentPagePropertyChanged;

        _contentPage = contentPage;

        if (_contentPage != null)
            _contentPage.PropertyChanged += OnContentPagePropertyChanged;

        Retarget();
    }

    private void OnContentPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == ContentPage.ContentProperty.PropertyName)
            Retarget();
    }

    private void Retarget()
    {
        _target?.Restore();
        _target = _contentPage == null ? null : InsetTarget.Create(_contentPage);
        _target?.Apply(_tabBarHeight, _safeAreaBottom);
    }
}
