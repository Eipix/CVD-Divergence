# 📊 CVD Divergence (ATAS Indicator)

**CVD Divergence** is a custom indicator for the **ATAS** platform that analyzes the cumulative delta and automatically detects divergences between price and delta.

The indicator plots divergence lines and highlights them in different colors depending on the direction (bullish or bearish).

---

<img src="https://github.com/Eipix/CVD-Divergence/blob/master/assets/divergences_btc_futures.png">

---

## ⚙️ Parameters

<img src="https://github.com/Eipix/CVD-Divergence/blob/master/assets/parameters.png">

---

| Parameter | Type | Description |
|-----------|------|-------------|
| **Max Bars Distance** | `int` | Upper limit for defining divergences (in bars). |
| **Min Bars Distance** | `int` | Filters out divergences whose distance is less than the specified value. |
| **Bullish Color** | `Color` | The color of bullish divergences. |
| **Bearish Color** | `Color` | The color of bearish divergences. |

---

## 📌 Integration into Other Indicators

**CVD Divergence** can be used as a composite indicator for strategies and other indicators.  
It consists of three DataSeries:

1. **ValueDataSeries**: cumulative delta values  
2. **ObjectDataSeries**: bullish divergences built from lower extrema, storing a **Divergence** structure  
3. **ObjectDataSeries**: bearish divergences built from upper extrema, storing a **Divergence** structure  

Example of a strategy that uses **CVD Divergence**: 

```

using ATAS.DataFeedsCore;
using ATAS.Indicators;
using ATAS.Indicators.Technical;
using ATAS.Strategies.Chart;
using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Strategy.Samples;

public class CVDStrategySample : ChartStrategy
{
    private Type _type = Type.All;
    private decimal _tradeVolume = 1m;

    private readonly ObjectDataSeries _bullishDivergences;
    private readonly ObjectDataSeries _bearishDivergences;

    private readonly ValueDataSeries _longArrows = new("_bullishSignals", "Bullish Signals")
    {
        VisualType = VisualMode.UpArrow,
        Color = Color.Green.Convert(),
        Width = 2
    };
    private readonly ValueDataSeries _shortArrows = new("_bearishSignals", "Bearish Signals")
    {
        VisualType = VisualMode.DownArrow,
        Color = Color.Red.Convert(),
        Width = 2
    };

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
        DataSeries.Add(_longArrows);
        DataSeries.Add(_shortArrows);

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
            _longArrows[bar] = last.Low - InstrumentInfo!.TickSize * 2;

            CloseCurrentPosition();
            OpenPosition(OrderDirections.Buy);
        }
        else if (IsValid(_bearishDivergences, bar))
        {
            _shortArrows[bar] = last.High + InstrumentInfo!.TickSize * 2;

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

```

This example can be found in the **CVD Divergence/Samples** folder.
