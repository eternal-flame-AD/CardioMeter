namespace CardioMeter;

public class AppPreferences
{
    static IPreferences prefs = Preferences.Default;

    public static String LastSelectedBLEUUID
    {
        get { return prefs.Get("LastSelectedBLEUUID", ""); }
        set { prefs.Set("LastSelectedBLEUUID", value); }
    }

    public static String HeartRateZoneJSON
    {
        get { return prefs.Get("HeartRateZoneJSON", ""); }
        set { prefs.Set("HeartRateZoneJSON", value); }
    }
}