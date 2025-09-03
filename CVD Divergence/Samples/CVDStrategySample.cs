using ATAS.DataFeedsCore;
using ATAS.Indicators;
using ATAS.Indicators.Technical;
using ATAS.Strategies.Chart;
using System.ComponentModel.DataAnnotations;

namespace Strategy.Samples;

public class CVDStrategySample : ChartStrategy
{
    private Type _type = Type.All;
    private decimal _tradeVolume = 1m;

    private readonly ObjectDataSeries _bullishDivergences;
    private readonly ObjectDataSeries _bearishDivergences;

    [Display] public CVDDivergence CDelta { get; set; } = new()
    {
        MaxDistance = 50,
        MinDistance = 20
    };

    [Display] public decimal TradeVolume
    {
        get => _tradeVolume;
        set => _tradeVolume = value > 0m ? value : _tradeVolume;
    }

    public CVDStrategySample()
    {
        Add(CDelta);

        DataSeries[0] = CDelta.DataSeries[0];
        _bullishDivergences = (ObjectDataSeries)CDelta.DataSeries[1];
        _bearishDivergences = (ObjectDataSeries)CDelta.DataSeries[2];

        SubscribeToDrawingLines();
        CDelta.MyPropertyChanged += RecalculateValues;

        void SubscribeToDrawingLines()
        {
            CDelta.Recalculating += TrendLines.Clear;
            CDelta.DivergenceDrawn += TrendLines.Add;
        }
    }

    protected override void OnCalculate(int bar, decimal value)
    {
        var last = GetCandle(bar);

        if (bar == CurrentBar - 1)
            return;

        if (IsValid(_bullishDivergences, bar))
        {
            CloseCurrentPosition();
            OpenPosition(OrderDirections.Buy);
        }
        else if (IsValid(_bearishDivergences, bar))
        {
            CloseCurrentPosition();
            OpenPosition(OrderDirections.Sell);
        }
    }

    private bool IsValid(ObjectDataSeries series, int bar)
    {
        if (series[bar] is not Divergence divergence)
            return false;

        bool allowAll = _type == Type.All;
        bool allowAbsorption = divergence.IsAbsorption && _type == Type.OnlyAbsorption;
        bool allowExhaustion = divergence.IsExhaustion && _type == Type.OnlyExhaustion;

        return allowAll || allowAbsorption || allowExhaustion;
    }

    private void OpenPosition(OrderDirections direction)
    {
        var order = new Order
        {
            Portfolio = Portfolio,
            Security = Security,
            Direction = direction,
            Type = OrderTypes.Market,
            QuantityToFill = GetOrderVolume(),
        };

        OpenOrder(order);
    }

    private void CloseCurrentPosition()
    {
        var order = new Order
        {
            Portfolio = Portfolio,
            Security = Security,
            Direction = CurrentPosition > 0 ? OrderDirections.Sell : OrderDirections.Buy,
            Type = OrderTypes.Market,
            QuantityToFill = Math.Abs(CurrentPosition),
        };

        OpenOrder(order);
    }

    private decimal GetOrderVolume()
    {
        if (CurrentPosition == 0)
            return _tradeVolume;

        if (CurrentPosition > 0)
            return _tradeVolume + CurrentPosition;

        return _tradeVolume + Math.Abs(CurrentPosition);
    }

    public enum Type
    {
        All,
        OnlyAbsorption,
        OnlyExhaustion
    }
}
