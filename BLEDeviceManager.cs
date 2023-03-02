#if ANDROID
using Android;
#endif

using System.Data;
using System.Globalization;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using ILogger = MetroLog.ILogger;
using LoggerFactory = MetroLog.LoggerFactory;

namespace CardioMeter;

public class BLEDeviceManager
{
    private static readonly  ILogger Log = LoggerFactory.GetLogger(nameof(BLEDeviceManager));
    private IBluetoothLE Ble { get; }
    public BluetoothState State => Ble.State;

    public BLEDeviceManager(IBluetoothLE ble) {
        this.Ble = ble;
        ble.Adapter.DeviceDiscovered += OnDeviceDiscovered;
        ble.StateChanged += OnStateChanged;
        Log.Info($"BLEDeviceManager created. state={ble.State}");
        
        #if ANDROID
        ((MainActivity) Platform.CurrentActivity)!.PermissionResultEvent += OnPermissionResult;
        #endif
    }

    public BLEDeviceManager() : this(CrossBluetoothLE.Current)
    {
    }
    
    public static Guid Guid16Bit(short uuid16)
    {
        return new Guid("0000" + uuid16.ToString("X4") + "-0000-1000-8000-00805F9B34FB");
    }
    public static Guid Guid16Bit(String uuid16)
    {
        return Guid16Bit(short.Parse(uuid16, NumberStyles.HexNumber));
    }
    
    public event EventHandler<IDevice> DeviceDiscovered;
    public event EventHandler<BluetoothStateChangedArgs> StateChanged; 

    void OnDeviceDiscovered(object sender, DeviceEventArgs e)
    {
        var device = e.Device;
        Log.Info($"Device {device.Name} ({device.Id})");
        DeviceDiscovered?.Invoke(this, e.Device);
    }
    
    void OnStateChanged(object sender, BluetoothStateChangedArgs e)
    {
        Log.Info($"Bluetooth state changed to {e.NewState}");
        StateChanged?.Invoke(this, e);
    }

    void OnPermissionResult(object sender, PermissionResultArgs e)
    {
        if (e.RequestCode == 120)
        {
            Log.Info($"OnPermissionResult: Granted={e.IsGranted}");
        }
    }

    public bool CheckPermissions()
    {

#if ANDROID
        var runtimePermissions = Android.OS.Build.VERSION.SdkInt > Android.OS.BuildVersionCodes.R
            ? new[]
            {
                Manifest.Permission.BluetoothScan, "android.permission.BLUETOOTH_CONNECT"
            }
            : new[]
            {
                Manifest.Permission.Bluetooth, Manifest.Permission.AccessFineLocation
            };

        var allGranted = true;
        foreach (var perm in runtimePermissions)
        {
            var curPermission = Android.App.Application.Context.CheckSelfPermission(Manifest.Permission.BluetoothScan);
            if (curPermission != Android.Content.PM.Permission.Granted)
                allGranted = false;
        }

        if (allGranted)
            return true;
        ((MainActivity) Platform.CurrentActivity)!.RequestPermissions(runtimePermissions, 120);
            return false;
        
#endif

        return (Ble.State == BluetoothState.On);
    }

    public async void StartScanning()
    {
        ScanFilterOptions options = new ScanFilterOptions();
        options.ServiceUuids = new[] { BLEHeartRateManager.HEART_RATE_SERVICE_GUID };
        await Ble.Adapter.StartScanningForDevicesAsync(options);
        Log.Info("Scanning started");
    }
    
    public async void StopScanning()
    {
        await Ble.Adapter.StopScanningForDevicesAsync();
        Log.Info("Scanning stopped");
    }
    
}