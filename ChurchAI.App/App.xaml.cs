using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ChurchAI.Infrastructure.Data;
using ChurchAI.Core.Interfaces;
using ChurchAI.Infrastructure.Repositories;

namespace ChurchAI.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public IHost Host { get; private set; }



    private void LogCrash(Exception? ex, string source)
    {
        if (ex == null) return;
        try
        {
            System.IO.File.AppendAllText(@"C:\Churchxtra\crash.log", $"[{DateTime.Now}] CRASH ({source}): {ex.Message}\n{ex.StackTrace}\n\n");
        }
        catch { }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnMainWindowClose;

        AppDomain.CurrentDomain.UnhandledException += (s, ev) => LogCrash(ev.ExceptionObject as Exception, "AppDomain");
        DispatcherUnhandledException += (s, ev) => 
        {
            LogCrash(ev.Exception, "Dispatcher");
            ev.Handled = true; // Try to prevent immediate termination
        };
        TaskScheduler.UnobservedTaskException += (s, ev) => LogCrash(ev.Exception, "TaskScheduler");

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Database
                var dbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ChurchAI", "churchai_bible.db");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);
                services.AddDbContext<BibleDbContext>(options =>
                    options.UseSqlite($"Data Source={dbPath}"));

                // Repositories
                services.AddScoped<IBibleRepository, BibleRepository>();
                services.AddScoped<ITranslationRepository, TranslationRepository>();
                services.AddScoped<IHymnRepository, HymnRepository>();
                services.AddScoped<ChurchAI.Core.Interfaces.IMediaRepository, ChurchAI.Infrastructure.Repositories.MediaRepository>();
                services.AddScoped<ChurchAI.Core.Interfaces.ILowerThirdRepository, ChurchAI.Infrastructure.Repositories.LowerThirdRepository>();

                // Services
                services.AddSingleton<ChurchAI.App.Services.Interfaces.ISettingsService, ChurchAI.App.Services.SettingsService>();
                services.AddSingleton<ChurchAI.App.Services.Interfaces.INavigationService, ChurchAI.App.Services.NavigationService>();
                services.AddSingleton<ChurchAI.App.Services.Interfaces.IWindowService, ChurchAI.App.Services.WindowService>();
                services.AddSingleton<ChurchAI.App.Services.Interfaces.IThemeService, ChurchAI.App.Services.ThemeService>();
                services.AddSingleton<ChurchAI.App.Services.Interfaces.ISearchHistoryService, ChurchAI.App.Services.SearchHistoryService>();
                services.AddSingleton<ChurchAI.App.Services.Interfaces.IProjectionService, ChurchAI.App.Services.ProjectionService>();
                services.AddSingleton<ChurchAI.App.Services.Interfaces.INDIService, ChurchAI.App.Services.NDIService>();
                services.AddSingleton<ChurchAI.App.Services.Interfaces.IWebServerService, ChurchAI.App.Services.WebServerService>();
                services.AddSingleton<ChurchAI.App.Services.Interfaces.ICommunityMessageService, ChurchAI.App.Services.CommunityMessageService>();
                services.AddSingleton<ChurchAI.App.Services.Interfaces.IMediaService, ChurchAI.App.Services.MediaService>();
                services.AddSingleton<ChurchAI.App.Services.Interfaces.ICustomTemplateService, ChurchAI.App.Services.CustomTemplateService>();
                services.AddSingleton<ChurchAI.App.Services.Interfaces.IAITemplateExtractorService, ChurchAI.App.Services.AITemplateExtractorService>();

                // Core & AI Services
                services.AddSingleton<ChurchAI.Core.Interfaces.IBibleReferenceParser, ChurchAI.Core.Parsers.SpokenReferenceParser>();
                
                // Speech Recognition (Local, Cloud, Resolver)
                services.AddSingleton<ChurchAI.App.Services.SpeechRecognitionService>();
                services.AddSingleton<ChurchAI.App.Services.CloudSpeechRecognitionService>();
                services.AddSingleton<ChurchAI.App.Services.GoogleSpeechRecognitionService>();
                services.AddSingleton<ChurchAI.App.Services.Interfaces.ISpeechRecognitionService, ChurchAI.App.Services.SpeechRecognitionResolver>();

                // AI Intents (Local, Cloud, Resolver)
                services.AddHttpClient<ChurchAI.AI.Services.OllamaIntentService>(client =>
                {
                    client.BaseAddress = new System.Uri("http://localhost:11434");
                });
                services.AddHttpClient<ChurchAI.App.Services.CloudIntentService>();
                services.AddSingleton<ChurchAI.Core.Interfaces.IAIIntentService, ChurchAI.App.Services.IntentServiceResolver>();
                
                // ViewModels
                services.AddSingleton<ChurchAI.App.ViewModels.MainWindowViewModel>();
                services.AddTransient<ChurchAI.App.ViewModels.BibleSearchViewModel>();
                services.AddTransient<ChurchAI.App.ViewModels.ProjectionViewModel>();
                services.AddTransient<ChurchAI.App.ViewModels.HistoryViewModel>();
                services.AddTransient<ChurchAI.App.ViewModels.SettingsViewModel>();
                services.AddTransient<ChurchAI.App.ViewModels.LoginViewModel>();
                services.AddTransient<ChurchAI.App.ViewModels.HymnSearchViewModel>();
                services.AddTransient<ChurchAI.App.ViewModels.CommunityViewModel>();
                services.AddTransient<ChurchAI.App.ViewModels.MediaViewModel>();
                services.AddTransient<ChurchAI.App.ViewModels.CustomTemplateBuilderViewModel>();
                
                // Views
                services.AddSingleton<MainWindow>();
                services.AddTransient<ChurchAI.App.Views.LoginWindow>();
                services.AddTransient<ChurchAI.App.Views.CustomTemplateBuilderWindow>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddDebug();
            })
            .Build();

        base.OnStartup(e);

        try
        {
            Host.Start();

            using (var scope = Host.Services.CreateScope())
            {
                var bibleRepository = scope.ServiceProvider.GetRequiredService<IBibleRepository>();
                bibleRepository.InitializeDatabaseAsync().GetAwaiter().GetResult();
            }

            _ = Host.Services.GetRequiredService<ChurchAI.App.Services.Interfaces.IProjectionService>();
        }
        catch (Exception ex)
        {
            LogCrash(ex, "StartupInitialization");
        }

        var mainWindow = Host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private async void OnExit(object sender, ExitEventArgs e)
    {
        await Host.StopAsync();
        Host.Dispose();
    }
}
