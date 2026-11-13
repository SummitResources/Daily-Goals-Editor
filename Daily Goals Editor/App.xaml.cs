using Daily_Goals_Editor.UI;

namespace Daily_Goals_Editor;

public partial class App : Application
{
	private readonly IServiceProvider _services;

	// MainPage is resolved in CreateWindow rather than injected here: its controls read the color and style
	// resources that InitializeComponent loads, so building it before then fails.
	public App(IServiceProvider services)
	{
		InitializeComponent();
		TextScale.Load();
		_services = services;
		UserAppTheme = AppTheme.Light;
	}

	protected override Window CreateWindow(IActivationState? activationState) => new(_services.GetRequiredService<MainPage>())
	{
		Title = "Daily Goals Editor",
		Width = 1320,
		Height = 840,
		MinimumWidth = 1080,
		MinimumHeight = 640
	};
}
