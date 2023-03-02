using System.Collections.ObjectModel;
using Plugin.BLE.Abstractions.Contracts;
using ILogger = MetroLog.ILogger;
using LoggerFactory = MetroLog.LoggerFactory;
namespace CardioMeter;

public partial class DeviceListBLETab
{
    private static readonly  ILogger Log = LoggerFactory.GetLogger(nameof(DeviceListBLETab));
    private  BLEDeviceManager _bleDeviceManager => App.Current.Services.GetService<BLEDeviceManager>();
    private  bool _isScanning;
    ObservableCollection<IDevice> DiscoveredDevices = new ();

    public DeviceListBLETab()
    {
        InitializeComponent();
        DeviceCollectionView.ItemsSource = DiscoveredDevices;
        _bleDeviceManager.DeviceDiscovered += OnDeviceDiscovered;
        _bleDeviceManager.StateChanged += (_,e) => BLEStatusLabel.Text = "BLE Status: " +  e.NewState;
        BLEStatusLabel.Text = "BLE Status: " + _bleDeviceManager.State;
        
        BLEScanToggleBtn.Clicked += OnBLEScanToggleBtnClicked;
    }

    private async void OnDeviceCollectionViewSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _bleDeviceManager.StopScanning();
        _isScanning = false;
        BLEScanToggleBtn.Text = "Start Scanning";
        var deviceGuid = ((IDevice)e.CurrentSelection[0]).Id;
        AppPreferences.LastSelectedBLEUUID = deviceGuid.ToString();
        try
        {
            var connectedDevice = App.Current.Services.GetService<BLEHeartRateManager>();
            await connectedDevice.Connect(deviceGuid);
        }
        catch (Exception ex)
        {
            Log.Error("Error connecting to device", ex);
            await DisplayAlert("Error", $"Error connecting to device: {ex.GetType()}\n\n{ex.Message}", "OK");
            DeviceCollectionView.SelectedItem = null;
            return;
        }
        await Shell.Current.GoToAsync("//home");
    }
    
    private void OnBLEScanToggleBtnClicked(object sender, EventArgs e)
    {
        if (_isScanning)
        {
             _bleDeviceManager.StopScanning();
            _isScanning = false;
            BLEScanToggleBtn.Text = "Start Scanning";
        }
        else
        {
            if (_bleDeviceManager.CheckPermissions())
            {
                _bleDeviceManager.StartScanning();
                _isScanning = true;
                BLEScanToggleBtn.Text = "Stop Scanning";
            }
        }
    }
    
    private void OnDeviceDiscovered(object sender, IDevice e)
    {
        Log.Info($"Device Discovered: {e.Name}");
        MainThread.BeginInvokeOnMainThread(() =>
        {
            DiscoveredDevices.Add(e);
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (_isScanning)
        {
            _bleDeviceManager.StopScanning();
            _isScanning = false;
            BLEScanToggleBtn.Text = "Start Scanning";
        }
    }
}