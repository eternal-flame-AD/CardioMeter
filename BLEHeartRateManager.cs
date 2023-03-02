using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using ILogger = MetroLog.ILogger;
using LoggerFactory = MetroLog.LoggerFactory;

namespace CardioMeter;

public class BLEHeartRateManager : IHeartRateProvider
{
    private static readonly  ILogger Log = LoggerFactory.GetLogger(nameof(BLEHeartRateManager));
    public const String HEART_RATE_SERVICE_UUID = "180D";
    public static Guid HEART_RATE_SERVICE_GUID => BLEDeviceManager.Guid16Bit(HEART_RATE_SERVICE_UUID);

    private IBluetoothLE Ble { get; }
    private Guid DeviceGuid { get; set;  }
    private IDevice Device { get; set; }
    private ICharacteristic HeartRateCharacteristic { get; set; }

    public event EventHandler<HeartRateEventArgs> HeartRateReceived;
    public event EventHandler OnConnectionLost;
    public event EventHandler OnConnectionEstablished;

    public BLEHeartRateManager(IBluetoothLE ble)
    {
        Ble = ble;
    }

    public BLEHeartRateManager() : this(CrossBluetoothLE.Current)
    {
    }

    private void OnReceiveBLECharacteristic(object sender, CharacteristicUpdatedEventArgs e)
    {
        HeartRateEventArgs hrEvent = HeartRateEventArgs.FromBLEHeartRateMeasurement(e.Characteristic.Value);
        Log.Info($"Decoded heart rate event: {hrEvent}");
        HeartRateReceived?.Invoke(this, hrEvent);
    }

    public async Task<bool> Connect(Guid deviceGuid)
    {
        if (Device?.State == DeviceState.Connected && this.DeviceGuid == deviceGuid)
            return true; 
        this.DeviceGuid = deviceGuid;
        Device =  await Ble.Adapter.ConnectToKnownDeviceAsync(DeviceGuid);
        var heartrateService = await Device.GetServiceAsync(HEART_RATE_SERVICE_GUID);
        HeartRateCharacteristic= await heartrateService.GetCharacteristicAsync(BLEDeviceManager.Guid16Bit(0x2a37)); // Heart Rate Measurement
        HeartRateCharacteristic.ValueUpdated += OnReceiveBLECharacteristic;
        await HeartRateCharacteristic.StartUpdatesAsync();
        OnConnectionEstablished?.Invoke(this, EventArgs.Empty);
        return true;
    }
    
    public async void Disconnect()
    {
        if (Device?.State == DeviceState.Connected)
        {
            await Ble.Adapter.DisconnectDeviceAsync(Device);
            HeartRateCharacteristic = null;
            OnConnectionLost?.Invoke(this, EventArgs.Empty);
        }
    }
    
    public void Dispose() => Disconnect();
    
}