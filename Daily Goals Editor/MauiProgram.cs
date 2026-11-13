using Daily_Goals_Editor.Core;
using Microsoft.Extensions.Logging;

namespace Daily_Goals_Editor;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder.UseMauiApp<App>();
#if MACCATALYST
		builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<Entry, HomeEndEntryHandler>());
#endif

		builder.Services.AddTransient(_ => new DayPlanStore(FileSystem.AppDataDirectory));
		builder.Services.AddTransient<MainPage>();

		// The design draws its own input chrome (Ui.Input), so the native text field border would double it up.
		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("Borderless", (handler, _) =>
		{
#if MACCATALYST
			handler.PlatformView.BorderStyle = UIKit.UITextBorderStyle.None;
			handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
			handler.PlatformView.FocusEffect = null;
#elif WINDOWS
			handler.PlatformView.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
			handler.PlatformView.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
#endif
		});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
