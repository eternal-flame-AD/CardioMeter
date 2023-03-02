using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CardioMeter;

public partial class HeartRateZoneEditPage : ContentPage
{
    class HeartRateZoneEdit : ObservableObject
    {
        public int Index { get; set; }
        public string Title { get; set; }
        public Color Color { get; set; }
        public string ColorEdit { get; set; }

        private int _value;

        public int Value
        {
            get => _value;
            set => SetProperty(ref _value, value) ;
        }


        public delegate bool UpdateValueDelegate(int Value);

        public UpdateValueDelegate ValueUpdater { get; set; }
        
        public string Unit { get; set; }
        public bool ColorEditable { get; set; }
        public bool CanInsert { get; set; }
        

        private int _hrMin;
        public int HRMin { get => _hrMin;
            set => SetProperty(ref _hrMin, value);
        }
        private int _hrMax;
        public int HRMax { get => _hrMax; set => SetProperty(ref _hrMax, value); }
        
        private int _effectiveBpm;
        public int EffectiveBPM { get => _effectiveBpm; set => SetProperty(ref _effectiveBpm, value); }
    }

    private ObservableCollection<HeartRateZoneEdit> _hrZonesEdit = new();
    private HeartRateZone _heartRateZone = App.Current.Services.GetService<HeartRateZone>();
    public HeartRateZoneEditPage()
    {
        InitializeComponent();
        setFromCurrentZones();
        HeartRateZoneCollectionView.ItemsSource = _hrZonesEdit;
    }

    private void OnZoneChanged(object sender, TextChangedEventArgs e)
    {
        if (e.OldTextValue == null)
            return;
        var ctx = (HeartRateZoneEdit)((Entry)sender).BindingContext;
        ctx.ValueUpdater(ctx.Value);
        foreach (var z in _hrZonesEdit)
        {
            z.EffectiveBPM = z.Unit == "BPM" ? z.Value : (int)(z.HRMin + (z.HRMax - z.HRMin) * (z.Value / 100f));
        }
        _heartRateZone.SavePref();
    }

    private void setFromCurrentZones()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _hrZonesEdit.Clear();
            _hrZonesEdit.Add(new HeartRateZoneEdit()
            {
                Title = "Min",
                Value = _heartRateZone.Minimum,
                Unit = "BPM",
                Color = Colors.Grey,
                ColorEditable = false,
                CanInsert = false,
                EffectiveBPM = _heartRateZone.Minimum,
                ValueUpdater = (newValue) =>
                {
                    if (newValue >= _heartRateZone.Maximum)
                        return false;
                    _heartRateZone.Minimum = newValue;
                    foreach (var z in _hrZonesEdit)
                        z.HRMin = newValue;
                    return true;
                },
            });
            for (var i = 0; i < _heartRateZone.ZonePercentages.Count; i++)
            {
                var iCopy = i;
                var pct = _heartRateZone.ZonePercentages[i];
                var color = _heartRateZone.ZoneColors[i];
                _hrZonesEdit.Add(new HeartRateZoneEdit()
                {
                    Title = $"Zone {i + 1}",
                    Value = pct,
                    Unit = "% HRR",
                    Color = color,
                    ColorEdit = color.ToHex(),
                    CanInsert = true,
                    EffectiveBPM = (int)(_heartRateZone.Minimum + (_heartRateZone.Maximum - _heartRateZone.Minimum) * (pct / 100f)),
                    Index = _hrZonesEdit.Count,
                    HRMin = _heartRateZone.Minimum,
                    HRMax = _heartRateZone.Maximum,
                    ValueUpdater = (newValue) =>
                    {
                        if (newValue >= (iCopy == _heartRateZone.ZonePercentages.Count - 1 
                                ? 100
                                : _heartRateZone.ZonePercentages[iCopy+1]))
                            return false;
                        if (newValue <= (iCopy == 0 ? 0 : _heartRateZone.ZonePercentages[iCopy-1]))
                            return false;
                        _heartRateZone.ZonePercentages[iCopy] = newValue;
                        return true;
                    }
                });
            }

            _hrZonesEdit.Add(new HeartRateZoneEdit()
            {
                Title = "Max",
                Value = _heartRateZone.Maximum,
                Unit = "BPM",
                Color = Colors.Grey,
                ColorEdit = "#ffffff",
                ColorEditable = false,
                CanInsert = true,
                EffectiveBPM = _heartRateZone.Maximum,
                Index = _hrZonesEdit.Count,
                ValueUpdater = (newValue) =>
                {
                    if (newValue <= _heartRateZone.Minimum)
                        return false;
                    _heartRateZone.Maximum = newValue;
                    foreach (var z in _hrZonesEdit)
                        z.HRMax = newValue;
                    return true;
                },
            });
        });
    }
}