using MetroLog;
using MetroLog.Targets;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;

namespace CardioMeter;

public partial class App : Application
{
	public App()
	{
		ConfigureLogger();
		Services = ConfigureServices();
		
		InitializeComponent();

		MainPage = new AppShell();
		
	}
	
	public new static App Current => (App)Application.Current;
	
	public IServiceProvider Services { get; }

	private static void ConfigureLogger()
	{
		var config = new LoggingConfiguration();
		
		#if DEBUG
		config.AddTarget(
			LogLevel.Trace, 
			LogLevel.Fatal, 
			new TraceTarget());
		#endif
		
		config.AddTarget(
			LogLevel.Info, 
			LogLevel.Fatal, 
			new ConsoleTarget());
		
		LoggerFactory.Initialize(config);
	}
	
	private static IServiceProvider ConfigureServices()
	{
		var services = new ServiceCollection();
		
		services.AddSingleton(CrossBluetoothLE.Current);
		services.AddSingleton<BLEDeviceManager>();
		services.AddTransient<BLEHeartRateManager>();
		services.AddTransient<DummyHeartRateSource>();
		services.AddSingleton(HeartRateZone.FromPref());
		
		return services.BuildServiceProvider();
	}
}
