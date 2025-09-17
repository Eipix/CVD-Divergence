using ATAS.Indicators.Drawing;
using CVD_Divergence.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

using static CVD_Divergence.Extensions;

namespace ATAS.Indicators.Technical;

[DisplayName("CVD Divergence")]
[Category("Bid x Ask,Delta,Volume")]
public class CVDDivergence : Indicator
{
    public event Action? MyPropertyChanged;
    public event Action<TrendLine>? DivergenceDrawn;
    public event Action? Recalculating;

    private HashSet<(int, int, decimal, decimal)> _linePositions = new();

    private Color _bullishDivergence = Color.Green;
    private Color _bearishDivergence = Color.Red;

    private int _minDistance = 10;
    private int _maxDistance = 100;

    private decimal _cumulativeDelta;

    private readonly ValueDataSeries _cvdLine = new("_cvdLine", "Cumulative Delta")
    {
        VisualType = VisualMode.Line,
        Color = Color.Red.Convert(),
    };
    private readonly ObjectDataSeries _bullishDivergences = new("_bullishDivergences", "Bullish Divergences");
    private readonly ObjectDataSeries _bearishDivergences = new("_bearishDivergences", "Bearish Divergences");

    #region Properties

    [Range(1, int.MaxValue)]
    [Display(Name = "Max Bars Distance", Description = "Sets the upper limit for searching for divergences. If there are more than the specified number of bars between two extremes, they will not be considered as a related pair for analysis.")]
    public int MaxDistance
    {
        get => _maxDistance;
        set
        {
            _maxDistance = Math.Max(_minDistance, value);
            RecalculateValues();
            MyPropertyChanged?.Invoke();
        }
    }

    [Range(1, int.MaxValue)]
    [Display(Name = "Min Bars Distance", Description = "Determines the minimum number of bars that two price extremes must be separated by in order for a divergence to be recorded between them. A smaller value is more sensitive to small fluctuations, a larger value filters out noise.")]
    public int MinDistance
    {
        get => _minDistance;
        set
        {
            _minDistance = Math.Min(value, _maxDistance);
            RecalculateValues();
            MyPropertyChanged?.Invoke();
        }
    }

    [Display(Name = "Bullish Color")] public Color BullishDivergence
    {
        get => _bullishDivergence;
        set
        {
            _bullishDivergence = value;
            RecalculateValues();
            MyPropertyChanged?.Invoke();
        }
    }

    [Display(Name = "Bearish Color")] public Color BearishDivergence
    {
        get => _bearishDivergence;
        set
        {
            _bearishDivergence = value;
            RecalculateValues();
            MyPropertyChanged?.Invoke();
        }
    }
    
    #endregion

    public CVDDivergence() : base(true)
    {
        Panel = IndicatorDataProvider.NewPanel;
        DenyToChangePanel = true;
        EnableCustomDrawing = true;

        DataSeries[0] = _cvdLine;
        DataSeries.Add(_bullishDivergences);
        DataSeries.Add(_bearishDivergences);
    }

    protected override void OnRecalculate()
    {
        _linePositions.Clear();
        TrendLines.Clear();
        _cumulativeDelta = 0;
        Recalculating?.Invoke();
    }

    protected override void OnCalculate(int bar, decimal value)
    {
        var lastCandle = GetCandle(bar);

        if (IsUnfinishedBar(bar))
        {
            _cvdLine[bar] = _cumulativeDelta + lastCandle.Delta;
            return;
        }

        _cumulativeDelta += lastCandle.Delta;
        _cvdLine[bar] = _cumulativeDelta;

        DetectDivergences(bar);
    }

    #region Private Methods

    private bool IsUnfinishedBar(in int bar) => bar == CurrentBar - 1;

    private void DetectDivergences(in int lastBar)
    {
        int pastBar = lastBar - _maxDistance;

        if (pastBar < 0)
            return;

        FindBearishDivergence(pastBar, lastBar);
        FindBullishDivergence(pastBar, lastBar);
    }

    private void FindBearishDivergence(in int pastBar, in int lastBar)
    {
        var (lastHigh, previousHigh) = GetLastTwoHighs(pastBar, lastBar);

        if (IsValid(lastHigh, previousHigh, lastBar) is false)
            return;

        Divergence bearish = new(lastHigh!, previousHigh!, _bearishDivergence, DivergenceType.Bearish);
        DrawDivergence(_bearishDivergences, bearish, lastBar);
    }
    
