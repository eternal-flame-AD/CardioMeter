namespace CardioMeter;

public partial class AppShell : Shell
{
	public AppShell()
	{
		#if !DEBUG
		MauiExceptions.UnhandledException += async (o, e) =>
		{
			await DisplayAlert("Fatal Exception",
				$"The Application Encountered a Fatal Exception:\n${e.ExceptionObject}\n\n It will now exit.",
				"OK");
		} ;
		#endif
		InitializeComponent();
	}
}
