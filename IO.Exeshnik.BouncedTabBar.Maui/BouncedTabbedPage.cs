using System.Collections.Specialized;
using System.ComponentModel;

namespace IO.Exeshnik.BouncedTabBar.Maui;

public class BouncedTabbedPage : TabbedPage
{
    public static readonly BindableProperty BubbleSizeProperty = BindableProperty.Create(
        nameof(BubbleSize),
        typeof(double),
        typeof(BouncedTabbedPage),
        44d,
        propertyChanged: OnBubbleSizePropertyChanged
    );

    public double BubbleSize
    {
        get => (double)GetValue(BubbleSizeProperty);
        set => SetValue(BubbleSizeProperty, value);
    }

    private static void OnBubbleSizePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        // Implicit styles are applied during base construction, before TabBar exists; its initializer copies the value.
        if (!(bindable is BouncedTabbedPage parent)
            || !(newValue is double newV)
            || parent.TabBar is null)
            return;

        parent.TabBar.BubbleSize = newV;
    }

    public static readonly BindableProperty IconPaddingProperty = BindableProperty.Create(
        nameof(IconPadding),
        typeof(double),
        typeof(BouncedTabbedPage),
        4d,
        propertyChanged: OnIconPaddingPropertyChanged
    );

    public double IconPadding
    {
        get => (double)GetValue(IconPaddingProperty);
        set => SetValue(IconPaddingProperty, value);
    }

    private static void OnIconPaddingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        // Implicit styles are applied during base construction, before TabBar exists; its initializer copies the value.
        if (!(bindable is BouncedTabbedPage parent)
            || !(newValue is double newV)
            || parent.TabBar is null)
            return;

        parent.TabBar.IconPadding = newV;
    }

    public static readonly BindableProperty BubbleColorProperty = BindableProperty.Create(
        nameof(BubbleColor),
        typeof(Color),
        typeof(BouncedTabbedPage),
        Colors.Red,
        propertyChanged: OnBubbleColorPropertyChanged
    );

    public Color BubbleColor
    {
        get => (Color)GetValue(BubbleColorProperty);
        set => SetValue(BubbleColorProperty, value);
    }

    private static void OnBubbleColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        // Implicit styles are applied during base construction, before TabBar exists; its initializer copies the value.
        if (!(bindable is BouncedTabbedPage parent)
            || !(newValue is Color newV)
            || parent.TabBar is null)
            return;

        parent.TabBar.BubbleColor = newV;
    }

    public static readonly BindableProperty BubbleMarginProperty = BindableProperty.Create(
        nameof(BubbleMargin),
        typeof(double),
        typeof(BouncedTabbedPage),
        8d,
        propertyChanged: OnBubbleMarginPropertyChanged
    );

    public double BubbleMargin
    {
        get => (double)GetValue(BubbleMarginProperty);
        set => SetValue(BubbleMarginProperty, value);
    }

    private static void OnBubbleMarginPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        // Implicit styles are applied during base construction, before TabBar exists; its initializer copies the value.
        if (!(bindable is BouncedTabbedPage parent)
            || !(newValue is double newV)
            || parent.TabBar is null)
            return;

        parent.TabBar.BubbleMargin = newV;
    }

    [Obsolete("Use TabbedPage.BarBackgroundColor or TabbedPage.BarBackground instead.")]
    public static readonly BindableProperty TabBackgroundColorProperty = BindableProperty.Create(
        nameof(TabBackgroundColor),
        typeof(Color),
        typeof(BouncedTabbedPage),
        Colors.White,
        propertyChanged: OnTabBackgroundColorPropertyChanged
    );

    [Obsolete("Use TabbedPage.BarBackgroundColor or TabbedPage.BarBackground instead.")]
    public Color TabBackgroundColor
    {
        get => (Color)GetValue(TabBackgroundColorProperty);
        set => SetValue(TabBackgroundColorProperty, value);
    }

    [Obsolete("Use TabbedPage.BarBackgroundColor or TabbedPage.BarBackground instead.")]
    private static void OnTabBackgroundColorPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (!(bindable is BouncedTabbedPage parent)
            || !(newValue is Color newV))
            return;

        if (parent.IsSet(TabBackgroundColorProperty))
            parent.BarBackgroundColor = newV;
        else
            parent.ClearValue(BarBackgroundColorProperty);
    }

    public BouncedTabBar TabBar { get; }

    private readonly HashSet<Page> _subscribedPages = new HashSet<Page>();
    private readonly TabBarInsetManager _insetManager = new TabBarInsetManager();

    private bool _isSyncingPages;

    public BouncedTabbedPage()
    {
        TabBar = new BouncedTabBar
        {
            BubbleSize = BubbleSize,
            IconPadding = IconPadding,
            BubbleColor = BubbleColor,
            BubbleMargin = BubbleMargin,
            TabBarBackground = GetTabBarBackground(),
            SelectedIconTintColor = GetSelectedIconTintColor(),
            UnselectedIconTintColor = GetUnselectedIconTintColor(),
        };
        TabBar.PropertyChanged += OnTabBarPropertyChanged;
        UpdateContentInset();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        // The base constructor raises property changes before TabBar is assigned.
        if (TabBar is null)
            return;

        if (propertyName == SelectedTabColorProperty.PropertyName)
            TabBar.SelectedIconTintColor = GetSelectedIconTintColor();
        else if (propertyName == UnselectedTabColorProperty.PropertyName)
            TabBar.UnselectedIconTintColor = GetUnselectedIconTintColor();
        else if (propertyName == BarBackgroundColorProperty.PropertyName || propertyName == BarBackgroundProperty.PropertyName)
            TabBar.TabBarBackground = GetTabBarBackground();
    }

    // Fallbacks instead of local values in the constructor, so styles can still set the tab colors.
    private Color GetSelectedIconTintColor()
    {
        return SelectedTabColor ?? Colors.White;
    }

    private Color GetUnselectedIconTintColor()
    {
        return UnselectedTabColor ?? Colors.Black;
    }

    // Same precedence as TabbedPage: BarBackground wins over BarBackgroundColor. The bar is drawn with a single
    // color, so only solid brushes are supported.
    private Color GetTabBarBackground()
    {
        return (BarBackground as SolidColorBrush)?.Color ?? BarBackgroundColor ?? Colors.White;
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        UpdateChildPageSubscriptions();

        if (Handler == null)
            return;

        // Icons may have changed while the page had no handler and was not listening.
        for (var i = 0; i < TabBar.TabItems.Count; i++)
            RefreshTabItem(i);
    }

    protected override void OnCurrentPageChanged()
    {
        base.OnCurrentPageChanged();

        SyncSelectedIndex();
    }

    private void OnTabBarPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == BouncedTabBar.TabBarHeightProperty.PropertyName || e.PropertyName == BouncedTabBar.PaddingProperty.PropertyName)
        {
            UpdateContentInset();
            return;
        }

        // While the tab items are being rebuilt the bar may clamp its index; CurrentPage is synced afterwards instead.
        if (_isSyncingPages || e.PropertyName != BouncedTabBar.SelectedIndexProperty.PropertyName)
            return;

        var index = TabBar.SelectedIndex;

        if (index < 0 || index >= TabBar.TabItems.Count)
            return;

        var page = TabBar.TabItems[index].Page;

        if (page != null)
            CurrentPage = page;
    }

    protected override void OnPagesChanged(NotifyCollectionChangedEventArgs e)
    {
        base.OnPagesChanged(e);

        _isSyncingPages = true;

        try
        {
            SyncTabItems();
        }
        finally
        {
            _isSyncingPages = false;
        }

        UpdateChildPageSubscriptions();
        SyncSelectedIndex();
    }

    // Rebuilds the tab items to match Children (same pages, same order). This covers every collection change,
    // including ItemsSource-generated pages, without depending on the shape of the change event.
    private void SyncTabItems()
    {
        var pages = Children.ToList();
        var tabItems = TabBar.TabItems;

        for (var i = tabItems.Count - 1; i >= 0; i--)
        {
            var page = tabItems[i].Page;

            if (page != null && pages.Contains(page))
                continue;

            tabItems.RemoveAt(i);
        }

        for (var i = 0; i < pages.Count; i++)
        {
            var page = pages[i];
            var currentIndex = IndexOfPage(page);

            if (currentIndex == -1)
                tabItems.Insert(i, CreateTabItem(page));
            else if (currentIndex != i)
                tabItems.Move(currentIndex, i);
        }
    }

    // Child pages are only listened to, and inset, while this page has a handler, so pages that outlive it
    // (e.g. singletons from DI) do not keep it alive and get their padding back. Tracked separately from TabItems,
    // which is public.
    private void UpdateChildPageSubscriptions()
    {
        var pages = Handler == null ? new HashSet<Page>() : Children.ToHashSet();

        foreach (var page in _subscribedPages.Where(page => !pages.Contains(page)).ToList())
        {
            page.PropertyChanged -= OnChildPagePropertyChanged;
            _insetManager.Detach(page);
            _subscribedPages.Remove(page);
        }

        foreach (var page in pages.Where(_subscribedPages.Add))
        {
            page.PropertyChanged += OnChildPagePropertyChanged;
            _insetManager.Attach(page);
        }
    }

    // The bar's bottom padding is the bottom safe area, which the handlers keep up to date.
    private void UpdateContentInset()
    {
        _insetManager.Update(TabBar.TabBarHeight, TabBar.Padding.Bottom);
    }

    private void SyncSelectedIndex()
    {
        var index = IndexOfPage(CurrentPage);

        if (index >= 0)
            TabBar.SelectedIndex = index;
    }

    private void OnChildPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!(sender is Page page)
            || (e.PropertyName != IconImageSourceProperty.PropertyName
                && e.PropertyName != BouncedPageExtensions.SelectedIconProperty.PropertyName
                && e.PropertyName != BouncedPageExtensions.UnselectedIconProperty.PropertyName))
            return;

        var index = IndexOfPage(page);

        if (index >= 0)
            RefreshTabItem(index);
    }

    private void RefreshTabItem(int index)
    {
        var current = TabBar.TabItems[index];

        if (current.Page == null)
            return;

        var updated = CreateTabItem(current.Page);

        if (updated.SelectedIcon != current.SelectedIcon || updated.UnselectedIcon != current.UnselectedIcon)
            TabBar.TabItems[index] = updated;
    }

    private int IndexOfPage(Page? page)
    {
        if (page == null)
            return -1;

        for (var i = 0; i < TabBar.TabItems.Count; i++)
        {
            if (TabBar.TabItems[i].Page == page)
                return i;
        }

        return -1;
    }

    private static TabItem CreateTabItem(Page page)
    {
        return new TabItem(page, page.GetSelectedIcon() ?? page.IconImageSource, page.GetUnselectedIcon());
    }
}
