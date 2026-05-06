using Microsoft.Extensions.Logging;
using ZXing.Net.Maui.Controls; // YENİ EKLENDİ
using ZXing.Net.Maui;

namespace CampusLostAndFound.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
               .UseBarcodeReader() // YENİ EKLENDİ: Uygulamaya QR okuma yeteneği verir
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}