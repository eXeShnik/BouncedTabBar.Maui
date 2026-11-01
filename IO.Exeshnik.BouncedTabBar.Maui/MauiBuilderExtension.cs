using SkiaSharp.Views.Maui.Controls.Hosting;

namespace IO.Exeshnik.BouncedTabBar.Maui;

public static class MauiBuilderExtension
{
    public static MauiAppBuilder UseBouncedTabBar(this MauiAppBuilder builder)
    {
        return builder
            .UseSkiaSharp()
            .ConfigureMauiHandlers(h => h.AddHandler<BouncedTabbedPage, BouncedTabbedPageHandler>());
    }
}