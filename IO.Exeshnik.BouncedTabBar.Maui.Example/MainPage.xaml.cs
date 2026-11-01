namespace IO.Exeshnik.BouncedTabBar.Maui;

public partial class MainPage
{
    private int _insertedTabsCount;

    private const int ItemsCount = 40;

    public MainPage()
    {
        InitializeComponent();

        for (var i = 1; i <= ItemsCount; i++)
        {
            Page4Items.Add(new Label { Text = $"ScrollView item {i}", TextColor = Colors.White });
            Page5Items.Add(new Label { Text = $"Grid row item {i}" });
        }

        Page6Items.ItemsSource = Enumerable.Range(1, ItemsCount).Select(i => $"CollectionView item {i}").ToList();
    }

    private void OnInsertTabClicked(object? sender, EventArgs e)
    {
        _insertedTabsCount++;

        Children.Insert(1, new ContentPage
        {
            Title = $"Inserted {_insertedTabsCount}",
            IconImageSource = "net_white.png",
            BackgroundColor = Colors.Orange,
            Content = new Label
            {
                Text = $"Inserted {_insertedTabsCount}",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        });
    }

    private void OnRemoveLastTabClicked(object? sender, EventArgs e)
    {
        if (Children.Count > 1)
            Children.RemoveAt(Children.Count - 1);
    }

    private void OnMoveLastTabClicked(object? sender, EventArgs e)
    {
        var last = Children[^1];
        Children.Remove(last);
        Children.Insert(0, last);
    }

    private void OnToggleIconClicked(object? sender, EventArgs e)
    {
        Page3.IconImageSource = Page3.IconImageSource is FileImageSource { File: "net_white.png" }
            ? "dotnet_bot.png"
            : "net_white.png";
    }

    private void OnToggleBarColorClicked(object? sender, EventArgs e)
    {
        BarBackgroundColor = BarBackgroundColor == Colors.LightYellow ? Colors.White : Colors.LightYellow;
    }

    private void OnReplacePage3ContentClicked(object? sender, EventArgs e)
    {
        var items = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(16, 0) };

        for (var i = 1; i <= ItemsCount; i++)
            items.Add(new Label { Text = $"Replaced content item {i}" });

        Page3.Content = new ScrollView { BackgroundColor = Colors.LightBlue, Content = items };
    }

    private async void OnPushScrollPageClicked(object? sender, EventArgs e)
    {
        var items = new VerticalStackLayout { Spacing = 4, Padding = new Thickness(16, 0) };

        for (var i = 1; i <= ItemsCount; i++)
            items.Add(new Label { Text = $"Pushed page item {i}", TextColor = Colors.White });

        if (sender is Element { Parent: Element { Parent: Page page } })
            await page.Navigation.PushAsync(new ContentPage
            {
                Title = "Pushed",
                Content = new ScrollView { BackgroundColor = Colors.DarkGreen, Content = items }
            });
    }

    private void OnPage6Refreshing(object? sender, EventArgs e)
    {
        Page6Refresh.IsRefreshing = false;
    }

    private void OnSetPage3PaddingClicked(object? sender, EventArgs e)
    {
        Page3.Padding = new Thickness(0, 0, 0, 40);
    }

    private void OnTogglePage6SpacingClicked(object? sender, EventArgs e)
    {
        var spacing = Page6Items.ItemsLayout is LinearItemsLayout { ItemSpacing: > 0 } ? 0 : 12;
        Page6Items.ItemsLayout = new LinearItemsLayout(ItemsLayoutOrientation.Vertical) { ItemSpacing = spacing };
    }
}
