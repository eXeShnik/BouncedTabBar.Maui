using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace IO.Exeshnik.BouncedTabBar.Maui;

[ContentProperty(nameof(TabItems))]
public class BouncedTabBar : SKCanvasView
{
    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
        nameof(SelectedIndex),
        typeof(int),
        typeof(BouncedTabBar),
        0,
        BindingMode.TwoWay,
        propertyChanged: OnSelectedIndexPropertyChanged,
        coerceValue: CoerceSelectedIndex
    );

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    private static void OnSelectedIndexPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (!(bindable is BouncedTabBar parent))
            return;

        parent.AnimateToSelectedIndex();
    }

    // Coercion runs before change notifications, so listeners never see an out-of-range index. An invalid value
    // keeps the current one (clamped, as it may predate the current items). Without tabs nothing can be validated,
    // so the value is kept and applied once tabs arrive.
    private static object CoerceSelectedIndex(BindableObject bindable, object value)
    {
        if (!(bindable is BouncedTabBar parent) || !(value is int index) || parent.TabItems.Count == 0)
            return value;

        var maxIndex = parent.TabItems.Count - 1;

        return index >= 0 && index <= maxIndex ? index : Math.Clamp(parent.SelectedIndex, 0, maxIndex);
    }

    public static readonly BindableProperty TabItemsProperty = BindableProperty.Create(
        nameof(TabItems),
        typeof(ObservableCollection<TabItem>),
        typeof(BouncedTabBar),
        propertyChanged: OnTabItemsPropertyChanged,
        defaultValueCreator: _ => new ObservableCollection<TabItem>()
    );

    public ObservableCollection<TabItem> TabItems
    {
        get => (ObservableCollection<TabItem>)GetValue(TabItemsProperty);
        set => SetValue(TabItemsProperty, value);
    }

    private static void OnTabItemsPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (!(bindable is BouncedTabBar parent))
            return;

        if (oldValue is ObservableCollection<TabItem> oldV)
            parent.UpdateItemsSubscription(oldV, false);

        if (newValue is ObservableCollection<TabItem> newV)
            parent.UpdateItemsSubscription(newV);

        parent.OnItemsChanged();
    }

    public static readonly BindableProperty BubbleColorProperty = BindableProperty.Create(
        nameof(BubbleColor),
        typeof(Color),
        typeof(BouncedTabBar),
        Colors.Red,
        propertyChanged: OnCanvasDependentPropertyChanged
    );

    public Color BubbleColor
    {
        get => (Color)GetValue(BubbleColorProperty);
        set => SetValue(BubbleColorProperty, value);
    }

    public static readonly BindableProperty SelectedIconTintColorProperty = BindableProperty.Create(
        nameof(SelectedIconTintColor),
        typeof(Color),
        typeof(BouncedTabBar),
        propertyChanged: OnCanvasDependentPropertyChanged
    );

    public Color? SelectedIconTintColor
    {
        get => (Color?)GetValue(SelectedIconTintColorProperty);
        set => SetValue(SelectedIconTintColorProperty, value);
    }

    public static readonly BindableProperty UnselectedIconTintColorProperty = BindableProperty.Create(
        nameof(UnselectedIconTintColor),
        typeof(Color),
        typeof(BouncedTabBar),
        propertyChanged: OnCanvasDependentPropertyChanged
    );

    public Color? UnselectedIconTintColor
    {
        get => (Color?)GetValue(UnselectedIconTintColorProperty);
        set => SetValue(UnselectedIconTintColorProperty, value);
    }

    public static readonly BindableProperty BubbleSizeProperty = BindableProperty.Create(
        nameof(BubbleSize),
        typeof(double),
        typeof(BouncedTabBar),
        40d,
        propertyChanged: OnBubbleSizePropertyChanged
    );

    public double BubbleSize
    {
        get => (double)GetValue(BubbleSizeProperty);
        set => SetValue(BubbleSizeProperty, value);
    }

    private static void OnBubbleSizePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (!(bindable is BouncedTabBar parent))
            return;

        parent.UpdateTabBarHeight();
        parent.ReloadIcons();
        parent.InvalidateSurface();
    }

    public static readonly BindableProperty IconPaddingProperty = BindableProperty.Create(
        nameof(IconPadding),
        typeof(double),
        typeof(BouncedTabBar),
        8d,
        propertyChanged: OnIconPaddingPropertyChanged
    );

    public double IconPadding
    {
        get => (double)GetValue(IconPaddingProperty);
        set => SetValue(IconPaddingProperty, value);
    }

    private static void OnIconPaddingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (!(bindable is BouncedTabBar parent))
            return;

        parent.ReloadIcons();
        parent.InvalidateSurface();
    }

    public static readonly BindableProperty BubbleMarginProperty = BindableProperty.Create(
        nameof(BubbleMargin),
        typeof(double),
        typeof(BouncedTabBar),
        8d,
        propertyChanged: OnCanvasDependentPropertyChanged
    );

    public double BubbleMargin
    {
        get => (double)GetValue(BubbleMarginProperty);
        set => SetValue(BubbleMarginProperty, value);
    }

    public static readonly BindableProperty TabBarBackgroundProperty = BindableProperty.Create(
        nameof(TabBarBackground),
        typeof(Color),
        typeof(BouncedTabBar),
        Colors.White,
        propertyChanged: OnCanvasDependentPropertyChanged
    );

    public Color TabBarBackground
    {
        get => (Color)GetValue(TabBarBackgroundProperty);
        set => SetValue(TabBarBackgroundProperty, value);
    }

    private static void OnCanvasDependentPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (!(bindable is BouncedTabBar parent))
            return;

        parent.InvalidateSurface();
    }

    public static readonly BindableProperty PaddingProperty = BindableProperty.Create(
        nameof(Padding),
        typeof(Thickness),
        typeof(BouncedTabBar),
        Thickness.Zero,
        propertyChanged: OnPaddingPropertyChanged
    );

    public Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    private static void OnPaddingPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (!(bindable is BouncedTabBar parent))
            return;

        parent.UpdateTabBarHeight();
        parent.InvalidateSurface();
    }

    public static readonly BindableProperty SelectionStateProperty = BindableProperty.Create(
        nameof(SelectionState),
        typeof(double),
        typeof(BouncedTabBar),
        1d,
        BindingMode.TwoWay,
        propertyChanged: OnSelectionStatePropertyChanged
    );

    public double SelectionState
    {
        get => (double)GetValue(SelectionStateProperty);
        set => SetValue(SelectionStateProperty, value);
    }

    private static void OnSelectionStatePropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (!(bindable is BouncedTabBar parent) || parent._isAnimationOnProcessing)
            return;

        parent.InvalidateSurface();
    }

    private static readonly BindablePropertyKey TabBarHeightPropertyKey = BindableProperty.CreateReadOnly(
        nameof(TabBarHeight),
        typeof(double),
        typeof(BouncedTabBar),
        -1d
    );

    public static readonly BindableProperty TabBarHeightProperty = TabBarHeightPropertyKey.BindableProperty;

    public double TabBarHeight
    {
        get => (double)GetValue(TabBarHeightProperty);
        private set => SetValue(TabBarHeightPropertyKey, value);
    }

    private Dictionary<ImageSource, SKImage> _icons = new Dictionary<ImageSource, SKImage>();
    private CancellationTokenSource? _iconsLoadingCts;

    private double _bounceState;
    private double _curveBounceState;
    private double _clickedIconColorOpacityState = 1;

    private int _previousSelectedIndex;
    private int _displayedIndex;

    private int _touchedTabIndex = -1;
    private DateTime _touchedAt;

    private bool _isAnimationOnProcessing;

    private float _initialBarY;
    private float _pixelScale = 1;
    private float _canvasWidth;

    private const uint SwitchTabAnimationLength = 400u;
    private const uint BounceAnimationLength = SwitchTabAnimationLength / 5;
    private const double TapMaxDurationMs = 300;

    // The bubble's drop shadow (sigma 7) reaches about three sigmas beyond the circle.
    private const double BubbleShadowExtent = 24;

    private const string TouchFeedbackAnimation = nameof(TouchFeedbackAnimation);
    private const string JumpAnimation = nameof(JumpAnimation);
    private const string JumpInertiaAnimation = nameof(JumpInertiaAnimation);
    private const string CurveInertiaAnimation = nameof(CurveInertiaAnimation);

    private static readonly SKSamplingOptions IconSampling = new SKSamplingOptions(SKFilterMode.Linear);

    public BouncedTabBar()
    {
        EnableTouchEvents = true;
        UpdateTabBarHeight();

        // The default collection comes from defaultValueCreator, which does not raise propertyChanged.
        UpdateItemsSubscription(TabItems);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler == null)
            ClearIcons();
        else
            ReloadIcons();
    }

    // The bar body plus the upper half of the resting bubble, which sticks out above the bar.
    private void UpdateTabBarHeight()
    {
        TabBarHeight = BubbleSize + BubbleSize / 2 + Padding.Bottom;
    }

    // Height of the bar body: from the bar top to the bottom edge, including the bottom safe area.
    internal double BarBodyHeight => BubbleSize + Padding.Bottom;

    // Height the hosting view needs: the bar body plus room above it for the jumping bubble, whose centre rises one
    // bubble size above the bar top, its radius and its drop shadow.
    internal double CanvasHeight => BarBodyHeight + BubbleSize * 1.5 + BubbleShadowExtent;

    private double GetIconSize()
    {
        return BubbleSize - 2 * IconPadding;
    }

    protected override void OnTouch(SKTouchEventArgs e)
    {
        var point = new SKPoint(e.Location.X / _pixelScale, e.Location.Y / _pixelScale);

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                if (!HasTabs() || point.Y < _initialBarY || _touchedTabIndex > -1)
                    return;

                _touchedTabIndex = GetTabIndexAt(point.X);

                if (_touchedTabIndex == -1)
                    return;

                e.Handled = true;
                _touchedAt = DateTime.Now;
                AnimateTouching();
                break;
            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                if (_touchedTabIndex == -1)
                    return;

                e.Handled = true;

                var duration = (DateTime.Now - _touchedAt).TotalMilliseconds;

                if (e.ActionType == SKTouchAction.Released && duration <= TapMaxDurationMs)
                    SelectedIndex = _touchedTabIndex;

                _touchedTabIndex = -1;

                AnimateTouching(false);
                break;
            default:
                if (_touchedTabIndex > -1)
                    e.Handled = true;
                break;
        }
    }

    private int GetTabIndexAt(float x)
    {
        var itemWidth = GetItemWidth(_canvasWidth);

        if (itemWidth <= 0)
            return -1;

        var index = (int)Math.Floor((x - Padding.Left) / itemWidth);

        return index >= 0 && index < TabItems.Count ? index : -1;
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (!HasTabs())
            return;

        // The handlers host the bar directly in the platform view tree, so MAUI never lays it out and
        // Width/Height stay unset. Draw in device-independent units at full pixel density instead.
        _pixelScale = Convert.ToSingle(Math.Max(DeviceDisplay.MainDisplayInfo.Density, 1));
        canvas.Scale(_pixelScale);

        _canvasWidth = e.Info.Width / _pixelScale;
        var canvasHeight = e.Info.Height / _pixelScale;

        _initialBarY = Convert.ToSingle(canvasHeight - BubbleSize - Padding.Bottom);

        using var paint = new SKPaint();
        paint.IsAntialias = true;
        paint.Style = SKPaintStyle.Fill;
        paint.Color = TabBarBackground.ToSKColor();
        paint.StrokeCap = SKStrokeCap.Round;
        paint.StrokeJoin = SKStrokeJoin.Round;

        var itemsCount = TabItems.Count;

        var widthF = _canvasWidth;
        var bottomPadding = Convert.ToSingle(Padding.Bottom);

        var bubbleSize = Convert.ToSingle(BubbleSize);
        var bubbleRadius = bubbleSize / 2;
        var bubbleMargin = Convert.ToSingle(BubbleMargin);
        var itemWidth = GetItemWidth(widthF);
        var iconPadding = Convert.ToSingle(IconPadding);
        var iconSize = Convert.ToSingle(GetIconSize());

        var isFirstPartOfAnimation = SelectionState <= 0.5;
        var selectionState = isFirstPartOfAnimation ? SelectionState : SelectionState - 0.5;
        var multiplier = Convert.ToSingle(selectionState / 0.5);
        var progressState = Convert.ToSingle(SelectionState / 1);
        var animationPartMultiplier = isFirstPartOfAnimation ? 1 - multiplier : multiplier;

        var workingIndex = isFirstPartOfAnimation ? _previousSelectedIndex : _displayedIndex;

        var middleXOfProcessingTab = GetTabCenterX(workingIndex, itemWidth);
        var middleXOfTargetTab = GetTabCenterX(_displayedIndex, itemWidth);
        var middleXOfPreviousTab = GetTabCenterX(_previousSelectedIndex, itemWidth);

        DrawCurvedBackground(
            paint,
            canvas,
            bubbleSize,
            bubbleRadius,
            bubbleMargin,
            animationPartMultiplier,
            middleXOfProcessingTab,
            widthF,
            bottomPadding
        );

        DrawSelectionBubble(
            paint,
            canvas,
            middleXOfTargetTab,
            middleXOfPreviousTab,
            bubbleSize,
            middleXOfProcessingTab,
            progressState,
            bubbleMargin,
            bubbleRadius
        );

        DrawTabIcons(
            canvas,
            bubbleRadius,
            iconPadding,
            itemsCount,
            itemWidth,
            iconSize,
            paint,
            progressState
        );
    }

    private void DrawTabIcons(
        SKCanvas canvas,
        float bubbleRadius,
        float iconPadding,
        int itemsCount,
        float itemWidth,
        float iconSize,
        SKPaint paint,
        float progressState
    )
    {
        using (new SKAutoCanvasRestore(canvas))
        {
            canvas.Translate(0, _initialBarY);

            var selectedYState = -bubbleRadius + iconPadding;
            var unselectedYState = iconPadding;

            for (var i = 0; i < itemsCount; i++)
            {
                if (i == _previousSelectedIndex || i == _displayedIndex)
                    continue;

                using (new SKAutoPaintRestore(paint))
                {
                    if (CheckIsColorSet(UnselectedIconTintColor))
                    {
                        var tintColor = UnselectedIconTintColor!.ToSKColor();

                        if (_touchedTabIndex == i)
                            tintColor = tintColor.WithAlpha(Convert.ToByte(_clickedIconColorOpacityState * 255));

                        paint.ColorFilter = SKColorFilter.CreateBlendMode(tintColor, SKBlendMode.SrcIn);
                    }

                    DrawTabIcon(TabItems[i].UnselectedIcon, iconSize, canvas, GetIconLeft(i, itemWidth, iconSize), unselectedYState, paint);
                }
            }

            var targetMultiplier = Math.Min(Math.Max(progressState, byte.MinValue), 1);
            var currentMultiplier = 1 - targetMultiplier;

            var targetAlpha = Convert.ToByte(targetMultiplier * 255);
            var currentAlpha = Convert.ToByte(currentMultiplier * 255);

            var yIncrease = bubbleRadius * progressState;

            var targetY = unselectedYState - yIncrease;
            var currentY = selectedYState + yIncrease;

            var previousIconLeft = GetIconLeft(_previousSelectedIndex, itemWidth, iconSize);
            var targetIconLeft = GetIconLeft(_displayedIndex, itemWidth, iconSize);

            //current
            if (progressState < 1)
            {
                using (new SKAutoPaintRestore(paint))
                {
                    if (CheckIsColorSet(UnselectedIconTintColor))
                        paint.ColorFilter = SKColorFilter.CreateBlendMode(
                            UnselectedIconTintColor!.ToSKColor().WithAlpha(targetAlpha),
                            SKBlendMode.SrcIn
                        );
                    DrawTabIcon(
                        TabItems[_previousSelectedIndex].UnselectedIcon,
                        iconSize,
                        canvas,
                        previousIconLeft,
                        currentY,
                        paint
                    );
                }

                using (new SKAutoPaintRestore(paint))
                {
                    if (CheckIsColorSet(SelectedIconTintColor))
                        paint.ColorFilter =
                            SKColorFilter.CreateBlendMode(
                                SelectedIconTintColor!.ToSKColor().WithAlpha(currentAlpha),
                                SKBlendMode.SrcIn
                            );
                    paint.ImageFilter =
                        SKImageFilter.CreateDropShadow(2, 2, 7f, 7f, SKColors.Black.WithAlpha(currentAlpha));
                    DrawTabIcon(
                        TabItems[_previousSelectedIndex].SelectedIcon,
                        iconSize,
                        canvas,
                        previousIconLeft,
                        currentY,
                        paint
                    );
                }
            }

            //target
            using (new SKAutoPaintRestore(paint))
            {
                if (CheckIsColorSet(UnselectedIconTintColor))
                    paint.ColorFilter = SKColorFilter.CreateBlendMode(
                        UnselectedIconTintColor!.ToSKColor().WithAlpha(currentAlpha),
                        SKBlendMode.SrcIn
                    );
                DrawTabIcon(
                    TabItems[_displayedIndex].UnselectedIcon,
                    iconSize,
                    canvas,
                    targetIconLeft,
                    targetY,
                    paint
                );
            }

            using (new SKAutoPaintRestore(paint))
            {
                if (CheckIsColorSet(SelectedIconTintColor))
                    paint.ColorFilter =
                        SKColorFilter.CreateBlendMode(
                            SelectedIconTintColor!.ToSKColor().WithAlpha(targetAlpha),
                            SKBlendMode.SrcIn
                        );
                paint.ImageFilter =
                    SKImageFilter.CreateDropShadow(2, 2, 7f, 7f, SKColors.Black.WithAlpha(targetAlpha));
                DrawTabIcon(
                    TabItems[_displayedIndex].SelectedIcon,
                    iconSize,
                    canvas,
                    targetIconLeft,
                    targetY,
                    paint
                );
            }
        }
    }

    // The background spans the full width; the tabs are laid out between the horizontal paddings (side safe areas).
    private float GetItemWidth(float width)
    {
        return Convert.ToSingle((width - Padding.Left - Padding.Right) / TabItems.Count);
    }

    private float GetTabCenterX(int index, float itemWidth)
    {
        return Convert.ToSingle(Padding.Left) + itemWidth * index + itemWidth / 2;
    }

    private float GetIconLeft(int index, float itemWidth, float iconSize)
    {
        return GetTabCenterX(index, itemWidth) - iconSize / 2;
    }

    private void DrawSelectionBubble(
        SKPaint paint,
        SKCanvas canvas,
        float middleXOfTargetTab,
        float middleXOfPreviousTab,
        float bubbleSize,
        float middleXOfProcessingTab,
        float progressState,
        float bubbleMargin,
        float bubbleRadius
    )
    {
        using (new SKAutoPaintRestore(paint))
        {
            using (new SKAutoCanvasRestore(canvas))
            {
                paint.ImageFilter = SKImageFilter.CreateDropShadow(2, 2, 7f, 7f, SKColors.Black);
                paint.Color = BubbleColor.ToSKColor();

                var isNewIndexHigher = _displayedIndex > _previousSelectedIndex;
                var diff = Math.Abs(middleXOfTargetTab - middleXOfPreviousTab);

                var cX = middleXOfProcessingTab;

                if (progressState < 1)
                {
                    cX = middleXOfPreviousTab + diff * progressState * (isNewIndexHigher ? 1 : -1);
                }

                var cY = CalculateBubbleY(
                    cX,
                    middleXOfPreviousTab,
                    middleXOfTargetTab,
                    bubbleSize,
                    bubbleMargin
                );

                if (_bounceState > 0)
                {
                    var yDiff = Convert.ToSingle(_bounceState * bubbleMargin);
                    cY += yDiff;
                }

                canvas.DrawCircle(cX, cY, bubbleRadius, paint);
            }
        }
    }

    private void DrawCurvedBackground(
        SKPaint paint,
        SKCanvas canvas,
        float bubbleSize,
        float bubbleRadius,
        float bubbleMargin,
        float animationPartMultiplier,
        float middleXOfProcessingTab,
        float widthF,
        float bottomPadding
    )
    {
        using (new SKAutoPaintRestore(paint))
        {
            using (new SKAutoCanvasRestore(canvas))
            {
                canvas.Translate(0, _initialBarY);

                var bottomY = bubbleSize - bubbleRadius + bubbleMargin;
                bottomY *= animationPartMultiplier;

                if (_curveBounceState > 0)
                {
                    var marginDiff = Convert.ToSingle((1 - _curveBounceState) * bubbleMargin);
                    bottomY -= marginDiff;
                }

                using var pathBuilder = new SKPathBuilder();
                pathBuilder.MoveTo(0, 0);
                pathBuilder.LineTo(middleXOfProcessingTab - bubbleSize - bubbleMargin, 0);
                pathBuilder.CubicTo(
                    middleXOfProcessingTab - bubbleRadius,
                    0,
                    middleXOfProcessingTab - bubbleRadius - bubbleMargin,
                    bottomY,
                    middleXOfProcessingTab,
                    bottomY
                );
                pathBuilder.CubicTo(
                    middleXOfProcessingTab + bubbleRadius + bubbleMargin,
                    bottomY,
                    middleXOfProcessingTab + bubbleRadius,
                    0,
                    middleXOfProcessingTab + bubbleSize + bubbleMargin,
                    0
                );
                pathBuilder.LineTo(widthF, 0);
                pathBuilder.LineTo(widthF, bubbleSize + bottomPadding);
                pathBuilder.LineTo(0, bubbleSize + bottomPadding);
                pathBuilder.Close();

                using var path = pathBuilder.Detach();
                canvas.DrawPath(path, paint);
            }
        }
    }

    // The bubble flies along a parabola through the previous tab (bar top), an apex one bubble above
    // the middle point and the target tab (slightly below the bar top, for the landing bounce).
    private float CalculateBubbleY(
        float cX,
        float middleXOfPreviousTab,
        float middleXOfTargetTab,
        float bubbleSize,
        float bubbleMargin
    )
    {
        if (_displayedIndex == _previousSelectedIndex)
            return _initialBarY;

        double x0 = middleXOfPreviousTab;
        double x2 = middleXOfTargetTab;
        var x1 = (x0 + x2) / 2;

        double y0 = _initialBarY;
        var y1 = y0 - bubbleSize;
        var y2 = y0 + bubbleMargin;

        // Lagrange form of the quadratic that passes through the three points.
        return Convert.ToSingle(
            y0 * (cX - x1) * (cX - x2) / ((x0 - x1) * (x0 - x2)) +
            y1 * (cX - x0) * (cX - x2) / ((x1 - x0) * (x1 - x2)) +
            y2 * (cX - x0) * (cX - x1) / ((x2 - x0) * (x2 - x1))
        );
    }

    private void DrawTabIcon(
        ImageSource? icon,
        float iconSize,
        SKCanvas canvas,
        float left,
        float imageY,
        SKPaint paint
    )
    {
        if (icon == null || !_icons.TryGetValue(icon, out var image))
        {
            DrawEmptyIcon(iconSize, canvas, left, imageY, paint);
            return;
        }

        // Icons are cached at pixel size, so fit them back into the icon square in device-independent units.
        var scale = Math.Min(iconSize / image.Width, iconSize / image.Height);
        var width = image.Width * scale;
        var height = image.Height * scale;

        var x = left + (iconSize - width) / 2f;
        var y = imageY + (iconSize - height) / 2f;

        // Images are multiplied by the paint alpha, so the bar background color must not fade the icons.
        var color = paint.Color;
        paint.Color = SKColors.Black;
        canvas.DrawImage(image, new SKRect(x, y, x + width, y + height), IconSampling, paint);
        paint.Color = color;
    }

    private void DrawEmptyIcon(float iconSize, SKCanvas canvas, float left, float imageY, SKPaint paint)
    {
        var color = paint.Color;
        paint.Color = SKColors.Black;
        var radius = iconSize / 2f;
        canvas.DrawCircle(left + radius, imageY + radius, radius, paint);
        paint.Color = color;
    }

    private async void ReloadIcons()
    {
        _iconsLoadingCts?.Cancel();
        _iconsLoadingCts?.Dispose();
        _iconsLoadingCts = null;

        if (Handler == null)
            return;

        var cts = new CancellationTokenSource();
        _iconsLoadingCts = cts;

        var pixelSize = (int)Math.Ceiling(GetIconSize() * Math.Max(DeviceDisplay.MainDisplayInfo.Density, 1));

        if (pixelSize <= 0)
            return;

        var sources = TabItems
            .SelectMany(item => new[] { item.SelectedIcon, item.UnselectedIcon })
            .OfType<ImageSource>()
            .Distinct()
            .ToList();

        var icons = new Dictionary<ImageSource, SKImage>();

        foreach (var source in sources)
        {
            try
            {
                using var bitmap = (await source.ToSkiaBitmap()).ResizeAndKeepAspectRatio(pixelSize, pixelSize);

                if (!bitmap.IsEmpty)
                    icons[source] = SKImage.FromBitmap(bitmap);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{nameof(BouncedTabBar)}: failed to load tab icon {source}. {ex}");
            }

            if (!cts.IsCancellationRequested)
                continue;

            DisposeIcons(icons);
            return;
        }

        DisposeIcons(_icons);
        _icons = icons;
        InvalidateSurface();
    }

    private void ClearIcons()
    {
        _iconsLoadingCts?.Cancel();
        _iconsLoadingCts?.Dispose();
        _iconsLoadingCts = null;

        DisposeIcons(_icons);
    }

    private static void DisposeIcons(Dictionary<ImageSource, SKImage> icons)
    {
        foreach (var icon in icons.Values)
            icon.Dispose();

        icons.Clear();
    }

    private void AnimateToSelectedIndex()
    {
        // A selection made during an animation is applied once the running animation finishes.
        if (_isAnimationOnProcessing
            || SelectedIndex < 0
            || SelectedIndex >= TabItems.Count
            || SelectedIndex == _displayedIndex)
            return;

        // Animations need the platform view; until it exists, jump straight to the new tab.
        if (Handler == null)
        {
            _previousSelectedIndex = _displayedIndex = SelectedIndex;
            SelectionState = 1;
            InvalidateSurface();
            return;
        }

        _previousSelectedIndex = _displayedIndex;
        _displayedIndex = SelectedIndex;

        _isAnimationOnProcessing = true;

        this.Animate(
            JumpAnimation,
            d =>
            {
                SelectionState = d;
                InvalidateSurface();
            },
            0,
            1,
            length: SwitchTabAnimationLength,
            easing: Easing.SinOut,
            finished: (_, _) =>
            {
                SelectionState = 1;
                _previousSelectedIndex = _displayedIndex;

                var halfOfBounceLenght = BounceAnimationLength / 2;

                this.Animate(
                    JumpInertiaAnimation,
                    d =>
                    {
                        _bounceState = d;
                        InvalidateSurface();
                    },
                    1,
                    0,
                    length: BounceAnimationLength,
                    easing: Easing.Linear,
                    finished: (_, _) =>
                    {
                        _bounceState = 0;
                        InvalidateSurface();
                    }
                );

                this.Animate(
                    CurveInertiaAnimation,
                    d =>
                    {
                        _curveBounceState = d;
                        InvalidateSurface();
                    },
                    1,
                    0.0001,
                    length: BounceAnimationLength,
                    easing: Easing.Linear,
                    finished: (_, _) =>
                    {
                        this.Animate(
                            CurveInertiaAnimation,
                            d =>
                            {
                                _curveBounceState = d;
                                InvalidateSurface();
                            },
                            0.0001,
                            1,
                            length: halfOfBounceLenght,
                            easing: Easing.Linear,
                            finished: (_, _) =>
                            {
                                _curveBounceState = 1;
                                InvalidateSurface();
                                _isAnimationOnProcessing = false;

                                AnimateToSelectedIndex();
                            }
                        );
                    }
                );
            }
        );
    }

    private void AnimateTouching(bool shouldFadeOut = true)
    {
        var endState = shouldFadeOut ? 0.1 : 1;

        this.Animate(
            TouchFeedbackAnimation,
            d =>
            {
                _clickedIconColorOpacityState = d;
                InvalidateSurface();
            },
            shouldFadeOut ? 1 : 0.1,
            endState,
            length: shouldFadeOut ? 100u : 200u,
            easing: Easing.SinOut,
            finished: (_, _) => _clickedIconColorOpacityState = endState
        );
    }

    private void UpdateItemsSubscription(INotifyCollectionChanged items, bool shouldSubscribe = true)
    {
        if (shouldSubscribe)
            items.CollectionChanged += OnItemsCollectionChanged;
        else
            items.CollectionChanged -= OnItemsCollectionChanged;
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(OnItemsChanged);
    }

    private void OnItemsChanged()
    {
        ClampIndices();
        AnimateToSelectedIndex();
        ReloadIcons();
        InvalidateSurface();
    }

    // Keeps the drawn indices valid when tabs are removed, including during a running animation.
    private void ClampIndices()
    {
        if (TabItems.Count == 0)
        {
            _displayedIndex = _previousSelectedIndex = 0;
            _touchedTabIndex = -1;
            return;
        }

        var maxIndex = TabItems.Count - 1;

        _displayedIndex = Math.Clamp(_displayedIndex, 0, maxIndex);
        _previousSelectedIndex = Math.Clamp(_previousSelectedIndex, 0, maxIndex);

        if (_touchedTabIndex > maxIndex)
            _touchedTabIndex = -1;

        var selectedIndex = Math.Clamp(SelectedIndex, 0, maxIndex);

        if (selectedIndex != SelectedIndex)
            SelectedIndex = selectedIndex;
    }

    private bool HasTabs()
    {
        return TabItems.Count != 0;
    }

    private bool CheckIsColorSet(Color? color)
    {
        return color != null && !color.Equals(Colors.Transparent);
    }
}
