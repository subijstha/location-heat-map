// MauiProgram.cs
// Application entry point — configures MAUI services and map integration.

using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace LocationHeatMap
{
    
    /// Configures and builds the MAUI application host.
    /// Registers required services including Maps and SQLite dependencies.
   
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                // Register .NET MAUI Maps
                .UseMauiMaps()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            // Enable detailed logging in debug builds
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
