using AndroidConfiguration = Microsoft.Maui.Controls.PlatformConfiguration.Android;
using AndroidTabbedPage = Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.TabbedPage;
using ToolbarPlacement = Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific.ToolbarPlacement;

namespace IO.Exeshnik.BouncedTabBar.Maui;

// Android bottom tab placement: MAUI uses a BottomNavigationView and reserves a content margin for it, which it
// sets again whenever the page reappears (push a page and go back to check).
public class BottomPlacementTabsPage : BouncedTabbedPage
{
    public BottomPlacementTabsPage()
    {
        AndroidTabbedPage.SetToolbarPlacement(On<AndroidConfiguration>(), ToolbarPlacement.Bottom);

        var colors = new[] { Colors.MediumPurple, Colors.CadetBlue, Colors.Goldenrod };

        for (var i = 0; i < colors.Length; i++)
        {
            var pushButton = new Button
            {
                Text = "Push page",
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };
            pushButton.Clicked += async (_, _) => await Navigation.PushAsync(new ContentPage { Title = "Pushed" });

            Children.Add(new ContentPage
            {
                Title = $"Bottom {i + 1}",
                IconImageSource = "dotnet_bot.png",
                BackgroundColor = colors[i],
                Content = new Grid
                {
                    Children =
                    {
                        new Label { Text = "Top", HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Start },
                        pushButton,
                        new Label { Text = "Bottom", HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.End }
                    }
                }
            });
        }
    }
}
