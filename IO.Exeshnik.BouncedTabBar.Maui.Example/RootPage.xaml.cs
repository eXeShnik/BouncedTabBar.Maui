namespace IO.Exeshnik.BouncedTabBar.Maui;

public partial class RootPage : ContentPage
{
    public RootPage()
    {
        InitializeComponent();
    }

    private async void OnXamlTabsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new MainPage());
    }

    private async void OnItemsSourceTabsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new ItemsSourceTabsPage());
    }

    private async void OnBottomPlacementTabsClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new BottomPlacementTabsPage());
    }
}
