using Microsoft.AspNetCore.Components.WebView.Maui;

namespace Gradio.Net;

public static class GradioServiceExtensions
{
    public static IServiceCollection AddGradio(this IServiceCollection services, Blocks blocks, Action<GradioServiceConfig>? additionalConfigurationAction = null)
    {
        var gradioApp = new GradioApp();
        services.AddSingleton(gradioApp);
        GradioServiceConfig gradioServiceConfig = new();
        additionalConfigurationAction?.Invoke(gradioServiceConfig);
        gradioApp.SetConfig(gradioServiceConfig);

        services.ConfigureMauiHandlers(delegate (IMauiHandlersCollection handlers)
        {
            handlers.AddHandler<IBlazorWebView>((IServiceProvider _) => new GradioBlazorWebViewHandler());
        });

        return services;
    }
}
