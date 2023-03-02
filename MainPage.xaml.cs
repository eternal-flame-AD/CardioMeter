using System.Collections.Specialized;
using System.Net;
using System.Text;
using MathNet.Numerics.Interpolation;
using ILogger = MetroLog.ILogger;
using LoggerFactory = MetroLog.LoggerFactory;

namespace CardioMeter;

public partial class MainPage
{
	private static readonly  ILogger Log = LoggerFactory.GetLogger(nameof(MainPage));
	private BLEHeartRateManager _bleHeartRateManager = App.Current.Services.GetService<BLEHeartRateManager>();
	private DummyHeartRateSource _dummyHeartRateSource = App.Current.Services.GetService<DummyHeartRateSource>();
	private HeartRateZone _heartRateZone = App.Current.Services.GetService<HeartRateZone>();
	private IHeartRateProvider _heartrateSource;

	class TrackedValue<T>
	{
		public T Value;
		public string Format;
		public DateTime? LastUpdate;
		public TimeSpan? ValidityPeriod;
		public TimeSpan HistoryPeriod;
		public IColorConverter<T> ColorConverter;
		public bool DoDerivative;
		public string Unit;
		
		public readonly IOrderedDictionary History = new OrderedDictionary();

		public void UpdateValue(T value, DateTime ts)
		{
			lock (this)
			{
				LastUpdate = ts;
				Value = value;
				History.Add(ts, value);
				var toRemove = new List<DateTime>();
				foreach (var key in History.Keys)
				{
					if (DateTime.Now - (DateTime)key > HistoryPeriod)
						toRemove.Add((DateTime)key);
				}
				foreach (var key in toRemove)
				{
					History.Remove(key);
				}
			}
		}

		public void ClearHistory()
		{
			lock (this)
			{
				History.Clear();
				LastUpdate = null;
				Value = default;
			}
		}

		private IInterpolation InterpolateHistory()
		{
			var x = new double[History.Count];
			var y = new double[History.Count];
			var now = DateTime.Now;
			var i = 0;
			lock (this)
			{
				foreach (var key in History.Keys)
				{
					var secToNow = ((DateTime)key - now).TotalSeconds;
					x[i] = secToNow;
					var val = History[key]!;
					switch (val)
					{
						case int v:
							y[i] = v;
							break;
						case double v:
							y[i] = v;
							break;
						default: throw new NotSupportedException("Unsupported type");
					}

					i++;
				}
			}

			return LinearSpline.InterpolateSorted(x, y);
		}

		public string HTML()
		{
			if (LastUpdate == null)
					return "<span>-?-</span>";
			var htmlBuilder = new StringBuilder();
			{
				if (ColorConverter != null)
					htmlBuilder.Append($"<span style=\"color:{ColorConverter.Color(Value)};\">");
				else
					htmlBuilder.Append("<span>");

				if (LastUpdate == null)
					htmlBuilder.Append("-?-");
				else
				{
					if (Format != null)
						htmlBuilder.Append(WebUtility.HtmlEncode(String.Format(Format, Value)));
					else
						htmlBuilder.Append(WebUtility.HtmlEncode(Value.ToString()));
				}

				htmlBuilder.Append("</span>&nbsp;");
				
				htmlBuilder.Append($"<small style=\"font-size:smaller\">{WebUtility.HtmlEncode(Unit)}</small>");

				if (ValidityPeriod != null)
					htmlBuilder.Append($"<small style=\"opacity:{1-ComputeActivityOpacity():F2}\">.</small>");

				if (DoDerivative)
					try
					{
						var spline = InterpolateHistory();
						var diff = spline.Differentiate(0);
						diff = Double.Clamp(diff, -9.99, 9.99);
						htmlBuilder.Append($"<small>({(diff>0?"+":"")}{diff:F2})</small>");
					}
					catch (Exception e)
					{
						Log.Info("Failed to compute derivative", e);
						htmlBuilder.Append($"<small style=\"color:yellow\">(?)</small>");
					}
			}
			//Log.Info($"Update HTML: {htmlBuilder}");
			return htmlBuilder.ToString();
		}
		
