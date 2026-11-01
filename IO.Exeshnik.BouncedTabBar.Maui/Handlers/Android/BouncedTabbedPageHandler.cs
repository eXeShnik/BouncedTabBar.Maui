using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Android.Views;
using AndroidX.CoordinatorLayout.Widget;
using AndroidX.Core.View;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace IO.Exeshnik.BouncedTabBar.Maui;

// MAUI adds its native tabs to the window's navigation layout while the TabbedPage is shown and removes them when
// it goes away. The bar follows the page's own view: it is added at the bottom of that layout, above the content,
// while the page's ViewPager is attached to the window (shown), and removed when it is detached (covered by a
// pushed page, popped or closed).
public class BouncedTabbedPageHandler : TabbedViewHandler
{
    private AView? _tabBarView;
    private CoordinatorLayout? _navigationLayout;

    private BouncedTabbedPage? BouncedTabbedPage => VirtualView as BouncedTabbedPage;

    protected override void ConnectHandler(AView platformView)
    {
        base.ConnectHandler(platformView);

        if (BouncedTabbedPage is not { } page)
            return;

        platformView.ViewAttachedToWindow += OnPlatformViewAttachedToWindow;
        platformView.ViewDetachedFromWindow += OnPlatformViewDetachedFromWindow;
        page.Appearing += OnPageAppearing;
        page.TabBar.PropertyChanged += OnTabBarPropertyChanged;

        if (platformView.IsAttachedToWindow)
            AttachTabBar();
    }

    protected override void DisconnectHandler(AView platformView)
    {
        if (BouncedTabbedPage is { } page)
        {
            page.Appearing -= OnPageAppearing;
            page.TabBar.PropertyChanged -= OnTabBarPropertyChanged;

            DetachTabBar();
            page.TabBar.Handler?.DisconnectHandler();
        }

        platformView.ViewAttachedToWindow -= OnPlatformViewAttachedToWindow;
        platformView.ViewDetachedFromWindow -= OnPlatformViewDetachedFromWindow;

        base.DisconnectHandler(platformView);
    }

    private void OnPlatformViewAttachedToWindow(object? sender, AView.ViewAttachedToWindowEventArgs e)
    {
        AttachTabBar();
    }

    private void OnPlatformViewDetachedFromWindow(object? sender, AView.ViewDetachedFromWindowEventArgs e)
    {
        DetachTabBar();
    }

    // MAUI sets its native tabs up again on each Appearing, including the content margin for bottom tabs.
    private void OnPageAppearing(object? sender, EventArgs e)
    {
        if (_tabBarView != null && _navigationLayout is { } navigationLayout)
            navigationLayout.Post(() => HideNativeTabs(navigationLayout));
    }

