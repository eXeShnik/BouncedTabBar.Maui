using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using WNavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using WPoint = Windows.Foundation.Point;

namespace IO.Exeshnik.BouncedTabBar.Maui;

// MAUI hosts tabbed content either in its own NavigationView or in the window's root one, with the tabs as that
// NavigationView's top menu. The menu is hidden while the page is loaded, and the bar is shown in a popup placed
// over the bottom of the page's own bounds, which works the same way for both layouts (and inside a FlyoutPage).
public class BouncedTabbedPageHandler : TabbedViewHandler
{
    // The window's root NavigationView is shared by pages; only the handler that hid its menu gives it back.
    private static readonly ConditionalWeakTable<WNavigationView, BouncedTabbedPageHandler> MenuOwners = new();

    private Popup? _popup;
    private FrameworkElement? _hostView;
    private WNavigationView? _navigationView;

    private BouncedTabbedPage? BouncedTabbedPage => VirtualView as BouncedTabbedPage;

    protected override void ConnectHandler(FrameworkElement platformView)
    {
        base.ConnectHandler(platformView);

        platformView.Loaded += OnPlatformViewLoaded;
        platformView.Unloaded += OnPlatformViewUnloaded;

        if (BouncedTabbedPage is { } page)
            page.TabBar.PropertyChanged += OnTabBarPropertyChanged;

        if (platformView.IsLoaded)
            AttachTabBar(platformView);
    }

    protected override void DisconnectHandler(FrameworkElement platformView)
    {
        platformView.Loaded -= OnPlatformViewLoaded;
        platformView.Unloaded -= OnPlatformViewUnloaded;

        DetachTabBar();

        if (BouncedTabbedPage is { } page)
        {
            page.TabBar.PropertyChanged -= OnTabBarPropertyChanged;
            page.TabBar.Handler?.DisconnectHandler();
        }

        base.DisconnectHandler(platformView);
    }

    private void OnPlatformViewLoaded(object sender, RoutedEventArgs e)
    {
        AttachTabBar((FrameworkElement)sender);
    }

    // WinUI can raise Unloaded after a newer Loaded (re-parenting, frame navigation); only a view that is really
    // gone detaches the bar.
    private void OnPlatformViewUnloaded(object sender, RoutedEventArgs e)
    {
        if (!((FrameworkElement)sender).IsLoaded)
            DetachTabBar();
    }

    private void OnTabBarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == BouncedTabBar.TabBarHeightProperty.PropertyName)
            UpdatePopupLayout();
    }

    private void AttachTabBar(FrameworkElement platformView)
    {
        if (_popup != null
            || BouncedTabbedPage is not { } page
            || MauiContext is not { } mauiContext
            || platformView.XamlRoot is not { } xamlRoot)
            return;

        HideNavigationViewMenu(page);

        // MAUI connects to the window's root NavigationView while the page loads; hide its menu once that has run.
        platformView.DispatcherQueue.TryEnqueue(() =>
        {
            if (_popup != null)
                HideNavigationViewMenu(page);
        });

        _popup = new Popup
        {
            Child = page.TabBar.ToPlatform(mauiContext),
            XamlRoot = xamlRoot,
            IsLightDismissEnabled = false
        };

        _hostView = platformView;
        _hostView.SizeChanged += OnHostViewSizeChanged;
        xamlRoot.Changed += OnXamlRootChanged;

        UpdatePopupLayout();
        _popup.IsOpen = true;
    }

    private void DetachTabBar()
    {
        if (_popup == null)
            return;

        if (_popup.XamlRoot is { } xamlRoot)
            xamlRoot.Changed -= OnXamlRootChanged;

        _popup.IsOpen = false;
        _popup.Child = null;
        _popup = null;

        if (_hostView != null)
            _hostView.SizeChanged -= OnHostViewSizeChanged;

        _hostView = null;

        if (_navigationView != null && MenuOwners.TryGetValue(_navigationView, out var owner) && owner == this)
        {
            MenuOwners.Remove(_navigationView);
            _navigationView.IsPaneVisible = true;
        }

        _navigationView = null;
    }

    // Keeps the reflected MAUI member when the app is trimmed.
    [DynamicDependency(MauiInternals.NavigationViewField, typeof(TabbedPage))]
    private void HideNavigationViewMenu(BouncedTabbedPage page)
    {
        _navigationView = MauiInternals.GetMemberValue(page, MauiInternals.NavigationViewField) as WNavigationView;

        if (_navigationView == null)
            return;

        MenuOwners.AddOrUpdate(_navigationView, this);
        _navigationView.IsPaneVisible = false;
    }

    private void OnHostViewSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdatePopupLayout();
    }

    private void OnXamlRootChanged(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        UpdatePopupLayout();
    }

    // A popup without a parent is positioned relative to the XamlRoot, so place it over the bottom of the page's
    // bounds in XamlRoot coordinates.
    private void UpdatePopupLayout()
    {
        if (_popup?.Child is not FrameworkElement tabBarView || _hostView == null || BouncedTabbedPage is not { } page)
            return;

        var height = page.TabBar.CanvasHeight;
        var hostOrigin = _hostView.TransformToVisual(null).TransformPoint(new WPoint(0, 0));

        tabBarView.Width = _hostView.ActualWidth;
        tabBarView.Height = height;

        _popup.HorizontalOffset = hostOrigin.X;
        _popup.VerticalOffset = hostOrigin.Y + Math.Max(0, _hostView.ActualHeight - height);
    }
}
