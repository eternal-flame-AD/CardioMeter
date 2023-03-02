using ILogger = MetroLog.ILogger;
using LoggerFactory = MetroLog.LoggerFactory;

namespace CardioMeter;

public class DummyHeartRateSource : IHeartRateProvider
{
    private static readonly ILogger Log = LoggerFactory.GetLogger(nameof(DummyHeartRateSource));
    private PeriodicTimer _timer;
    private Task _task;
    
    public DummyHeartRateSource()
    {  
        KeepAlive();
    }
    
    public void KeepAlive()
    {
        if (_task == null || _task.IsCompleted)
        {
            _task = Task.Run(async () =>
            {
                _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
                while (true)
                {
                    var ticked =  await _timer.WaitForNextTickAsync();
                    if (ticked)
                        HeartRateReceived?.Invoke(this, HeartRateEventArgs.FromDummy(DateTime.Now));
                }
            });
        }
    }
    
    public void Dispose()
    {
        _timer.Dispose();
    }
    public event EventHandler<HeartRateEventArgs> HeartRateReceived;
    public event EventHandler OnConnectionLost;
    public event EventHandler OnConnectionEstablished;
}