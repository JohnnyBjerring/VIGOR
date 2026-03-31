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
            
            var tempPath = Path.GetTempPath();
            var logPath = Path.Combine(tempPath, "MauiBlazorLog.txt");
            try 
            { 
                File.WriteAllText(logPath, "Starting MAUI App...\n"); 
                System.Diagnostics.Debug.WriteLine($"Log file: {logPath}");
            } catch { } // Don't crash loading if logs fail
            builder.Logging.AddProvider(new FileLoggerProvider(logPath));
#endif

			builder.Services.AddCascadingAuthenticationState();
			builder.Services.AddAuthorizationCore();
			builder.Services.AddScoped<VIGOR.MAUI.Services.MauiAuthStateProvider>();
			builder.Services.AddScoped<Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider>(sp => sp.GetRequiredService<VIGOR.MAUI.Services.MauiAuthStateProvider>());
			builder.Services.AddScoped<VIGOR.Shared.Interfaces.Services.IAuthStateService>(sp => sp.GetRequiredService<VIGOR.MAUI.Services.MauiAuthStateProvider>());

			builder.Services.AddScoped<VIGOR.Shared.Interfaces.Services.IAuthService, VIGOR.MAUI.Services.ApiAuthService>();
			builder.Services.AddScoped<VIGOR.Shared.Interfaces.Services.IStartPageResolver, VIGOR.MAUI.Services.StartPageResolver>();
			builder.Services.AddScoped<VIGOR.Shared.Interfaces.Services.INavigationService, VIGOR.MAUI.Services.NavigationService>();
			builder.Services.AddScoped<VIGOR.Shared.Interfaces.Services.ICitizenService, VIGOR.Shared.Services.CitizenClientService>();
			
			// Note: For Android Emulator change localhost to 10.0.2.2
			var baseAddress = DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:5249/" : "http://localhost:5249/";
			builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(baseAddress) });

			// Catch async/global exceptions that happen *after* CreateMauiApp completes
			AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
			{
				try
				{
					var tempPath = Path.Combine(Path.GetTempPath(), "MauiGlobalCrashLog.txt");
					File.WriteAllText(tempPath, args.ExceptionObject.ToString());
				}
				catch { }
				if (Microsoft.Maui.Controls.Application.Current?.MainPage != null)
				{
					Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
					{
						Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert("Crash", args.ExceptionObject.ToString(), "OK");
					});
				}
			};

			System.Diagnostics.Debug.WriteLine("MAUI App is built and returning...");
			return builder.Build();
		}
		catch (Exception ex)
		{
			// Safe logging to avoid crashes during logging
			try
			{
				var tempPath = Path.Combine(Path.GetTempPath(), "MauiCrashLog.txt");
				File.WriteAllText(tempPath, ex.ToString());
				System.Diagnostics.Debug.WriteLine($"Maui crash log written to: {tempPath}");
			}
			catch 
			{
				System.Diagnostics.Debug.WriteLine("CRITICAL ERROR: Failed to write crash log! " + ex.Message);
			}
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
