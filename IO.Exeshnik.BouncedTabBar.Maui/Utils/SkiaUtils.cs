using System.Reflection;

#if ANDROID
using SkiaSharp.Views.Android;
#elif IOS || MACCATALYST
using SkiaSharp.Views.iOS;
#endif

using SkiaSharp;

namespace IO.Exeshnik.BouncedTabBar.Maui;

public static class SkiaUtils
{
    public static async Task<SKBitmap> ToSkiaBitmap(this ImageSource imageSource)
    {
#if ANDROID || IOS || MACCATALYST
        if (Application.Current?.Handler?.MauiContext?.Services is null)
            return new SKBitmap();

        var handler = Application.Current.Handler.MauiContext.Services.GetService<IImageSourceServiceProvider>()?.GetImageSourceService(imageSource.GetType());

        if (handler is null)
            return new SKBitmap();

#if ANDROID
        using var image = await handler
            .GetDrawableAsync(imageSource, Android.App.Application.Context);

        return image?.Value is Android.Graphics.Drawables.BitmapDrawable bitmapDrawable ? bitmapDrawable.Bitmap.ToSKBitmap() : new SKBitmap();
#elif IOS || MACCATALYST
        using var image = await handler
            .GetImageAsync(imageSource, Convert.ToSingle(DeviceDisplay.MainDisplayInfo.Density));

        return image?.Value is not null ? image.Value.ToSKBitmap() : new SKBitmap();
#endif

#elif WINDOWS
        switch (imageSource)
        {
            case FileImageSource { File.Length: > 0 } fileImageSource:
                return DecodeAppFile(fileImageSource.File);
            case StreamImageSource { Stream: not null } streamImageSource:
                await using (var stream = await streamImageSource.Stream(CancellationToken.None))
                    return stream is null ? new SKBitmap() : SKBitmap.Decode(stream) ?? new SKBitmap();
            default:
                return new SKBitmap();
        }
#else
        return new SKBitmap();
#endif
    }

#if WINDOWS
    // MAUI's resizetizer writes MauiImage files next to the app with scale qualifiers (name.scale-200.png),
    // so pick the scale matching the display and fall back to the plain file name.
    private static SKBitmap DecodeAppFile(string file)
    {
        var path = Path.IsPathRooted(file) ? file : FindScaledAppFile(file) ?? Path.Combine(AppContext.BaseDirectory, file);

        return File.Exists(path) ? SKBitmap.Decode(path) ?? new SKBitmap() : new SKBitmap();
    }

    private static string? FindScaledAppFile(string file)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, Path.GetDirectoryName(file) ?? string.Empty);

        if (!Directory.Exists(directory))
            return null;

        var candidates = Directory
            .EnumerateFiles(directory, $"{Path.GetFileNameWithoutExtension(file)}.scale-*{Path.GetExtension(file)}")
            .OrderBy(GetFileScale)
            .ToList();
        var displayScale = DeviceDisplay.MainDisplayInfo.Density * 100;

        // The smallest scale that still covers the display, otherwise the largest one available.
        return candidates.FirstOrDefault(path => GetFileScale(path) >= displayScale) ?? candidates.LastOrDefault();
    }

    private static int GetFileScale(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var scale = name[(name.LastIndexOf(".scale-", StringComparison.Ordinal) + ".scale-".Length)..];

        return int.TryParse(scale, out var value) ? value : 0;
    }
#endif

    public static SKTypeface ToTypeface(
        this string fontFamily,
        FontAttributes fontAttributes = FontAttributes.None,
        Assembly? assembly = null
    )
    {
        assembly ??= typeof(SkiaUtils).Assembly;

        if (!string.IsNullOrEmpty(fontFamily))
        {
            var resourcesList = assembly.GetManifestResourceNames();

            foreach (var s in resourcesList)
            {
                if (!s.ToLowerInvariant().Contains($"{fontFamily.ToLowerInvariant()}."))
                    continue;

                try
                {
                    return SKTypeface.FromStream(assembly.GetManifestResourceStream(s));
                }
                catch
                {
                    return SKTypeface.CreateDefault();
                }
            }
        }

        var isItalic = fontAttributes.HasFlag(FontAttributes.Italic);
        var isBold = fontAttributes.HasFlag(FontAttributes.Bold);

        var fontStyle = isItalic switch
        {
            true when isBold => SKFontStyle.BoldItalic,
            true => SKFontStyle.Italic,
            _ => isBold ? SKFontStyle.Bold : SKFontStyle.Normal
        };

        return SKTypeface.FromFamilyName(fontFamily, fontStyle);
    }

    public static SKBitmap ResizeAndKeepAspectRatio(
        this SKBitmap source,
        int targetWidth,
        int targetHeight
    )
    {
        if (source.IsEmpty || source.Width == targetWidth && source.Height == targetHeight)
            return source;

        // Fit inside the target size: the larger ratio decides, so neither side exceeds the target.
        var scalingX = (float)source.Width / targetWidth;
        var scalingY = (float)source.Height / targetHeight;
        var scaling = Math.Max(scalingX, scalingY);

        var newWidth = Math.Clamp((int)Math.Round(source.Width / scaling), 1, targetWidth);
        var newHeight = Math.Clamp((int)Math.Round(source.Height / scaling), 1, targetHeight);

        var resized = source.Resize(new SKSizeI(newWidth, newHeight), new SKSamplingOptions(SKFilterMode.Linear));
        source.Dispose();

        return resized;
    }
}

public class SKAutoPaintRestore : IDisposable
{
    private static readonly List<PropertyInfo> PaintProperties = new List<PropertyInfo>();

    private readonly Dictionary<PropertyInfo, object?> _initialValues = new Dictionary<PropertyInfo, object?>();

    private SKPaint? _paint;
    private Type? _type;

    public SKAutoPaintRestore(SKPaint? paint)
    {
        _paint = paint;
        _type = paint?.GetType();

        InitPropertiesIfNeeded();
        GetInitialValues();
    }

    private void InitPropertiesIfNeeded()
    {
        if (PaintProperties.Count > 0 || _type is null)
            return;

        foreach (var propertyInfo in _type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (propertyInfo.PropertyType == typeof(IntPtr) ||
                propertyInfo.SetMethod == null ||
                propertyInfo.CustomAttributes.FirstOrDefault(x => x.AttributeType == typeof(ObsoleteAttribute)) !=
                null)
                continue;

            PaintProperties.Add(propertyInfo);
        }
    }

    private void GetInitialValues()
    {
        foreach (var paintProperty in PaintProperties)
            _initialValues.Add(paintProperty, paintProperty.GetValue(_paint));
    }

    private void SetInitialValues()
    {
        foreach (var initialValue in _initialValues)
        {
            var currentValue = initialValue.Key.GetValue(_paint);

            initialValue.Key.SetValue(_paint, initialValue.Value);

            if (currentValue is IDisposable disposable)
                disposable.Dispose();
        }
    }

    public void Dispose()
    {
        SetInitialValues();

        _initialValues.Clear();
        _paint = null;
        _type = null;
    }
}