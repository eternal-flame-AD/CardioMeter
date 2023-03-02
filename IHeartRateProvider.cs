namespace CardioMeter;

public struct HeartRateEventArgs
{
    public int HeartRate { get; private set; }
    public int? EnergyExpended { get; private set; }
    public DateTime Timestamp { get; private set; }
    public double? RRInterval { get; private set; }
    
    public override String ToString()
    {
        return String.Format("HeartRate: {0}, EnergyExpended: {1} J, Timestamp: {2}, RRInterval: {3} ms", HeartRate, EnergyExpended, Timestamp, RRInterval);
    }

    public static HeartRateEventArgs FromDummy(DateTime timestamp)
    {
        var ret = new HeartRateEventArgs();
        ret.Timestamp = timestamp;
        var unixMs = (timestamp.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        var periodMs = 30_000;
        var amplitude = 20.0;
        ret.HeartRate = (int)(amplitude * Math.Sin(unixMs / periodMs * 2 * Math.PI) + 160);
        ret.RRInterval = 6000.0 / ret.HeartRate;
        return ret;
    } 

    public static HeartRateEventArgs FromBLEHeartRateMeasurement(byte[] rawValues)
    {
        // https://github.com/oesmith/gatt-xml/blob/master/org.bluetooth.characteristic.heart_rate_measurement.xml
        var ret = new HeartRateEventArgs();
        ret.Timestamp = DateTime.Now;
        var flagByte = rawValues[0];
        var hrIsUint16 = (flagByte & 0x01) != 0;
        var valuePtr = 1;
        if (hrIsUint16)
        {
            ret.HeartRate = BitConverter.ToUInt16(rawValues, valuePtr);
            valuePtr += 2;
        }
        else
        {
            ret.HeartRate = rawValues[1];
            valuePtr += 1;
        }
        var sensorContactFlag = (flagByte & 0x06) >> 1;
        var hasEnergyExpended = (flagByte & 0x08) != 0;
        if (hasEnergyExpended)
        {
            ret.EnergyExpended = BitConverter.ToUInt16(rawValues, valuePtr);
            valuePtr += 2;
        }
        var hasRRIntervals = (flagByte & 0x10) != 0;
        // only get the final RR interval
        if (hasRRIntervals)
        {
            ret.RRInterval = BitConverter.ToUInt16(rawValues, rawValues.Length - 2) / 1024.0 * 1000.0;
        }
        return ret;
    }
}

public interface IHeartRateProvider
{
    public event EventHandler<HeartRateEventArgs> HeartRateReceived;
    public event EventHandler OnConnectionLost;
    public event EventHandler OnConnectionEstablished;
}