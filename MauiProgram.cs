using ECHO_PRINT.Resources.pages;
using Microsoft.Extensions.Logging;
using Plugin.Maui.Audio;
using Plugin.LocalNotification;
using CommunityToolkit.Maui;
using ECHO_PRINT.Services;


namespace ECHO_PRINT
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseLocalNotification()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton(AudioManager.Current);
            builder.Services.AddTransient<RecordPage>();
            builder.UseMauiApp<App>().UseMauiCommunityToolkit();
            builder.Services.AddSingleton<EchoPrint_CRUD>();
            builder.Services.AddTransient<RecordList>();
            builder.Services.AddSingleton<IMediaPicker>(MediaPicker.Default);
 



#if DEBUG
            builder.Logging.AddDebug();
#endif


            return builder.Build();
        }
    }
}
