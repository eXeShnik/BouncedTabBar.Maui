using System.ComponentModel;

namespace IO.Exeshnik.BouncedTabBar.Maui;

// Adds the inset to the bottom padding of a page or a ScrollView, on top of the padding it had before.
internal sealed class PaddingInsetTarget : InsetTarget
{
    private readonly BindableProperty _paddingProperty;

    private Thickness _originalPadding;
    private Thickness? _appliedPadding;
    private double _tabBarHeight;
    private double _safeAreaBottom;

    public PaddingInsetTarget(ContentPage page, Element element, BindableProperty paddingProperty)
        : base(page, element)
    {
        _paddingProperty = paddingProperty;
        _originalPadding = (Thickness)element.GetValue(paddingProperty);

        Element.PropertyChanged += OnElementPropertyChanged;
    }

    public override void Apply(double tabBarHeight, double safeAreaBottom)
    {
        _tabBarHeight = tabBarHeight;
        _safeAreaBottom = safeAreaBottom;

        ApplyPadding();
    }

    public override void Restore()
    {
        Element.PropertyChanged -= OnElementPropertyChanged;

        // Clearing brings back a style or default value; only a different (local) original is written back.
        Element.ClearValue(_paddingProperty);

        if ((Thickness)Element.GetValue(_paddingProperty) != _originalPadding)
            Element.SetValue(_paddingProperty, _originalPadding);
    }

    private void ApplyPadding()
    {
        var inset = GetInset(_tabBarHeight, _safeAreaBottom);
        var padding = new Thickness(_originalPadding.Left, _originalPadding.Top, _originalPadding.Right, _originalPadding.Bottom + inset);

        _appliedPadding = padding;
        Element.SetValue(_paddingProperty, padding);
    }

    // A padding set elsewhere while the inset is applied (code, a binding update) replaces it, so it becomes
    // the new original and the inset goes back on top.
    private void OnElementPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != _paddingProperty.PropertyName)
            return;

        var padding = (Thickness)Element.GetValue(_paddingProperty);

        if (padding == _appliedPadding)
            return;

        _originalPadding = padding;
        ApplyPadding();
    }
}
