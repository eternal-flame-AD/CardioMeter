using ILogger = MetroLog.ILogger;
using LoggerFactory = MetroLog.LoggerFactory;

namespace CardioMeter;


public class HeartRateGraph : IDrawable
{
    private static readonly ILogger Log = LoggerFactory.GetLogger(nameof(HeartRateGraph));
    private HeartRateZone _heartRateZone = App.Current.Services.GetService<HeartRateZone>();
    private IHeartRateProvider _heartRateSource;
    private List<HeartRateEventArgs> _heartRateData = new ();
    private TimeSpan _xAxisSpan = TimeSpan.FromMinutes(5);
    RectF _rect;
    
    public void setHeartRateSource(IHeartRateProvider heartRateSource)
    {
        lock (this)
        {
            if (_heartRateSource != null)
            {
                _heartRateSource.HeartRateReceived -= OnHeartRateReceived;
            }
            _heartRateSource = heartRateSource;
            _heartRateSource.HeartRateReceived += OnHeartRateReceived;
            _heartRateData = new();
        }
    }
    
    private void OnHeartRateReceived(object sender, HeartRateEventArgs e)
    {
        lock (this)
        {
            _heartRateData.Add(e);
            _heartRateData.Sort((x, y) => x.Timestamp.CompareTo(y.Timestamp));
            var firstIdx = _heartRateData.FindIndex((x) => DateTime.Now - x.Timestamp <_xAxisSpan);
            if (firstIdx > 0)
                _heartRateData.RemoveRange(0, firstIdx);
        }
    }

    private void DrawHorizontalLine(ICanvas canvas, float y)
    {
        canvas.DrawLine(_rect.X, _rect.Y + y, _rect.X + _rect.Width, _rect.Y+y);
    }
    
    private double YAxisValueToY(double value)
    {
        return _rect.Y + (1-value) * _rect.Height;
    }
    
    private double XAxisValueToX(double value)
    {
        return _rect.X + value * _rect.Width;
    }
    
    private double BpmToYAxisValue(double bpm)
    {
        return  (bpm - _heartRateZone.Minimum) / (_heartRateZone.Maximum - _heartRateZone.Minimum);
    }

    private double TimeStampToXAxisValue(DateTime time)
    {
        return 1- (DateTime.Now - time).Ticks / (double)_xAxisSpan.Ticks;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        _rect = dirtyRect;
        canvas.Antialias = true;
        canvas.FillColor = Colors.Transparent;
        
        canvas.StrokeColor = Colors.Gray;
        canvas.StrokeSize = 1;
        canvas.DrawRectangle(dirtyRect);

        var currentBpm = 0;
        var currentZone = 0;
        if (_heartRateData.Count > 0)
        {
            currentBpm = _heartRateData.Last().HeartRate;
            currentZone = (int)_heartRateZone.BPMAsZone(currentBpm);
        }

        for (int zone = 0; zone < _heartRateZone.ZonePercentages.Count; zone++)
        {
            canvas.StrokeColor =  _heartRateZone.ZoneColors[zone];
            var y = YAxisValueToY(_heartRateZone.ZonePercentages[zone]/(double)100);
            DrawHorizontalLine(canvas, (float)y);
            if (currentBpm > _heartRateZone.Minimum && currentBpm < _heartRateZone.Maximum &&  currentZone == zone+1)
            {
                float yStart = (float)YAxisValueToY(_heartRateZone.ZonePercentages[zone] / (double)100);
                float yEnd = currentZone  < _heartRateZone.ZonePercentages.Count
                    ? (float)YAxisValueToY(_heartRateZone.ZonePercentages[currentZone] / (double)100)
                    : (_rect.Y +  _rect.Height);
                var rect = new RectF(_rect.X,yStart ,_rect.Width, yEnd - yStart);
                
                canvas.FillColor = _heartRateZone.ZoneColors[zone].WithAlpha(0.2f);
                canvas.FillRectangle(rect);
            }
        }

        lock (this)
        {
            PointF? lastPoint = null;
            foreach (var data in _heartRateData)
            {
                var zone = _heartRateZone.BPMAsZone(data.HeartRate);
                if (zone < 1)
                    canvas.StrokeColor = Colors.Gray;
                else 
                    canvas.StrokeColor = _heartRateZone.ZoneColors[(int)zone-1];
                var point = new PointF((float)XAxisValueToX(TimeStampToXAxisValue(data.Timestamp)),
                    (float)YAxisValueToY(BpmToYAxisValue(data.HeartRate)));
               // Log.Info($"Plotting point {point}");
                if (lastPoint != null)
                    canvas.DrawLine(lastPoint.Value, point);
                lastPoint = point;
            }
        }
    }
}