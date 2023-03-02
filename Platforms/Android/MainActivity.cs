using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;

namespace CardioMeter;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    public event EventHandler<PermissionResultArgs> PermissionResultEvent;
    
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
    {
        var isGranted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;
        var evtArgs = new PermissionResultArgs(requestCode, isGranted);
        PermissionResultEvent?.Invoke(this, evtArgs);

        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }
}