		private double ComputeActivityOpacity()
		{
			if (LastUpdate == null)
				return 0;
			if (ValidityPeriod == null)
				return 1;
			var validity = 1 - (DateTime.Now - LastUpdate.Value).Ticks / (double)ValidityPeriod.Value.Ticks;
			if (validity < 0)
				validity = 0;
	
			return  1-validity;
		}
	}
	private TrackedValue<int> _heartRate = new()
	{
		ValidityPeriod = TimeSpan.FromSeconds(5),
		HistoryPeriod =  TimeSpan.FromMinutes(1),
		DoDerivative = true,
		Unit = "bpm",
	};
	private TrackedValue<double> _rrMs = new()
	{
		ValidityPeriod = TimeSpan.FromSeconds(5),
		HistoryPeriod =  TimeSpan.FromMinutes(1),
		Format = "{0:F2}",
		Unit = "ms",
	};
	
	public MainPage()
	{
		InitializeComponent();
		_heartRate.ColorConverter = _heartRateZone;
		setHeartrateSource(_dummyHeartRateSource);
	}
	

	private void setHeartrateSource(IHeartRateProvider heartrateSource)
	{
		if (_heartrateSource != null)
		{
			_heartrateSource.OnConnectionLost -= OnConnectionLost;
			_heartrateSource.HeartRateReceived -= OnHeartRateReceived;
			_heartrateSource.OnConnectionEstablished -= OnConnectionEstablished;
		}

		_heartrateSource = heartrateSource;
		if (heartrateSource != null)
		{
			((HeartRateGraph)heartRateGraphView.Drawable).setHeartRateSource(_heartrateSource);
			_heartrateSource.HeartRateReceived += OnHeartRateReceived;
			_heartrateSource.OnConnectionLost += OnConnectionLost;
			_heartrateSource.OnConnectionEstablished += OnConnectionEstablished;
		}
		_heartRate.ClearHistory();
		_rrMs.ClearHistory();
		UpdateUILabels();
	}

	private void OnConnectionEstablished(object sender, EventArgs e)
	{
		MainThread.BeginInvokeOnMainThread( () => {
		MainStatusLabel.Text = "Connected";
		MainStatusLabel.TextColor = Colors.Green;
		});
	}

	private void UpdateUILabels()
	{
		MainThread.BeginInvokeOnMainThread( () =>
		{
			HeartRateLabel.Text = _heartRate.HTML();
			RRIntervalLabel.Text = _rrMs.HTML();
			heartRateGraphView.Invalidate();
		});
	}

	private void OnHeartRateReceived(object sender, HeartRateEventArgs e)
	{
		lock (_heartRate)
		{
			_heartRate.UpdateValue(e.HeartRate, e.Timestamp);
			if (e.RRInterval > 0)
			{
				_rrMs.UpdateValue(e.RRInterval.Value, e.Timestamp);
			}
		}
		UpdateUILabels();
	}
	
	private void OnConnectionLost(object sender, EventArgs e)
	{
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			await DisplayAlert("Connection Lost", "The connection to the heart rate monitor was lost.", "OK");
			ConnectBtn.IsVisible = true;
			MainStatusLabel.Text = "Not connected, Synthetic Data";
			setHeartrateSource(_dummyHeartRateSource);
		});
	}
	
	protected override  void OnAppearing()
	{
		base.OnAppearing();
		_dummyHeartRateSource.KeepAlive();
		if (String.IsNullOrEmpty(AppPreferences.LastSelectedBLEUUID))
			ConnectBtn.Text = "Scan for Devices";
		else 
			ConnectBtn.Text = "Connect";
	}

	private async void OnConnectBtnClicked(object sender, EventArgs e)
	{
		try
		{
			var lastUUID = AppPreferences.LastSelectedBLEUUID;
			if (String.IsNullOrEmpty(lastUUID))
			{
				await Shell.Current.GoToAsync("//bledevices");
				return;
			}

			ConnectBtn.Text = "Connecting...";
			ConnectBtn.IsEnabled = false;
			var connected = await _bleHeartRateManager.Connect(new Guid(lastUUID));
			if (connected)
			{
				OnConnectionEstablished(this, EventArgs.Empty);
				setHeartrateSource(_bleHeartRateManager);
				ConnectBtn.IsVisible = false;
			}
		}
		catch (Exception ex)
		{
			Log.Info($"Caught Connect Exception: {ex}");
			await DisplayAlert("Connection Failed", ex.Message, "OK");
		}
		finally
		{
			ConnectBtn.IsEnabled = true;
			ConnectBtn.Text = "Connect";
		}
	}
}

