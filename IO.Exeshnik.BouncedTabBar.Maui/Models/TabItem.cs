namespace IO.Exeshnik.BouncedTabBar.Maui;

public class TabItem
{
    public Page? Page { get; }
    
    public ImageSource? SelectedIcon { get; set; }

    private ImageSource? _unselectedIcon;
    public ImageSource? UnselectedIcon
    {
        get => _unselectedIcon ?? SelectedIcon;
        set => _unselectedIcon = value;
    }

    public TabItem(Page page, ImageSource? selectedIcon, ImageSource? unselectedIcon)
    {
        Page = page;
        SelectedIcon = selectedIcon;
        UnselectedIcon = unselectedIcon;
    }

    public TabItem()
    {
        
    }
}