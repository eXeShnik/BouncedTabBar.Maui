namespace IO.Exeshnik.BouncedTabBar.Maui;

public static class BouncedPageExtensions
{
    #region SelectedIconProperty

    public static readonly BindableProperty SelectedIconProperty = BindableProperty.CreateAttached(
        "SelectedIcon",
        typeof(ImageSource),
        typeof(BouncedPageExtensions),
        null
    );

    public static ImageSource? GetSelectedIcon(this BindableObject bindable)
    {
        return (ImageSource?)bindable.GetValue(SelectedIconProperty);
    }

    public static void SetSelectedIcon(this BindableObject bindable, ImageSource? value)
    {
        bindable.SetValue(SelectedIconProperty, value);
    }

    #endregion

    #region UnselectedIconProperty

    public static readonly BindableProperty UnselectedIconProperty = BindableProperty.CreateAttached(
        "UnselectedIcon",
        typeof(ImageSource),
        typeof(BouncedPageExtensions),
        null
    );

    public static ImageSource? GetUnselectedIcon(this BindableObject bindable)
    {
        return (ImageSource?)bindable.GetValue(UnselectedIconProperty);
    }

    public static void SetUnselectedIcon(this BindableObject bindable, ImageSource? value)
    {
        bindable.SetValue(UnselectedIconProperty, value);
    }

    #endregion
}