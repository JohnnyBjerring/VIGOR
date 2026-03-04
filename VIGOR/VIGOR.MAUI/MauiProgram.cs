using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace VIGOR.MAUI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		try
		{
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				});

			builder.Services.AddMauiBlazorWebView();

#if DEBUG
			builder.Services.AddBlazorWebViewDeveloperTools();
			builder.Logging.AddDebug();
			builder.Logging.AddConsole(); // Added this to see logs in dotnet run cmd
            builder.Logging.SetMinimumLevel(LogLevel.Trace); // Force all logs through
            builder.Logging.AddFilter("*", LogLevel.Trace);
            
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var logPath = Path.Combine(desktopPath, "MauiBlazorLog.txt");
            File.WriteAllText(logPath, "Starting MAUI App...\n"); // clear old logs
            builder.Logging.AddProvider(new FileLoggerProvider(logPath));
#endif

			builder.Services.AddAuthorizationCore();
			builder.Services.AddScoped<VIGOR.MAUI.Services.MauiAuthStateProvider>();
			builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(sp => sp.GetRequiredService<VIGOR.MAUI.Services.MauiAuthStateProvider>());
			builder.Services.AddScoped<VIGOR.Shared.Interfaces.Services.IAuthStateService>(sp => sp.GetRequiredService<VIGOR.MAUI.Services.MauiAuthStateProvider>());

			builder.Services.AddScoped<VIGOR.Shared.Interfaces.Services.IAuthService, VIGOR.MAUI.Services.ApiAuthService>();
			builder.Services.AddScoped<VIGOR.Shared.Interfaces.Services.IStartPageResolver, VIGOR.MAUI.Services.StartPageResolver>();
			builder.Services.AddScoped<VIGOR.Shared.Interfaces.Services.INavigationService, VIGOR.MAUI.Services.NavigationService>();
			
			// Note: For Android Emulator change localhost to 10.0.2.2
			builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri("http://localhost:5249/") });

			// Catch async/global exceptions that happen *after* CreateMauiApp completes
			AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
			{
				var executionPath = AppDomain.CurrentDomain.BaseDirectory;
				File.WriteAllText(Path.Combine(executionPath, "MauiGlobalCrashLog.txt"), args.ExceptionObject.ToString());
				if (Microsoft.Maui.Controls.Application.Current?.MainPage != null)
				{
					Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
					{
						Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("Crash", args.ExceptionObject.ToString(), "OK");
					});
				}
			};

			return builder.Build();
		}
		catch (Exception ex)
		{
			// MAUI Windows apps often detach from the console. Write the error to a physical file so we can read it!
			var executionPath = AppDomain.CurrentDomain.BaseDirectory;
			File.WriteAllText(Path.Combine(executionPath, "MauiCrashLog.txt"), ex.ToString());
			throw;
		}
	}
}

public class FileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    public FileLoggerProvider(string filePath) => _filePath = filePath;
    public ILogger CreateLogger(string categoryName) => new FileLogger(_filePath, categoryName);
    public void Dispose() { }
}

public class FileLogger : ILogger
{
    private readonly string _filePath;
    private readonly string _categoryName;
    public FileLogger(string filePath, string categoryName) { _filePath = filePath; _categoryName = categoryName; }
    public IDisposable BeginScope<TState>(TState state) => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (exception != null)
        {
            var msg = $"[{logLevel}] {_categoryName}: {exception}";
            File.AppendAllText(_filePath, msg + "\n");
            
            // Show popup on UI thread
            if (Microsoft.Maui.Controls.Application.Current?.MainPage != null)
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("App Error", exception.Message + "\n\nSee desktop logs for details.", "OK");
                });
            }
        }
        else if (logLevel >= LogLevel.Warning) 
        {
            var msg = $"[{logLevel}] {_categoryName}: {formatter(state, exception)}";
            File.AppendAllText(_filePath, msg + "\n");
            
            if (logLevel >= LogLevel.Error && Microsoft.Maui.Controls.Application.Current?.MainPage != null)
            {
                Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
                {
                    Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("App Error", formatter(state, exception), "OK");
                });
            }
        }
    }
}