    private void FindBullishDivergence(in int pastBar, in int lastBar)
    {
        var (lastLow, previousLow) = GetLastTwoLows(pastBar, lastBar);

        if (IsValid(lastLow, previousLow, lastBar) is false)
            return;

        Divergence bullish = new(lastLow!, previousLow!, _bullishDivergence, DivergenceType.Bullish);
        DrawDivergence(_bullishDivergences, bullish, lastBar);
    }

    private (PriceExtremum? last, PriceExtremum? previous) GetLastTwoHighs(in int startBar, in int endBar)
    {
        static bool SkipCondition(IndicatorCandle candle, decimal tempExtremum) => candle.High < tempExtremum;
        static decimal CandlePointSelector(IndicatorCandle candle) => candle.High;

        return GetLastTwoExtremums(SkipCondition, CandlePointSelector, startBar, endBar, decimal.MinValue);
    }

    private (PriceExtremum? last, PriceExtremum? previous) GetLastTwoLows(in int startBar, in int endBar)
    {
        static bool SkipCondition(IndicatorCandle candle, decimal tempExtremum) => candle.Low > tempExtremum;
        static decimal CandlePointSelector(IndicatorCandle candle) => candle.Low;

        return GetLastTwoExtremums(SkipCondition, CandlePointSelector, startBar, endBar, decimal.MaxValue);
    }

    private (PriceExtremum? last, PriceExtremum? previous) GetLastTwoExtremums(Func<IndicatorCandle, decimal, bool> skipCondition, Func<IndicatorCandle, decimal> candlePointSelector, in int startBar, in int endBar, decimal currentExtremumValue)
    {
        PriceExtremum? previous = null;
        PriceExtremum? last = null;

        for (int i = startBar; i <= endBar; i++)
        {
            var candle = GetCandle(i);

            if (skipCondition.Invoke(candle, currentExtremumValue))
                continue;

            if (IsSequentialBar(i) is false)
                previous = last;

            currentExtremumValue = candlePointSelector.Invoke(candle);
            last = new PriceExtremum(i, currentExtremumValue, _cvdLine[i]);
        }

        return (last, previous);

        bool IsSequentialBar(int bar) => last is null or { Bar: 0} || last.Bar == bar - 1;
    }
    
    private bool IsValid(PriceExtremum? last, PriceExtremum? previous, int lastBar)
    {

        if (IsExtremumsNotFound())
            return false;

        if (IsLastLaterThanPrevious() is false)
            throw new InvalidOperationException($"last extremum must be later than previous last.Bar - {last.Bar}, previous.Bar - {previous.Bar}");

        if (IsTooCloseDistance())
            return false;

        if (IsCurrentExtremum() is false)
            return false;

        if (IsDivergence(previous!, last!) is false)
            return false;

        if (IsAlreadyDrawn())
            return false;

        return true;

        bool IsExtremumsNotFound() => last is null || previous is null;
        bool IsLastLaterThanPrevious() => last!.Bar > previous!.Bar;
        bool IsTooCloseDistance() => last!.Bar - previous!.Bar < _minDistance;
        bool IsCurrentExtremum() => last!.Bar == lastBar;
        bool IsAlreadyDrawn() => _linePositions.Add((previous!.Bar, last!.Bar, previous.Price, last.Price)) is false;
    }

    private bool IsDivergence(PriceExtremum left, PriceExtremum right)
    {
        decimal leftDelta = left.Delta;
        decimal rightDelta = right.Delta;
        decimal leftPrice = left.Price;
        decimal rightPrice = right.Price;

        return (RisingOrEqual(leftDelta, rightDelta) && Falling(leftPrice, rightPrice))
            || (FallingOrEqual(leftDelta, rightDelta) && Rising(leftPrice, rightPrice))
            || (RisingOrFalling(leftDelta, rightDelta) && leftPrice == rightPrice);
    }

    private void DrawDivergence(ObjectDataSeries divergences, Divergence divergence, in int last)
    {
        var pen = new Pen(divergence.Color);
        TrendLine line = new(divergence.Left.Bar, divergence.Left.Price, divergence.Right.Bar, divergence.Right.Price, pen);

        TrendLines.Add(line);
        divergences[last] = divergence;
        DivergenceDrawn?.Invoke(line);
    }

    #endregion
}
