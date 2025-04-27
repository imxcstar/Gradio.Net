using Microsoft.Extensions.Logging;
using Gradio.Net;
using demo;

namespace Demo.Blazor.Hybrid.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddGradio(CreateBlocks().Result);

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

        static async Task<Blocks> CreateBlocks()
        {
            using (Blocks blocks = gr.Blocks())
            {
                await FirstDemo.Create();

                await MediaDemo.Create();

                await LayoutDemo.Create();

                await ChatbotDemo.Create();

                await FormDemo.Create();

                await ProgressDemo.Create();

                return blocks;
            }
        }
    }
}