    private void OnTabBarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == BouncedTabBar.TabBarHeightProperty.PropertyName)
            UpdateTabBarViewHeight();
    }

    private void AttachTabBar()
    {
        if (BouncedTabbedPage is not { } page
            || MauiContext is not { } mauiContext
            || GetNavigationLayout(mauiContext) is not { } navigationLayout)
            return;

        var tabBarView = page.TabBar.ToPlatform(mauiContext);

        if (tabBarView.Parent != navigationLayout)
        {
            (tabBarView.Parent as ViewGroup)?.RemoveView(tabBarView);
            navigationLayout.AddView(
                tabBarView,
                new CoordinatorLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, GetTabBarViewHeight(page, tabBarView))
                {
                    Gravity = (int)GravityFlags.Bottom
                }
            );
        }

        if (_tabBarView != tabBarView)
        {
            _tabBarView = tabBarView;
            _tabBarView.ViewAttachedToWindow += OnTabBarViewAttachedToWindow;
            _tabBarView.LayoutChange += OnTabBarViewLayoutChange;
        }

        _navigationLayout = navigationLayout;
        UpdateSafeArea();

        // MAUI sets up its native tabs when the page appears, partly asynchronously; hide them once that has run.
        navigationLayout.Post(() => HideNativeTabs(navigationLayout));
    }

    private void DetachTabBar()
    {
        if (_tabBarView == null)
            return;

        _tabBarView.ViewAttachedToWindow -= OnTabBarViewAttachedToWindow;
        _tabBarView.LayoutChange -= OnTabBarViewLayoutChange;
        (_tabBarView.Parent as ViewGroup)?.RemoveView(_tabBarView);
        _tabBarView = null;
        _navigationLayout = null;
    }

    // The CoordinatorLayout that hosts MAUI's app bar, tabs and page content. NavigationRootManager.RootView is that
    // layout, or a drawer container around it when a FlyoutPage is involved.
    private static CoordinatorLayout? GetNavigationLayout(IMauiContext mauiContext)
    {
        var rootView = mauiContext.Services.GetService<NavigationRootManager>()?.RootView;

        return rootView?.FindViewById(Resource.Id.navigationlayout_content)?.Parent as CoordinatorLayout;
    }

    // Keeps the reflected MAUI members when the app is trimmed.
    [DynamicDependency(MauiInternals.TabbedPageManagerField, typeof(TabbedPage))]
    [DynamicDependency(MauiInternals.TabLayoutProperty, "Microsoft.Maui.Controls.Handlers.TabbedPageManager", "Microsoft.Maui.Controls")]
    [DynamicDependency(MauiInternals.BottomNavigationViewProperty, "Microsoft.Maui.Controls.Handlers.TabbedPageManager", "Microsoft.Maui.Controls")]
    private void HideNativeTabs(CoordinatorLayout navigationLayout)
    {
        if (BouncedTabbedPage is not { } page
            || MauiInternals.GetMemberValue(page, MauiInternals.TabbedPageManagerField) is not { } tabbedPageManager)
            return;

        foreach (var tabsPropertyName in new[] { MauiInternals.TabLayoutProperty, MauiInternals.BottomNavigationViewProperty })
        {
            if (MauiInternals.GetMemberValue(tabbedPageManager, tabsPropertyName) is AView tabsView)
                tabsView.Visibility = ViewStates.Gone;
        }

        // MAUI reserves the bottom navigation height for bottom tabs; tab content is inset under the bar instead.
        if (navigationLayout.FindViewById(Resource.Id.navigationlayout_content)?.LayoutParameters is ViewGroup.MarginLayoutParams { BottomMargin: not 0 } contentLayoutParams)
        {
            contentLayoutParams.BottomMargin = 0;
            navigationLayout.RequestLayout();
        }
    }

    private void OnTabBarViewAttachedToWindow(object? sender, AView.ViewAttachedToWindowEventArgs e)
    {
        UpdateSafeArea();
    }

    private void OnTabBarViewLayoutChange(object? sender, AView.LayoutChangeEventArgs e)
    {
        UpdateSafeArea();
    }

    // The navigation layout is drawn edge to edge, so the bar extends behind the system bars and display cutouts and
    // keeps its tabs clear of them through its padding (bottom, and the sides in landscape).
    private void UpdateSafeArea()
    {
        if (BouncedTabbedPage is not { } page
            || _tabBarView?.Context is not { } context
            || ViewCompat.GetRootWindowInsets(_tabBarView)?.GetInsets(WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout()) is not { } insets)
            return;

        var padding = new Thickness(context.FromPixels(insets.Left), 0, context.FromPixels(insets.Right), context.FromPixels(insets.Bottom));

        if (page.TabBar.Padding != padding)
            page.TabBar.Padding = padding;
    }

    private void UpdateTabBarViewHeight()
    {
        if (BouncedTabbedPage is not { } page || _tabBarView?.LayoutParameters is not { } layoutParameters)
            return;

        var height = GetTabBarViewHeight(page, _tabBarView);

        if (layoutParameters.Height == height)
            return;

        layoutParameters.Height = height;
        _tabBarView.LayoutParameters = layoutParameters;
    }

    private static int GetTabBarViewHeight(BouncedTabbedPage page, AView tabBarView)
    {
        return tabBarView.Context is { } context ? (int)Math.Ceiling(context.ToPixels(page.TabBar.CanvasHeight)) : 0;
    }
}
