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

    private decimal _barShadowPercent = 0.01m;
    private decimal _cumulativeDelta;

    private readonly ValueDataSeries _cvdLine = new("_cvdLine", "Cumulative Delta")
    {
        UseMinimizedModeIfEnabled = true,
        VisualType = VisualMode.Line,
        Color = Color.Red.Convert(),
        ShowZeroValue = false,
        IsHidden = false
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

    [Range(0, int.MaxValue)]
    [Display(Name = "Bar Shadow Percent From Body", Description = "Determines the minimum percentage of the candle's shadow relative to its body, necessary for the candle's extremum (maximum or minimum) to be taken into account when searching for divergences. Helps filter out weak or insignificant levels.")]
    public decimal BarShadowPercent
    {
        get => _barShadowPercent * 100m;
        set
        {
            _barShadowPercent = value / 100m;
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

        if (UpdateIfUnfinishedBar(lastCandle, bar))
            return;

        _cumulativeDelta += lastCandle.Delta;
        _cvdLine[bar] = _cumulativeDelta;

        DetectDivergences(bar);
    }

    #region Private Methods

    private bool UpdateIfUnfinishedBar(in IndicatorCandle lastCandle, in int lastBar)
    {
        if (lastBar == CurrentBar - 1)
        {
            _cvdLine[lastBar] = _cumulativeDelta + lastCandle.Delta;
            return true;
        }
        return false;
    }

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

        Divergence bearish = new(lastHigh, previousHigh, _bearishDivergence, DivergenceType.Bearish);
        DrawDivergence(_bearishDivergences, bearish, lastBar);
    }
    
    private void FindBullishDivergence(in int pastBar, in int lastBar)
    {
        var (lastLow, previousLow) = GetLastTwoLows(pastBar, lastBar);

        if (IsValid(lastLow, previousLow, lastBar) is false)
            return;

        Divergence bullish = new(lastLow, previousLow, _bullishDivergence, DivergenceType.Bullish);
        DrawDivergence(_bullishDivergences, bullish, lastBar);
    }

    private (PriceExtremum last, PriceExtremum previous) GetLastTwoHighs(in int startBar, in int endBar)
    {
        static bool SkipCondition(IndicatorCandle candle, decimal tempExtremum) => candle.High < tempExtremum;
        static decimal CandlePointSelector(IndicatorCandle candle) => candle.High;
        static decimal ShadowPercentSelector(IndicatorCandle candle) => candle.UpperShadowPercent();

        return GetLastTwoExtremums(SkipCondition, CandlePointSelector, ShadowPercentSelector, startBar, endBar, decimal.MinValue);
    }

    private (PriceExtremum last, PriceExtremum previous) GetLastTwoLows(in int startBar, in int endBar)
    {
        static bool SkipCondition(IndicatorCandle candle, decimal tempExtremum) => candle.Low > tempExtremum;
        static decimal CandlePointSelector(IndicatorCandle candle) => candle.Low;
        static decimal ShadowPercentSelector(IndicatorCandle candle) => candle.LowerShadowPercent();

        return GetLastTwoExtremums(SkipCondition, CandlePointSelector, ShadowPercentSelector, startBar, endBar, decimal.MaxValue);
    }

    private (PriceExtremum last, PriceExtremum previous) GetLastTwoExtremums(Func<IndicatorCandle, decimal, bool> skipCondition, Func<IndicatorCandle, decimal> candlePointSelector, Func<IndicatorCandle, decimal> shadowPercentSelector, in int startBar, in int endBar, decimal currentExtremumValue)
    {
        IndicatorCandle? lastCandle = null;

        PriceExtremum previous = default;
        PriceExtremum last = default;

        for (int i = startBar; i <= endBar; i++)
        {
            var candle = GetCandle(i);

            if (skipCondition.Invoke(candle, currentExtremumValue))
                continue;

            if (IsSequentialBar(i) is false)
                previous = last;

            currentExtremumValue = candlePointSelector.Invoke(candle);
            last = new PriceExtremum(i, currentExtremumValue, _cvdLine[i]);
            lastCandle = candle;
        }

        if (lastCandle is not null && PassesShadowFilter(lastCandle) is false)
            return (default, default);

        return (last, previous);

        bool IsSequentialBar(int bar) => last.Bar is 0 || last.Bar == bar - 1;
        bool PassesShadowFilter(in IndicatorCandle candle) => shadowPercentSelector.Invoke(candle) >= _barShadowPercent;
    }
    
    private bool IsValid(in PriceExtremum last, in PriceExtremum previous, in int lastBar)
    {
        bool noSuitableExtremumsFound = last == default || previous == default;
        bool isTooCloseDistance = last.Bar - previous.Bar < _minDistance;
        bool isCurrentExtremum = last.Bar == lastBar;

        if (noSuitableExtremumsFound)
            return false;

        if (isTooCloseDistance)
            return false;

        if (isCurrentExtremum is false)
            return false;

        if (IsDivergence(previous, last) is false)
            return false;

        bool alreadyDrawn = _linePositions.Add((previous.Bar, last.Bar, previous.Price, last.Price)) is false;

        if (alreadyDrawn)
            return false;

        return true;
    }

    private bool IsDivergence(in PriceExtremum left, in PriceExtremum right)
    {
        var leftDelta = left.Delta;
        var rightDelta = right.Delta;
        var leftPrice = left.Price;
        var rightPrice = right.Price;

        return (RisingOrEqual(leftDelta, rightDelta) && Falling(leftPrice, rightPrice))
            || (FallingOrEqual(leftDelta, rightDelta) && Rising(leftPrice, rightPrice))
            || (RisingOrFalling(leftDelta, rightDelta) && leftPrice == rightPrice);
    }

    private void DrawDivergence(ObjectDataSeries divergences, in Divergence divergence, in int last)
    {
        var pen = new Pen(divergence.Color);
        TrendLine line = new(divergence.Left.Bar, divergence.Left.Price, divergence.Right.Bar, divergence.Right.Price, pen);

        TrendLines.Add(line);
        divergences[last] = divergence;
        DivergenceDrawn?.Invoke(line);
    }

    #endregion
}
