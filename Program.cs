using Avalonia;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using GameLauncher.Functions;

namespace GameLauncher;

class Program
{
    public static readonly string BasePath = AppDomain.CurrentDomain.BaseDirectory; //@"C:\Users\Geisthardt\AppData\Local\Programs\PaulsGameLauncher";
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static Task Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return Task.CompletedTask;
    } 

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    
    

    
    
    
}