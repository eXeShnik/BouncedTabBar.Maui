using System.ComponentModel;

namespace IO.Exeshnik.BouncedTabBar.Maui;

// CollectionView and ListView have no Padding, so the inset goes on their native scrolling view. That view only
// exists while the list has a handler, and a new handler brings a new view without our inset.
internal sealed class NativeListInsetTarget : InsetTarget
{
    private readonly View _list;

    private INotifyPropertyChanged? _itemsLayout;
    private double _tabBarHeight;
    private double _safeAreaBottom;
    private bool _isRestored;

    public NativeListInsetTarget(ContentPage page, View list)
        : base(page, list)
    {
        _list = list;
        _list.HandlerChanged += OnListHandlerChanged;
        _list.PropertyChanged += OnListPropertyChanged;

        SubscribeItemsLayout();
    }

    public override void Apply(double tabBarHeight, double safeAreaBottom)
    {
        _tabBarHeight = tabBarHeight;
        _safeAreaBottom = safeAreaBottom;

        UpdateNativeInset();
    }

    public override void Restore()
    {
        _list.HandlerChanged -= OnListHandlerChanged;
        _list.PropertyChanged -= OnListPropertyChanged;

        if (_itemsLayout != null)
            _itemsLayout.PropertyChanged -= OnItemsLayoutPropertyChanged;

        _isRestored = true;
        NativeListInset.SetBottomInset(_list, 0);
    }

    protected override bool RespectsBottomSafeArea()
    {
        return base.RespectsBottomSafeArea() || NativeListInset.RespectsBottomSafeArea(_list);
    }

    private void OnListHandlerChanged(object? sender, EventArgs e)
    {
        UpdateNativeInset();
    }

    private void OnListPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != StructuredItemsView.ItemsLayoutProperty.PropertyName)
            return;

        SubscribeItemsLayout();
        ReapplyAfterHandlerUpdate();
    }

    private void OnItemsLayoutPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ReapplyAfterHandlerUpdate();
    }

    // On Android MAUI resets the list padding when the items layout, its span or its spacing changes,
    // so reapply once the handler has processed the change.
    private void ReapplyAfterHandlerUpdate()
    {
        _list.Dispatcher.Dispatch(UpdateNativeInset);
    }

    private void SubscribeItemsLayout()
    {
        if (_itemsLayout != null)
            _itemsLayout.PropertyChanged -= OnItemsLayoutPropertyChanged;

        _itemsLayout = (_list as StructuredItemsView)?.ItemsLayout as INotifyPropertyChanged;

        if (_itemsLayout != null)
            _itemsLayout.PropertyChanged += OnItemsLayoutPropertyChanged;
    }

    private void UpdateNativeInset()
    {
        if (_isRestored)
            return;

        NativeListInset.SetBottomInset(_list, GetInset(_tabBarHeight, _safeAreaBottom));
    }
}
