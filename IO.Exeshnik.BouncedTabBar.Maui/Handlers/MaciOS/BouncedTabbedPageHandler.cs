using System.ComponentModel;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using UIKit;

namespace IO.Exeshnik.BouncedTabBar.Maui;

// The bar is a subview of the tab bar controller's view, pinned to its bottom and drawn over the tab content.
// The native UITabBar is hidden, so tab content extends to the bottom edge and is inset under the bar instead.
public class BouncedTabbedPageHandler : TabbedRenderer
{
    private BouncedTabbedPage? _page;
    private TabBarContainerView? _tabBarContainer;
    private NSLayoutConstraint? _tabBarHeightConstraint;
    private MoreNavigationDelegate? _moreNavigationDelegate;

    // TabbedRenderer never reports the previous element here, so the page the bar belongs to is tracked separately.
    protected override void OnElementChanged(VisualElementChangedEventArgs e)
    {
        base.OnElementChanged(e);

        if (_page != null && _page != e.NewElement)
            RemoveTabBar();

        if (e.NewElement is BouncedTabbedPage newPage)
            AddTabBar(newPage);
    }

    // Disconnecting the handler disposes the renderer without another OnElementChanged.
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            RemoveTabBar();

        base.Dispose(disposing);
    }

    public override void ViewDidLayoutSubviews()
    {
        base.ViewDidLayoutSubviews();

        // UIKit may insert views for tab transitions; keep the bar on top of them.
        if (_tabBarContainer != null)
            View?.BringSubviewToFront(_tabBarContainer);

        // The "More" delegate is not called in every case (e.g. on iPad), so the rule is also applied after each
        // layout pass, which follows every tab switch.
        if (_page is { } page && MoreNavigationController is { ViewControllers.Length: > 1, TopViewController: { } topViewController } moreNavigationController)
            MoreNavigationDelegate.ApplyChrome(moreNavigationController, topViewController, page, false);
    }

    public override void ViewSafeAreaInsetsDidChange()
    {
        base.ViewSafeAreaInsetsDidChange();

        UpdateSafeArea();
    }

    private void AddTabBar(BouncedTabbedPage page)
    {
        if (_page != null || View is not { } view || ((IElementHandler)this).MauiContext is not { } mauiContext)
            return;

        HideNativeTabBar();

        _moreNavigationDelegate = new MoreNavigationDelegate(page);
        MoreNavigationController.Delegate = _moreNavigationDelegate;

        var canvasView = page.TabBar.ToPlatform(mauiContext);
        canvasView.TranslatesAutoresizingMaskIntoConstraints = false;

        var container = new TabBarContainerView(page.TabBar)
        {
            TranslatesAutoresizingMaskIntoConstraints = false
        };
        container.AddSubview(canvasView);
        view.AddSubview(container);

        NSLayoutConstraint.ActivateConstraints(
            new[]
            {
                canvasView.LeadingAnchor.ConstraintEqualTo(container.LeadingAnchor),
                canvasView.TrailingAnchor.ConstraintEqualTo(container.TrailingAnchor),
                canvasView.TopAnchor.ConstraintEqualTo(container.TopAnchor),
                canvasView.BottomAnchor.ConstraintEqualTo(container.BottomAnchor),
                container.LeadingAnchor.ConstraintEqualTo(view.LeadingAnchor),
                container.TrailingAnchor.ConstraintEqualTo(view.TrailingAnchor),
                container.BottomAnchor.ConstraintEqualTo(view.BottomAnchor)
            }
        );

        _tabBarHeightConstraint = container.HeightAnchor.ConstraintEqualTo((nfloat)page.TabBar.CanvasHeight);
        _tabBarHeightConstraint.Active = true;
        _tabBarContainer = container;
        _page = page;

        page.TabBar.PropertyChanged += OnTabBarPropertyChanged;

        UpdateSafeArea();
    }

    private void RemoveTabBar()
    {
        if (_page is not { } page)
            return;

        _page = null;
        page.TabBar.PropertyChanged -= OnTabBarPropertyChanged;

        _tabBarContainer?.RemoveFromSuperview();
        _tabBarContainer = null;
        _tabBarHeightConstraint = null;

        if (MoreNavigationController.Delegate == _moreNavigationDelegate)
            MoreNavigationController.Delegate = null!;

        _moreNavigationDelegate = null;

        page.TabBar.Handler?.DisconnectHandler();
    }

    // iOS 18 added a proper way to hide the bar, which also covers the iPad tab bar at the top.
    private void HideNativeTabBar()
    {
        if (OperatingSystem.IsIOSVersionAtLeast(18) || OperatingSystem.IsMacCatalystVersionAtLeast(18))
            TabBarHidden = true;
        else
            TabBar.Hidden = true;
    }

    private void OnTabBarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != BouncedTabBar.TabBarHeightProperty.PropertyName
            || _tabBarHeightConstraint == null
            || sender is not BouncedTabBar tabBar)
            return;

        _tabBarHeightConstraint.Constant = (nfloat)tabBar.CanvasHeight;
    }

    // The bar extends behind the home indicator and, in landscape, the sensor housing; its padding keeps the tabs
    // inside the safe area.
    private void UpdateSafeArea()
    {
        if (_page is not { } page || View is not { } view)
            return;

        var insets = view.SafeAreaInsets;
        var padding = new Thickness(insets.Left, 0, insets.Right, insets.Bottom);

        if (page.TabBar.Padding != padding)
            page.TabBar.Padding = padding;
    }

    // On iPhone UIKit shows at most five tabs and moves later ones into its "More" navigation controller, even with
    // the tab bar hidden. The bar selects those tabs directly, so at a tab's root there must be no way back to
    // UIKit's "More" list. A plain page shows no navigation bar there; a NavigationPage tab, whose pages UIKit moves
    // into the "More" controller, keeps the bar for its titles but without the back button. Deeper pages navigate
    // as usual.
    private sealed class MoreNavigationDelegate : UINavigationControllerDelegate
    {
        // The "More" list itself plus the tab's root page.
        private const int TabRootDepth = 2;

        private readonly WeakReference<BouncedTabbedPage> _page;

        public MoreNavigationDelegate(BouncedTabbedPage page)
        {
            _page = new WeakReference<BouncedTabbedPage>(page);
        }

        public override void WillShowViewController(UINavigationController navigationController, UIViewController viewController, bool animated)
        {
            if (_page.TryGetTarget(out var page))
                ApplyChrome(navigationController, viewController, page, animated);
        }

        // Only changes what differs, so it is safe to call from layout passes.
        public static void ApplyChrome(UINavigationController navigationController, UIViewController viewController, BouncedTabbedPage page, bool animated)
        {
            var isTabRoot = navigationController.ViewControllers?.Length <= TabRootDepth;
            var hidesNavigationBar = isTabRoot && page.CurrentPage is not NavigationPage;

            if (navigationController.NavigationBarHidden != hidesNavigationBar)
                navigationController.SetNavigationBarHidden(hidesNavigationBar, animated);

            if (isTabRoot && !viewController.NavigationItem.HidesBackButton)
                viewController.NavigationItem.HidesBackButton = true;

            if (navigationController.InteractivePopGestureRecognizer is { } popGesture && popGesture.Enabled == isTabRoot)
                popGesture.Enabled = !isTabRoot;
        }
    }

    // The canvas is taller than the bar to leave room for the jumping bubble. Touches above the bar body go to the
    // tab content underneath.
    private sealed class TabBarContainerView : UIView
    {
        private readonly WeakReference<BouncedTabBar> _tabBar;

        public TabBarContainerView(BouncedTabBar tabBar)
        {
            _tabBar = new WeakReference<BouncedTabBar>(tabBar);
        }

        public override bool PointInside(CoreGraphics.CGPoint point, UIEvent? uievent)
        {
            return _tabBar.TryGetTarget(out var tabBar)
                   && point.Y >= Bounds.Height - tabBar.BarBodyHeight
                   && base.PointInside(point, uievent);
        }
    }
}
