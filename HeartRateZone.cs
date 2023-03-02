using System.Text;
using System.Text.Json;
using Microsoft.Maui.Graphics;
using ILogger = MetroLog.ILogger;
using LoggerFactory = MetroLog.LoggerFactory;

namespace CardioMeter;

public interface IColorConverter<T>
{
    public string Color(T value);
}

public class HeartRateZone : IColorConverter<int>
{
    private static readonly  ILogger Log = LoggerFactory.GetLogger(nameof(HeartRateZone));
    public static HeartRateZone Default = new()
    {
        Minimum = 50,
        Maximum = 200,
        ZonePercentages = new List<int>{ 50, 60, 70, 80, 90 },
        ZoneColors = new List<Color>
        {
            Microsoft.Maui.Graphics.Color.FromRgba(0xc0, 0xc0, 0xc0, 0xff),
            Microsoft.Maui.Graphics.Color.FromRgba(0x00, 0xff, 0xff, 0xff),
            Microsoft.Maui.Graphics.Color.FromRgba(0x00, 0xff, 0x00, 0xff),
            Microsoft.Maui.Graphics.Color.FromRgba(0xff, 0xff, 0x00, 0xff),
            Microsoft.Maui.Graphics.Color.FromRgba(0xff, 0x00, 0x00, 0xff),
        }
    };
    
    private static JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new JsonColorConverter()
        }
    };
    
    public int Minimum { get; set; }
    public List<int> ZonePercentages { get; set; }
    public List<Color> ZoneColors { get; set; }
    public int Maximum { get; set; }
    
    public string Black =  "#000000";

    public static HeartRateZone FromPref() {
        var jsonString = AppPreferences.HeartRateZoneJSON;
        Log.Info("HeartRateZone.FromPref: {0}", jsonString);
        if (string.IsNullOrEmpty(jsonString)) {
            return Default;
            
        }
        try
        {
            var fromPref =  JsonSerializer.Deserialize<HeartRateZone>(Encoding.UTF8.GetBytes(jsonString), _jsonSerializerOptions);
            if (fromPref.ZoneColors.Count > 1 && fromPref.ZonePercentages.Count > 1)
                return fromPref;
        }
        catch (Exception ex)
        {
            Log.Error("Error deserializing HeartRateZone", ex);
        }

        Default.SavePref();
        return Default;
    }
    
    public void SavePref()
    {
        var jsonString = JsonSerializer.Serialize(this, _jsonSerializerOptions);
        Log.Info("HeartRateZone.SavePref: {0}", jsonString);
        AppPreferences.HeartRateZoneJSON = jsonString;
    }
    
    public string Color(int value)
    {
        if (value <= Minimum) return Black;
        if (value >= Maximum) return ZoneColors[^1].ToArgbHex();
        var zone = (int)BPMAsZone(value);
        if (zone==0)
            return "";
        return ZoneColors[zone - 1].ToArgbHex();
    }
    
    public double BPMAsZone(int bpm)
    {
        double avgZoneWidth = (double)(Maximum - Minimum) / (ZonePercentages.Count + 1);
        if (bpm <= Minimum)
            return (bpm - Minimum) /avgZoneWidth;

        for (var zoneIdx = 0; zoneIdx <= ZonePercentages.Count; zoneIdx++)
        {
            double zoneBegin = zoneIdx == 0 ? Minimum : 
                (ZonePercentages[zoneIdx - 1] * (double)(Maximum - Minimum) /100+ Minimum);
            double zoneEnd = zoneIdx == ZonePercentages.Count ? Maximum : 
                (ZonePercentages[zoneIdx] * (double)(Maximum - Minimum) /100+ Minimum);
            if (zoneEnd > bpm)
            {
                return zoneIdx + (bpm - zoneBegin) / (zoneEnd - zoneBegin);
            }
        }

        return ZonePercentages.Count + 1 + (bpm - Maximum) / avgZoneWidth;
        
    }
}