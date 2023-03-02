namespace CardioMeter;

public class PermissionResultArgs : EventArgs
{
    public int RequestCode { get;}
    public bool IsGranted { get;}


    public PermissionResultArgs(int requestCode, bool isGranted)
    {
        this.RequestCode = requestCode;
        this.IsGranted = isGranted;
    }
}
