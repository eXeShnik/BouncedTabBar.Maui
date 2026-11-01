namespace IO.Exeshnik.BouncedTabBar.Maui;

// Tabs generated from ItemsSource + ItemTemplate: SelectedItem must follow taps on the bar.
public class ItemsSourceTabsPage : BouncedTabbedPage
{
    internal sealed record TabInfo(string Title, ImageSource Icon, Color Color);

    public ItemsSourceTabsPage()
    {
        BubbleColor = Colors.Orange;

        ItemTemplate = new DataTemplate(() =>
        {
            var title = new Label
            {
                FontSize = 24,
                HorizontalOptions = LayoutOptions.Center
            };
            title.SetBinding(Label.TextProperty, static (TabInfo tab) => tab.Title);

            var page = new ContentPage
            {
                Content = new VerticalStackLayout
                {
                    VerticalOptions = LayoutOptions.Center,
                    Spacing = 8,
                    Children = { title }
                }
            };
            page.SetBinding(TitleProperty, static (TabInfo tab) => tab.Title);
            page.SetBinding(IconImageSourceProperty, static (TabInfo tab) => tab.Icon);
            page.SetBinding(BackgroundColorProperty, static (TabInfo tab) => tab.Color);

            return page;
        });

        ItemsSource = new[]
        {
            new TabInfo("Alpha", "dotnet_bot.png", Colors.SteelBlue),
            new TabInfo("Beta", "net_white.png", Colors.SeaGreen),
            new TabInfo("Gamma", "dotnet_bot.png", Colors.IndianRed),
            new TabInfo("Delta", "net_white.png", Colors.DarkOrange)
        };
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        // Shown in the navigation bar, so taps on the bar can be checked against SelectedItem.
        if (propertyName == SelectedItemProperty.PropertyName)
            Title = $"SelectedItem: {(SelectedItem as TabInfo)?.Title}";
    }
}
